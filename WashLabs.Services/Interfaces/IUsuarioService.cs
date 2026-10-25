using WashLabs.Models.DTOs;

namespace WashLabs.Services.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDTO>> ListarAsync(CancellationToken ct = default);
    Task<UsuarioDTO?> ObtenerAsync(int id, CancellationToken ct = default);
}
