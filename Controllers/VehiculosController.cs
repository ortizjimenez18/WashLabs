using Microsoft.AspNetCore.Mvc;
using WashLabs.Services.Interfaces;
using WashLabs.ViewModels;
using Microsoft.AspNetCore.Authorization;
using WashLabs.Models;
namespace WashLabs.Controllers;

[Authorize]
public sealed class VehiculosController(IOperacionService operaciones) : Controller
{
    private int? ClienteActual => User.IsInRole("Cliente") ? User.Id() : null;
    private async Task<IReadOnlyList<WashLabs.Models.DTOs.ClienteOpcionDTO>> Clientes(CancellationToken ct) =>
        (await operaciones.ClientesAsync(ct)).Where(c => !ClienteActual.HasValue || c.ClienteId == ClienteActual).ToArray();
    public async Task<IActionResult> Index(CancellationToken ct) => View(await operaciones.VehiculosAsync(ct, ClienteActual));
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
