using WashLabs.Data.Entities;

namespace WashLabs.Data.Interfaces;

public interface IServicioRepository
{
    Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken ct);
    Task<Servicio?> ObtenerAsync(int id, CancellationToken ct);
    Task<bool> NombreExisteAsync(string nombre, int exceptoId, CancellationToken ct);
    Task<bool> GuardarAsync(Servicio servicio, bool crear, CancellationToken ct);
}
