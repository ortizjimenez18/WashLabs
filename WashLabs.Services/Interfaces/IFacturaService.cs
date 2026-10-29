using WashLabs.Models.DTOs;

namespace WashLabs.Services.Interfaces;

public interface IFacturaService
{
    Task<bool> AgregarProductoAsync(long id, int productoId, decimal cantidad, int usuarioId, CancellationToken ct);
    Task<bool> CambiarCantidadAsync(long id, long detalleId, decimal cantidad, int usuarioId, CancellationToken ct);
    Task<bool> QuitarProductoAsync(long id, long detalleId, int usuarioId, CancellationToken ct);
    Task<HistorialFacturacionDTO> ConsultarAsync(FiltroFacturacionDTO filtro, int usuarioId, CancellationToken ct);
    Task<HistorialFacturacionDTO> OpcionesAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<FacturaResumenDTO>> ListarAsync(CancellationToken cancellationToken, int usuarioId);
    Task<FacturaDTO?> ObtenerAsync(long id, CancellationToken cancellationToken, int usuarioId);
}
