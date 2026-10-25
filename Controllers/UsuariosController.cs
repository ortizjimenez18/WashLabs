using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
