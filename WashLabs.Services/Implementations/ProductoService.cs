using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;
using WashLabs.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace WashLabs.Services.Implementations;

public sealed class ProductoService(IProductoRepository repository) : IProductoService
{
    public async Task<ProductoFormulario?> FormularioAsync(int? id, CancellationToken ct = default)
    {
        ProductoFormulario f;
        if (id is null) f = new();
        else
        {
            var p = await repository.ObtenerAsync(id.Value, ct);
            if (p is null) return null;
            f = ProductoMapper.ToFormulario(p);
        }
        await CargarCategoriasAsync(f, ct);
        return f;
    }
    public async Task CargarCategoriasAsync(ProductoFormulario f, CancellationToken ct = default) =>
        f.Categorias = (await repository.ListarCategoriasMantenimientoAsync(ct)).Select(ProductoMapper.ToDTO).ToArray();
    public async Task<string?> GuardarAsync(ProductoFormulario f, bool crear, string? nuevaImagen, CancellationToken ct = default)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(f, new ValidationContext(f), errores, true)) return errores[0].ErrorMessage;
        if (decimal.Round(f.Precio, 2) != f.Precio) return "El precio admite hasta dos decimales.";
        if (!await repository.CategoriaExisteAsync(f.CategoriaProductoId, ct)) return "La categoría seleccionada no existe.";
        if (!string.IsNullOrWhiteSpace(f.SKU) && await repository.SkuExisteAsync(f.SKU.Trim(), crear ? 0 : f.ProductoId, ct))
            return "Ya existe un producto con ese SKU.";
        var actual = crear ? null : await repository.ObtenerAsync(f.ProductoId, ct);
        if (!crear && actual is null) return "El producto ya no existe.";
        try
        {
            return await repository.GuardarAsync(ProductoMapper.ToEntity(f, nuevaImagen ?? actual?.ImagenUrl), crear, ct)
                ? null : "El producto ya no existe.";
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        { return "Ya existe un producto con ese SKU."; }
        catch (DbUpdateException) { return "No se pudo guardar el producto. Verifique los valores y la categoría seleccionada."; }
    }
    public async Task<IReadOnlyList<ProductoDTO>> ListarTodosAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListarTodosAsync(cancellationToken)).Select(ProductoMapper.ToDTO).ToArray();

    public async Task<ProductoDTO?> ObtenerMantenimientoAsync(int id, CancellationToken cancellationToken = default)
    {
        var producto = id > 0 ? await repository.ObtenerAsync(id, cancellationToken) : null;
        return producto is null ? null : ProductoMapper.ToDTO(producto);
    }
    public async Task<IReadOnlyList<ProductoDTO>> ListarAsync(int? categoriaId, string? busqueda, CancellationToken cancellationToken = default)
    {
        var productos = await repository.ListarActivosAsync(categoriaId, busqueda?.Trim(), cancellationToken);
        return productos.Select(ProductoMapper.ToDTO).ToArray();
    }

    public async Task<ProductoDTO?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return null;
        var producto = await repository.ObtenerActivoAsync(id, cancellationToken);
        return producto is null ? null : ProductoMapper.ToDTO(producto);
    }

    public async Task<IReadOnlyList<CategoriaProductoDTO>> ListarCategoriasAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListarCategoriasAsync(cancellationToken)).Select(ProductoMapper.ToDTO).ToArray();
}
