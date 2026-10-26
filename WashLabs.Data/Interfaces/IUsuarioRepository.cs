using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Data.Interfaces;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct);
    Task<Usuario?> ObtenerAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Rol>> RolesAdministrablesAsync(CancellationToken ct);
    Task<string?> CambiarRolAsync(int usuarioId, int rolId, int administradorId,
        Func<ContextoCambioRolDTO, string?> validar, CancellationToken ct);
}
