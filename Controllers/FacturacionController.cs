using Microsoft.AspNetCore.Mvc;
using WashLabs.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WashLabs.Models;

namespace WashLabs.Controllers;

[Authorize]
public sealed class FacturacionController(IFacturaService facturas) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var lista = await facturas.ListarAsync(cancellationToken);
        if (User.IsInRole("Cliente")) lista = lista.Where(f => f.ClienteId == User.Id()).ToArray();
        if (User.IsInRole("Encargado")) lista = lista.Where(f => f.SucursalId.ToString() == User.FindFirstValue("SucursalId")).ToArray();
        return View(lista);
    }

    public async Task<IActionResult> Detalle(long id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var factura = await facturas.ObtenerAsync(id, cancellationToken);
        if (factura is not null && (User.IsInRole("Cliente") && factura.ClienteId != User.Id()
            || User.IsInRole("Encargado") && factura.Sucursal.SucursalId.ToString() != User.FindFirstValue("SucursalId"))) return NotFound();
        return factura is null ? NotFound() : View(factura);
    }
}
