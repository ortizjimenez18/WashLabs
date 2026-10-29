using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Data.Interfaces;

public interface IFacturaRepository
{
    Task<bool> AgregarProductoAsync(long id, int productoId, decimal cantidad, int usuarioId, CancellationToken ct);
    Task<bool> CambiarCantidadAsync(long id, long detalleId, decimal cantidad, int usuarioId, CancellationToken ct);
    Task<bool> QuitarProductoAsync(long id, long detalleId, int usuarioId, CancellationToken ct);
    Task<HistorialFacturacionDTO> ConsultarAsync(FiltroFacturacionDTO filtro, int usuarioId, CancellationToken ct);
    Task<HistorialFacturacionDTO> OpcionesAsync(int usuarioId, CancellationToken ct);
    Task<string?> NombreNegocioAsync(CancellationToken ct);
    Task<IReadOnlyList<Factura>> ListarAsync(CancellationToken cancellationToken, int usuarioId);
    Task<Factura?> ObtenerAsync(long id, CancellationToken cancellationToken, int usuarioId);
}
