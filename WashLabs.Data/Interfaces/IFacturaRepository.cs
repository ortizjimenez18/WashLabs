using WashLabs.Data.Entities;

namespace WashLabs.Data.Interfaces;

public interface IFacturaRepository
{
    Task<IReadOnlyList<Factura>> ListarAsync(CancellationToken cancellationToken);
    Task<Factura?> ObtenerAsync(long id, CancellationToken cancellationToken);
}
