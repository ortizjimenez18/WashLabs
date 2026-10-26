using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Interfaces;

namespace WashLabs.Controllers;

[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SucursalesController(ISucursalAdministracionService service) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await service.ListarAsync(ct));
    public async Task<IActionResult> Detalle(int id, CancellationToken ct) =>
        await service.ObtenerAsync(id, ct) is { } s ? View(s) : NotFound();
    public async Task<IActionResult> Crear(CancellationToken ct) => View("Formulario", await service.FormularioAsync(null, ct));
    public async Task<IActionResult> Editar(int id, CancellationToken ct) =>
        await service.FormularioAsync(id, ct) is { } f ? View("Formulario", f) : NotFound();
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Crear(SucursalFormulario form, CancellationToken ct) { form.SucursalId = 0; return Guardar(form, true, ct); }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Editar(int id, SucursalFormulario form, CancellationToken ct)
    { if (id != form.SucursalId) return Task.FromResult<IActionResult>(BadRequest()); return Guardar(form, false, ct); }
    private async Task<IActionResult> Guardar(SucursalFormulario f, bool crear, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var error = await service.GuardarAsync(f, crear, ct);
            if (error is null) { TempData["Exito"] = crear ? "Sucursal creada correctamente." : "Sucursal actualizada correctamente."; return RedirectToAction(nameof(Index)); }
            ModelState.AddModelError("", error);
        }
        await service.CargarEncargadosAsync(f, ct); return View("Formulario", f);
    }
}
