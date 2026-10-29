using WashLabs.Models.DTOs;

namespace WashLabs.ViewModels;

public sealed record ProductosIndexViewModel(IReadOnlyList<ProductoDTO> Productos,
    IReadOnlyList<CategoriaProductoDTO> Categorias, int? CategoriaId, string? Busqueda);
