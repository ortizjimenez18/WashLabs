using WashLabs.Models.DTOs;

namespace WashLabs.Services.Interfaces;

public interface IFacturaService
{
    Task<IReadOnlyList<FacturaResumenDTO>> ListarAsync(CancellationToken cancellationToken = default);
    Task<FacturaDTO?> ObtenerAsync(long id, CancellationToken cancellationToken = default);
}
