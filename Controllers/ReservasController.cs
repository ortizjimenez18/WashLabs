using Microsoft.AspNetCore.Mvc;
using WashLabs.Services.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.ViewModels;
using Microsoft.AspNetCore.Authorization;
using WashLabs.Models;

namespace WashLabs.Controllers;

[Authorize(Roles = "Cliente,Encargado,Administrador")]
public sealed class ReservasController(IReservaService reservas, IOperacionService operaciones) : Controller
{
    private int EncargadoUsuarioId => User.Id();

    private async Task<bool> CargarFormulario(CrearReservaViewModel model, CancellationToken ct)
    {
        model.Formulario = await reservas.FormularioAsync(EncargadoUsuarioId, ct, model.Reserva.SucursalId > 0 ? model.Reserva.SucursalId : null);
        if (model.Formulario is null) return false;
        model.Reserva.SucursalId = model.Formulario.SucursalId;
        model.Clientes = await operaciones.ClientesAsync(ct);
        if (User.IsInRole("Cliente")) model.Clientes = model.Clientes.Where(c => c.ClienteId == User.Id()).ToArray();
        model.Vehiculos = await operaciones.VehiculosAsync(ct, User.IsInRole("Cliente") ? User.Id() : null);
        if (model.Reserva.Fecha.HasValue && model.Reserva.ServicioId > 0)
            model.Horarios = await reservas.HorariosAsync(EncargadoUsuarioId, model.Reserva.ServicioId, model.Reserva.Fecha.Value, ct, model.Reserva.SucursalId);
        return true;
    }

    [HttpGet]
    public async Task<IActionResult> Crear(int? servicioId, int? sucursalId, CancellationToken ct)
    {
        var model = new CrearReservaViewModel();
        model.Reserva.SucursalId = sucursalId ?? 0;
        if (!await CargarFormulario(model, ct)) return NotFound();
        if (model.Clientes.Count == 1) model.Reserva.ClienteId = model.Clientes[0].ClienteId;
        var propios = model.Vehiculos.Where(v => v.ClienteId == model.Reserva.ClienteId).ToArray();
        if (propios.Length == 1) model.Reserva.VehiculoId = propios[0].VehiculoId;
        if (servicioId.HasValue && model.Formulario!.Servicios.Any(s => s.ServicioId == servicioId.Value))
            model.Reserva.ServicioId = servicioId.Value;
        model.Reserva.Respuestas = model.Formulario!.Preguntas.Select(p => new CrearRespuestaReservaDTO { PreguntaId = p.PreguntaId }).ToList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Disponibilidad(int servicioId, DateOnly fecha, int sucursalId, CancellationToken ct)
    {
        if (!ModelState.IsValid || servicioId <= 0) return BadRequest();
        return Json((await reservas.HorariosAsync(EncargadoUsuarioId, servicioId, fecha, ct, sucursalId)).Select(h => h.ToString("HH:mm")));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearReservaViewModel model, CancellationToken ct)
    {
        if (User.IsInRole("Cliente")) { model.Reserva.ClienteId = User.Id(); ModelState.Remove("Reserva.ClienteId"); }
        if (ModelState.IsValid)
        {
            var resultado = await reservas.CrearAsync(model.Reserva, EncargadoUsuarioId, ct, User.Id());
            if (resultado.Exito)
            {
                TempData["Mensaje"] = "Reserva registrada correctamente con su proforma. Puedes consultarla en Facturación.";
                return RedirectToAction(nameof(Detalle), new { id = resultado.CitaId });
            }
            ModelState.AddModelError("", resultado.Error ?? "No se pudo registrar la reserva.");
        }
        if (!await CargarFormulario(model, ct)) return NotFound();
        // Se reconstruyen los identificadores de preguntas desde la BD, preservando solo sus respuestas.
        model.Reserva.Respuestas = model.Formulario!.Preguntas.Select(p => new CrearRespuestaReservaDTO {
            PreguntaId = p.PreguntaId, Respuesta = model.Reserva.Respuestas.FirstOrDefault(r => r.PreguntaId == p.PreguntaId)?.Respuesta ?? "" }).ToList();
        foreach (var key in ModelState.Keys.Where(k => k.EndsWith(".PreguntaId", StringComparison.Ordinal)).ToArray()) ModelState.Remove(key);
        return View(model);
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole("Cliente")) return View(await reservas.ListarClienteAsync(User.Id(), cancellationToken));
        var historial = await reservas.ListarAsync(EncargadoUsuarioId, cancellationToken);
        return historial is null ? NotFound("El usuario no tiene acceso al historial de reservas.") : View(historial);
    }

    public async Task<IActionResult> Detalle(long id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var reserva = User.IsInRole("Cliente") ? await reservas.ObtenerClienteAsync(id, User.Id(), cancellationToken)
            : await reservas.ObtenerAsync(id, EncargadoUsuarioId, cancellationToken);
        if (reserva is not null)
            ViewData["Vehiculos"] = await operaciones.VehiculosAsync(cancellationToken, reserva.ClienteId);
        return reserva is null ? NotFound() : View(reserva);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> Completar(long id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!await operaciones.CompletarCitaAsync(id, EncargadoUsuarioId, cancellationToken))
        {
            TempData["Mensaje"] = "No se pudo completar el servicio. Revisa el estado, el vehículo y los permisos; si otra operación está en curso, intenta nuevamente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        TempData["Mensaje"] = "Servicio completado: factura emitida y lavado acreditado en WashPoints.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> AsignarVehiculo(long id, int vehiculoId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!await operaciones.AsignarVehiculoAsync(id, vehiculoId, EncargadoUsuarioId, cancellationToken))
            return NotFound();
        TempData["Mensaje"] = "Vehículo actualizado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(long id, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!await operaciones.CancelarCitaAsync(id, User.Id(), ct)) return NotFound();
        TempData["Mensaje"] = "Reserva cancelada. Su proforma se conserva anulada.";
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
