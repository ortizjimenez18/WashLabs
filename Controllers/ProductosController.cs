using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WashLabs.Services.Interfaces;
using WashLabs.ViewModels;
using WashLabs.Models.ViewModels;
using WashLabs.Services;

namespace WashLabs.Controllers;

public sealed class ProductosController(IProductoService productos, IImagenService imagenes) : Controller
{
    [Authorize(Roles = "Encargado,Administrador")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Crear(CancellationToken ct) => View("Formulario", await productos.FormularioAsync(null, ct));
    [Authorize(Roles = "Encargado,Administrador")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Editar(int id, CancellationToken ct) =>
        await productos.FormularioAsync(id, ct) is { } f ? View("Formulario", f) : NotFound();
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador"), RequestSizeLimit(3 * 1024 * 1024)]
    public Task<IActionResult> Crear(ProductoFormulario form, IFormFile? imagen, CancellationToken ct)
    { form.ProductoId = 0; return Guardar(form, true, imagen, ct); }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Encargado,Administrador"), RequestSizeLimit(3 * 1024 * 1024)]
    public Task<IActionResult> Editar(int id, ProductoFormulario form, IFormFile? imagen, CancellationToken ct) =>
        id != form.ProductoId ? Task.FromResult<IActionResult>(BadRequest()) : Guardar(form, false, imagen, ct);
    private async Task<IActionResult> Guardar(ProductoFormulario f, bool crear, IFormFile? imagen, CancellationToken ct)
    {
        var actual = crear ? null : await productos.ObtenerMantenimientoAsync(f.ProductoId, ct);
        if (!crear && actual is null) return NotFound();
        f.ImagenUrl = actual?.ImagenUrl;
        string? nueva = null;
        var guardada = false;
        try
        {
            if (ModelState.IsValid)
            {
                nueva = await imagenes.GuardarAsync(imagen, ct);
                var error = await productos.GuardarAsync(f, crear, nueva, ct);
                if (error is null)
                {
                    guardada = true; TempData["Exito"] = crear ? "Producto creado correctamente." : "Producto actualizado correctamente.";
                    return RedirectToAction(nameof(Mantenimiento));
                }
                ModelState.AddModelError("", error);
            }
        }
        catch (ArgumentException ex) { ModelState.AddModelError("imagen", ex.Message); }
        catch (IOException) { ModelState.AddModelError("imagen", "No se pudo guardar la imagen. Intente nuevamente."); }
        finally { if (!guardada) imagenes.EliminarNueva(nueva); }
        await productos.CargarCategoriasAsync(f, ct);
        return View("Formulario", f);
    }
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
