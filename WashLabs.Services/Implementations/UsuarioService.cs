using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;

namespace WashLabs.Services.Implementations;

public sealed class UsuarioService(IUsuarioRepository repository) : IUsuarioService
{
    private static UsuarioDTO Map(Usuario u) => new(u.UsuarioId, u.NombreCompleto,
        u.Rol.Nombre, u.Correo, u.Activo, u.Telefono, u.FechaRegistro);

    public async Task<IReadOnlyList<UsuarioDTO>> ListarAsync(CancellationToken ct = default) =>
        (await repository.ListarAsync(ct)).Select(Map).ToArray();

    public async Task<UsuarioDTO?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var usuario = id > 0 ? await repository.ObtenerAsync(id, ct) : null;
        return usuario is null ? null : Map(usuario);
    }
}
