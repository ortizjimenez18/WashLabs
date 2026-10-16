using WashLabs.Models.DTOs;

namespace WashLabs.ViewModels;

public sealed class FacturacionDetalleViewModel
{
    public required FacturaDTO Factura { get; init; }
    public bool PuedeEditar { get; init; }
    public bool MostrarProductos { get; init; }
    public string? BusquedaProducto { get; init; }
    public int? CategoriaProductoId { get; init; }
    public string? VolverUrl { get; init; }
    public IReadOnlyList<ProductoDTO> Productos { get; init; } = [];
    public IReadOnlyList<CategoriaProductoDTO> Categorias { get; init; } = [];
}
