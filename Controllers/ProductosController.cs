using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WashLabs.Services.Interfaces;
using WashLabs.ViewModels;

namespace WashLabs.Controllers;

public sealed class ProductosController(IProductoService productos) : Controller
{
    [Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> Mantenimiento(CancellationToken cancellationToken) =>
        View(await productos.ListarTodosAsync(cancellationToken));

    [Authorize(Roles = "Encargado,Administrador")]
    public async Task<IActionResult> DetalleMantenimiento(int id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var producto = await productos.ObtenerMantenimientoAsync(id, cancellationToken);
        ViewData["Mantenimiento"] = true;
        return producto is null ? NotFound() : View("Detalle", producto);
    }
    public async Task<IActionResult> Index(int? categoriaId, string? busqueda, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        busqueda = busqueda?.Trim();
        var lista = await productos.ListarAsync(categoriaId, busqueda, cancellationToken);
        var categorias = await productos.ListarCategoriasAsync(cancellationToken);
        return View(new ProductosIndexViewModel(lista, categorias, categoriaId, busqueda));
    }

    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var producto = await productos.ObtenerAsync(id, cancellationToken);
        if (producto is not null) return View(producto);
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Mensaje"] = "El producto solicitado no existe o no está disponible.";
        return View("NoEncontrado");
    }
}
