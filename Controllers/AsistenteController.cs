using Microsoft.AspNetCore.Mvc;
using WashLabs.Services.Interfaces;
namespace WashLabs.Controllers;
public sealed class AsistenteController(IOperacionService operaciones) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await operaciones.CategoriasAsistenteAsync(ct));
    [HttpGet]
    public async Task<IActionResult> Categorias(CancellationToken ct) =>
        Json(await operaciones.CategoriasAsistenteAsync(ct));

    [HttpGet]
    public async Task<IActionResult> Consulta(int id, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        var pregunta = await operaciones.PreguntaAsync(id, ct);
        return pregunta is null ? NotFound() : Json(pregunta);
    }
    public async Task<IActionResult> Pregunta(int id, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        var pregunta = await operaciones.PreguntaAsync(id, ct);
        return pregunta is null ? NotFound() : View(pregunta);
    }
}
