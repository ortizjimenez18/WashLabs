using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;

namespace WashLabs.Services.Implementations;

public sealed class FacturaService(IFacturaRepository repository) : IFacturaService
{
    public async Task<IReadOnlyList<FacturaResumenDTO>> ListarAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListarAsync(cancellationToken)).Select(FacturaMapper.ToResumenDTO).ToArray();

    public async Task<FacturaDTO?> ObtenerAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return null;
        var factura = await repository.ObtenerAsync(id, cancellationToken);
        return factura is null ? null : FacturaMapper.ToDTO(factura);
    }
}
