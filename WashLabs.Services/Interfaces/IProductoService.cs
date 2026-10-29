using WashLabs.Models.DTOs;

namespace WashLabs.Services.Interfaces;

public interface IProductoService
{
    Task<IReadOnlyList<ProductoDTO>> ListarTodosAsync(CancellationToken cancellationToken = default);
    Task<ProductoDTO?> ObtenerMantenimientoAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductoDTO>> ListarAsync(int? categoriaId, string? busqueda, CancellationToken cancellationToken = default);
    Task<ProductoDTO?> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoriaProductoDTO>> ListarCategoriasAsync(CancellationToken cancellationToken = default);
}
