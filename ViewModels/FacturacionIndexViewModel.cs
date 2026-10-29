using WashLabs.Models.DTOs;

namespace WashLabs.ViewModels;

public sealed class FacturacionIndexViewModel
{
    public FiltroFacturacionDTO Filtro { get; set; } = new();
    public HistorialFacturacionDTO Historial { get; set; } = new([], new(0, 0, 0m), [], []);
    public bool MostrarSucursal { get; set; }
}
