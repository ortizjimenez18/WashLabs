using Microsoft.AspNetCore.Mvc;
using WashLabs.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using WashLabs.Models;
using WashLabs.Models.DTOs;
using WashLabs.ViewModels;

namespace WashLabs.Controllers;

[Authorize(Roles = "Cliente,Encargado,Administrador")]
public sealed class FacturacionController(IFacturaService facturas, IProductoService productos, IOperacionService operaciones,
    WashLabs.Services.TipoCambioHacienda hacienda, IReservaService reservas, ICuentaService cuentas) : Controller
{
    private int UsuarioId => User.IsInRole("Administrador") && ReservasController.EncargadoDePruebaId > 0
        ? ReservasController.EncargadoDePruebaId : User.Id();

    [HttpGet, Authorize(Roles = "Encargado,Administrador")]
    public Task<IActionResult> Crear(int? sucursalId, CancellationToken ct) =>
        MostrarEmision(new() { SucursalId = sucursalId ?? 0, Lineas = [new()] }, ct);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> Crear(EmitirFacturaDTO solicitud, CancellationToken ct, string? cotizacion = null)
    {
        if (ModelState.IsValid)
        {
            var tasa = await hacienda.ParaEmitirAsync(cotizacion, UsuarioId, solicitud.SolicitudId.ToString("D"), ct);
            var id = await facturas.EmitirAsync(solicitud, UsuarioId, tasa, ct);
            if (id is not null)
            {
                TempData["Mensaje"] = "Factura emitida correctamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            ModelState.AddModelError("", "No se pudo emitir la factura. Revisa el cliente, la sucursal, las líneas y las cantidades.");
        }
        if (solicitud.Lineas.Count is < 1 or > 50) solicitud.Lineas = [new()];
        return await MostrarEmision(solicitud, ct);
    }

    private async Task<IActionResult> MostrarEmision(EmitirFacturaDTO solicitud, CancellationToken ct)
    {
        var formulario = await reservas.FormularioAsync(UsuarioId, ct, solicitud.SucursalId > 0 ? solicitud.SucursalId : null);
        var encargado = await cuentas.SesionAsync(UsuarioId, ct);
        if (formulario is null || encargado is null) return NotFound();
        solicitud.SucursalId = formulario.SucursalId;
        var tasa = await hacienda.VentaAsync(ct);
        return View("Crear", new EmitirFacturaViewModel(solicitud, formulario, encargado.Nombre,
            await operaciones.ClientesAsync(ct), await productos.ListarAsync(null, null, ct), tasa,
            hacienda.Proteger(tasa, UsuarioId, solicitud.SolicitudId.ToString("D"))));
    }
    public async Task<IActionResult> Index([FromQuery] FiltroFacturacionDTO filtro, CancellationToken cancellationToken)
    {
        var historial = ModelState.IsValid ? await facturas.ConsultarAsync(filtro, UsuarioId, cancellationToken)
            : await facturas.OpcionesAsync(UsuarioId, cancellationToken);
        return View(new FacturacionIndexViewModel { Filtro = filtro, Historial = historial, MostrarSucursal = User.IsInRole("Administrador") });
    }

    public async Task<IActionResult> Detalle(long id, CancellationToken cancellationToken,
        bool mostrarProductos = false, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        var factura = await facturas.ObtenerAsync(id, cancellationToken, UsuarioId);
        if (factura is null) return NotFound();
        var puedeEditar = factura.PermiteProductos && (User.IsInRole("Encargado") || User.IsInRole("Administrador"));
        if (factura.TipoDocumento == "PROFORMA")
            factura = factura with { TipoCambio = await hacienda.VentaAsync(cancellationToken),
                Encargado = puedeEditar ? (await cuentas.SesionAsync(UsuarioId, cancellationToken))?.Nombre : null };
        var formulario = puedeEditar && mostrarProductos
            ? await reservas.FormularioAsync(UsuarioId, cancellationToken, factura.Sucursal.SucursalId) : null;
        return View(new FacturacionDetalleViewModel { Factura = factura, PuedeEditar = puedeEditar,
            MostrarProductos = puedeEditar && mostrarProductos, VolverUrl = VolverSeguro(volverUrl),
            Cotizacion = puedeEditar ? hacienda.Proteger(factura.TipoCambio, UsuarioId, $"proforma-{id}") : null,
            Servicios = formulario?.Servicios ?? [],
            Productos = puedeEditar && mostrarProductos ? await productos.ListarAsync(null, null, cancellationToken) : [] });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> AgregarServicio(long id, int servicioId, decimal cantidad, CancellationToken ct, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        TempData["Mensaje"] = await facturas.AgregarServicioAsync(id, servicioId, cantidad, UsuarioId, ct)
            ? "Servicio incluido en la proforma." : "No se pudo agregar el servicio. Si ya está en la orden, revisa sus unidades.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> AgregarProducto(long id, int productoId, decimal cantidad, CancellationToken ct, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        TempData["Mensaje"] = await facturas.AgregarProductoAsync(id, productoId, cantidad, UsuarioId, ct)
            ? "Producto incluido en la proforma." : "No se pudo agregar el producto. Revisa la cantidad, el estado y los permisos.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> CambiarCantidad(long id, long detalleId, decimal cantidad, CancellationToken ct, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        TempData["Mensaje"] = await facturas.CambiarCantidadAsync(id, detalleId, cantidad, UsuarioId, ct)
            ? "Cantidad actualizada. Los totales de la proforma se recalcularon."
            : "No se pudo cambiar la cantidad. Revisa las unidades, los descuentos y el estado de la proforma.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> QuitarProducto(long id, long detalleId, CancellationToken ct, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        TempData["Mensaje"] = await facturas.QuitarProductoAsync(id, detalleId, UsuarioId, ct)
            ? "Línea retirada. Los totales de la proforma se recalcularon."
            : "No se pudo quitar la línea. Revisa el documento y sus permisos.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }

    private string? VolverSeguro(string? url) => !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url)
        && (url == "/Facturacion" || url == "/Facturacion/Index" || url.StartsWith("/Facturacion?", StringComparison.Ordinal)
            || url.StartsWith("/Facturacion/Index?", StringComparison.Ordinal)) ? url : null;

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> CompletarCita(long id, CancellationToken ct, string? volverUrl = null, string? cotizacion = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        var factura = await facturas.ObtenerAsync(id, ct, UsuarioId);
        if (factura?.CitaId is null) return NotFound();
        TempData["Mensaje"] = await operaciones.CompletarCitaAsync(factura.CitaId.Value, UsuarioId, ct,
            factura.TipoDocumento == "PROFORMA" ? await hacienda.ParaEmitirAsync(cotizacion, UsuarioId, $"proforma-{id}", ct) : null)
            ? factura.TieneCanjeWashPoints
                ? "Cita completada. Factura emitida con el descuento WashPoints."
                : "Cita completada. Factura emitida y lavado acreditado en WashPoints."
            : "No se pudo completar la cita. Revisa el estado y los permisos de la orden.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }
}
