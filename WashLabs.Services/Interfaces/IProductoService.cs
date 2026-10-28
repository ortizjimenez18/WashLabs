using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Services.Interfaces;

public interface IProductoService
{
    Task<ProductoFormulario?> FormularioAsync(int? id, CancellationToken ct = default);
    Task CargarCategoriasAsync(ProductoFormulario f, CancellationToken ct = default);
    Task<string?> GuardarAsync(ProductoFormulario f, bool crear, string? nuevaImagen, CancellationToken ct = default);
    Task<IReadOnlyList<ProductoDTO>> ListarTodosAsync(CancellationToken cancellationToken = default);
    Task<ProductoDTO?> ObtenerMantenimientoAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductoDTO>> ListarAsync(int? categoriaId, string? busqueda, CancellationToken cancellationToken = default);
    Task<ProductoDTO?> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoriaProductoDTO>> ListarCategoriasAsync(CancellationToken cancellationToken = default);
}
