using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WashLabs.Models.ViewModels;
using WashLabs.Services;
using WashLabs.Services.Interfaces;

namespace WashLabs.Controllers;

[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ServiciosController(IServicioService service, IImagenService imagenes) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await service.ListarAsync(ct));
    public async Task<IActionResult> Detalle(int id, CancellationToken ct) =>
        await service.ObtenerAsync(id, ct) is { } s ? View(s) : NotFound();
    public IActionResult Crear() => View("Formulario", new ServicioFormulario());
    public async Task<IActionResult> Editar(int id, CancellationToken ct) =>
        await service.FormularioAsync(id, ct) is { } f ? View("Formulario", f) : NotFound();
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public Task<IActionResult> Crear(ServicioFormulario form, IFormFile? imagen, CancellationToken ct)
    { form.ServicioId = 0; return Guardar(form, true, imagen, ct); }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public Task<IActionResult> Editar(int id, ServicioFormulario form, IFormFile? imagen, CancellationToken ct) =>
        id != form.ServicioId ? Task.FromResult<IActionResult>(BadRequest()) : Guardar(form, false, imagen, ct);
    private async Task<IActionResult> Guardar(ServicioFormulario f, bool crear, IFormFile? imagen, CancellationToken ct)
    {
        var actual = crear ? null : await service.FormularioAsync(f.ServicioId, ct);
        if (!crear && actual is null) return NotFound();
        f.ImagenUrl = actual?.ImagenUrl;
        string? nueva = null;
        var guardada = false;
        try
        {
            if (ModelState.IsValid)
            {
                nueva = await imagenes.GuardarAsync(imagen, ct);
                var error = await service.GuardarAsync(f, crear, nueva, ct);
                if (error is null)
                {
                    guardada = true; TempData["Exito"] = crear ? "Servicio creado correctamente." : "Servicio actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", error);
            }
        }
        catch (ArgumentException ex) { ModelState.AddModelError("imagen", ex.Message); }
        catch (IOException) { ModelState.AddModelError("imagen", "No se pudo guardar la imagen. Intente nuevamente."); }
        finally { if (!guardada) imagenes.EliminarNueva(nueva); }
        return View("Formulario", f);
    }
}
