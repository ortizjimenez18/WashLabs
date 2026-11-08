using Microsoft.AspNetCore.Mvc;
using WashLabs.Services.Interfaces;
using WashLabs.ViewModels;
using Microsoft.AspNetCore.Authorization;
using WashLabs.Models;
namespace WashLabs.Controllers;

[Authorize(Roles = "Cliente,Encargado,Administrador")]
public sealed class VehiculosController(IOperacionService operaciones) : Controller
{
    private int? ClienteActual => User.IsInRole("Cliente") ? User.Id() : null;
    private async Task<IReadOnlyList<WashLabs.Models.DTOs.ClienteOpcionDTO>> Clientes(CancellationToken ct) =>
        (await operaciones.ClientesAsync(ct)).Where(c => !ClienteActual.HasValue || c.ClienteId == ClienteActual).ToArray();
    public async Task<IActionResult> Index(CancellationToken ct) => View(await operaciones.VehiculosAsync(ct, ClienteActual));
    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct) =>
        await operaciones.VehiculoAsync(id, ClienteActual, ct) is { } vehiculo ? View(vehiculo) : NotFound();

    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var actual = await operaciones.VehiculoAsync(id, ClienteActual, ct);
        if (actual is null) return NotFound();
        ViewData["VehiculoId"] = id;
        return View("Crear", new RegistroVehiculoViewModel {
            Clientes = [new(actual.ClienteId, actual.Cliente)],
            Vehiculo = new() { ClienteId = actual.ClienteId, Matricula = actual.Matricula,
                Marca = actual.Marca, Modelo = actual.Modelo, Anio = actual.Anio,
                Color = actual.Color, CategoriaTamano = actual.CategoriaTamano } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, RegistroVehiculoViewModel model, CancellationToken ct)
    {
        var actual = await operaciones.VehiculoAsync(id, ClienteActual, ct);
        if (actual is null) return NotFound();
        model.Vehiculo.ClienteId = actual.ClienteId;
        ModelState.Remove("Vehiculo.ClienteId");
        if (ModelState.IsValid)
        {
            var resultado = await operaciones.EditarVehiculoAsync(id, model.Vehiculo, ClienteActual, ct);
            if (resultado.Exito)
            {
                TempData["Mensaje"] = "Vehículo actualizado correctamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            ModelState.AddModelError("", resultado.Error ?? "No se pudo actualizar el vehículo.");
        }
        ViewData["VehiculoId"] = id;
        model.Clientes = [new(actual.ClienteId, actual.Cliente)];
        return View("Crear", model);
    }
    [HttpGet]
    public async Task<IActionResult> Crear(CancellationToken ct) =>
        View(new RegistroVehiculoViewModel { Clientes = await Clientes(ct), Vehiculo = new() { ClienteId = ClienteActual ?? 0 } });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(RegistroVehiculoViewModel model, CancellationToken ct)
    {
        if (ClienteActual.HasValue) { model.Vehiculo.ClienteId = ClienteActual.Value; ModelState.Remove("Vehiculo.ClienteId"); }
        if (ModelState.IsValid)
        {
            var resultado = await operaciones.RegistrarVehiculoAsync(model.Vehiculo, ct);
            if (resultado.Exito)
            {
                TempData["Mensaje"] = "Vehículo registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", resultado.Error ?? "No se pudo registrar el vehículo.");
        }
        model.Clientes = await Clientes(ct);
        return View(model);
    }
}
