using Microsoft.AspNetCore.Mvc;
using WashLabs.Services.Interfaces;
using WashLabs.ViewModels;
using Microsoft.AspNetCore.Authorization;
using WashLabs.Models;
namespace WashLabs.Controllers;
[Authorize]
public sealed class WashPointsController(IOperacionService operaciones) : Controller
{
    public async Task<IActionResult> Index(int? clienteId, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        var clientes = await operaciones.ClientesAsync(ct);
        if (User.IsInRole("Cliente")) { clienteId = User.Id(); clientes = clientes.Where(c => c.ClienteId == clienteId).ToArray(); }
        if (!clienteId.HasValue && clientes.Count == 1) clienteId = clientes[0].ClienteId;
        if (clienteId.HasValue && !clientes.Any(c => c.ClienteId == clienteId.Value)) return NotFound();
        var puntos = clienteId.HasValue ? await operaciones.WashPointsAsync(ct, clienteId) : [];
        return View(new WashPointsIndexViewModel(clientes, clienteId, puntos));
    }
}
