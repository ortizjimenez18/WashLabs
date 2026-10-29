using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;

namespace WashLabs.Services.Implementations;

public sealed class ProductoService(IProductoRepository repository) : IProductoService
{
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
