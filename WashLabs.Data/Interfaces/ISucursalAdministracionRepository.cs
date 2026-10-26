using WashLabs.Data.Entities;

namespace WashLabs.Data.Interfaces;

public interface ISucursalAdministracionRepository
{
    Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken ct);
    Task<Sucursal?> ObtenerAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Usuario>> EncargadosAsync(int? sucursalId, CancellationToken ct);
    Task<string?> GuardarAsync(Sucursal sucursal, IReadOnlyList<int> encargados, bool crear, CancellationToken ct);
}
