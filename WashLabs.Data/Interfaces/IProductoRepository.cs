using WashLabs.Data.Entities;

namespace WashLabs.Data.Interfaces;

public interface IProductoRepository
{
    Task<IReadOnlyList<Producto>> ListarTodosAsync(CancellationToken cancellationToken);
    Task<Producto?> ObtenerAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Producto>> ListarActivosAsync(int? categoriaId, string? busqueda, CancellationToken cancellationToken);
    Task<Producto?> ObtenerActivoAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoriaProducto>> ListarCategoriasAsync(CancellationToken cancellationToken);
}
