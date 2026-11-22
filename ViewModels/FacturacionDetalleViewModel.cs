using WashLabs.Models.DTOs;

namespace WashLabs.ViewModels;

public sealed class FacturacionDetalleViewModel
{
    public required FacturaDTO Factura { get; init; }
    public bool PuedeEditar { get; init; }
    public bool MostrarProductos { get; init; }
    public string? VolverUrl { get; init; }
    public string? Cotizacion { get; init; }
    public IReadOnlyList<ProductoDTO> Productos { get; init; } = [];
    public IReadOnlyList<ServicioReservaDTO> Servicios { get; init; } = [];
}
