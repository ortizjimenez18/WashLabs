using WashLabs.Data.Entities;

namespace WashLabs.Data.Interfaces;

public interface IInicioRepository
{
    Task<ConfiguracionSistema?> ObtenerNegocioAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Servicio>> ListarServiciosAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Sucursal>> ListarSucursalesAsync(CancellationToken cancellationToken);
}
