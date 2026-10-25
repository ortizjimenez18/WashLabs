using WashLabs.Data.Entities;

namespace WashLabs.Data.Interfaces;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct);
    Task<Usuario?> ObtenerAsync(int id, CancellationToken ct);
}
