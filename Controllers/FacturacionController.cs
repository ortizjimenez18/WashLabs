using Microsoft.AspNetCore.Mvc;
using WashLabs.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using WashLabs.Models;
using WashLabs.Models.DTOs;
using WashLabs.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace WashLabs.Controllers;

[Authorize(Roles = "Cliente,Encargado,Administrador")]
public sealed class FacturacionController(IFacturaService facturas, IProductoService productos, IOperacionService operaciones) : Controller
{
    public async Task<IActionResult> Index([FromQuery] FiltroFacturacionDTO filtro, CancellationToken cancellationToken)
    {
        var historial = ModelState.IsValid ? await facturas.ConsultarAsync(filtro, User.Id(), cancellationToken)
            : await facturas.OpcionesAsync(User.Id(), cancellationToken);
        return View(new FacturacionIndexViewModel { Filtro = filtro, Historial = historial, MostrarSucursal = User.IsInRole("Administrador") });
    }

    public async Task<IActionResult> Detalle(long id, CancellationToken cancellationToken,
        [StringLength(120)] string? busquedaProducto = null, int? categoriaProductoId = null,
        bool mostrarProductos = false, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        var factura = await facturas.ObtenerAsync(id, cancellationToken, User.Id());
        if (factura is null) return NotFound();
        var puedeEditar = factura.PermiteProductos && (User.IsInRole("Encargado") || User.IsInRole("Administrador"));
        return View(new FacturacionDetalleViewModel { Factura = factura, PuedeEditar = puedeEditar,
            MostrarProductos = puedeEditar && mostrarProductos, BusquedaProducto = busquedaProducto,
            CategoriaProductoId = categoriaProductoId, VolverUrl = VolverSeguro(volverUrl),
            Productos = puedeEditar && mostrarProductos ? await productos.ListarAsync(categoriaProductoId, busquedaProducto, cancellationToken) : [],
            Categorias = puedeEditar && mostrarProductos ? await productos.ListarCategoriasAsync(cancellationToken) : [] });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> AgregarProducto(long id, int productoId, decimal cantidad, CancellationToken ct, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        TempData["Mensaje"] = await facturas.AgregarProductoAsync(id, productoId, cantidad, User.Id(), ct)
            ? "Producto incluido en la proforma." : "No se pudo agregar el producto. Revisa la cantidad, el estado y los permisos.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> CambiarCantidad(long id, long detalleId, decimal cantidad, CancellationToken ct, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        TempData["Mensaje"] = await facturas.CambiarCantidadAsync(id, detalleId, cantidad, User.Id(), ct)
            ? "Cantidad actualizada. Los totales de la proforma se recalcularon."
            : "No se pudo cambiar la cantidad. Revisa las unidades, los descuentos y el estado de la proforma.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> QuitarProducto(long id, long detalleId, CancellationToken ct, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        TempData["Mensaje"] = await facturas.QuitarProductoAsync(id, detalleId, User.Id(), ct)
            ? "Producto retirado. Los totales de la proforma se recalcularon."
            : "No se pudo quitar el producto. Revisa el documento y sus permisos.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }

    private string? VolverSeguro(string? url) => !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url)
        && (url == "/Facturacion" || url == "/Facturacion/Index" || url.StartsWith("/Facturacion?", StringComparison.Ordinal)
            || url.StartsWith("/Facturacion/Index?", StringComparison.Ordinal)) ? url : null;

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> CompletarCita(long id, CancellationToken ct, string? volverUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        var factura = await facturas.ObtenerAsync(id, ct, User.Id());
        if (factura?.CitaId is null) return NotFound();
        TempData["Mensaje"] = await operaciones.CompletarCitaAsync(factura.CitaId.Value, User.Id(), ct)
            ? "Cita completada. Factura emitida y lavado acreditado en WashPoints."
            : "No se pudo completar la cita. Revisa el estado y los permisos de la orden.";
        return RedirectToAction(nameof(Detalle), new { id, volverUrl = VolverSeguro(volverUrl) });
    }
}
