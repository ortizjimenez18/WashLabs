using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Interfaces;

namespace WashLabs.Controllers;

[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsuariosController(IUsuarioService service) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await service.ListarAsync(ct));

    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        var usuario = await service.ObtenerAsync(id, ct);
        return usuario is null ? NotFound() : View(usuario);
    }

    [HttpGet]
    public async Task<IActionResult> CambiarRol(int id, CancellationToken ct) =>
        await service.FormularioRolAsync(id, ct) is { } f ? View(f) : NotFound();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(int id, CambiarRolFormulario formulario, CancellationToken ct)
    {
        if (id != formulario.UsuarioId) return BadRequest();
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var administradorId)) return Forbid();
        if (ModelState.IsValid)
        {
            var error = await service.CambiarRolAsync(formulario, administradorId, ct);
            if (error is null)
            {
                TempData["Exito"] = "Rol actualizado correctamente. El usuario deberá iniciar sesión nuevamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            ModelState.AddModelError("", error);
        }
        await service.CargarFormularioRolAsync(formulario, ct);
        if (formulario.Usuario is null) return NotFound();
        return View(formulario);
    }
}
