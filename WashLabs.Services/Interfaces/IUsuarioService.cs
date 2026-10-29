using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Services.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDTO>> ListarAsync(CancellationToken ct = default);
    Task<UsuarioDTO?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<CambiarRolFormulario?> FormularioRolAsync(int id, CancellationToken ct = default);
    Task CargarFormularioRolAsync(CambiarRolFormulario formulario, CancellationToken ct = default);
    Task<string?> CambiarRolAsync(CambiarRolFormulario formulario, int administradorId, CancellationToken ct = default);
}
