using System.ComponentModel.DataAnnotations;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;

namespace WashLabs.Services.Implementations;

public sealed class UsuarioService(IUsuarioRepository repository) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioDTO>> ListarAsync(CancellationToken ct = default) =>
        (await repository.ListarAsync(ct)).Select(UsuarioMapper.ToDTO).ToArray();

    public async Task<UsuarioDTO?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var usuario = id > 0 ? await repository.ObtenerAsync(id, ct) : null;
        return usuario is null ? null : UsuarioMapper.ToDTO(usuario);
    }

    public async Task<CambiarRolFormulario?> FormularioRolAsync(int id, CancellationToken ct = default)
    {
        var usuario = id > 0 ? await repository.ObtenerAsync(id, ct) : null;
        if (usuario is null || !usuario.Activo || usuario.Rol.Nombre is not ("Cliente" or "Encargado")) return null;
        var f = new CambiarRolFormulario { UsuarioId = id, RolActualId = usuario.RolId,
            RolId = usuario.RolId, SucursalActualId = usuario.SucursalId };
        await CargarFormularioRolAsync(f, ct);
        return f;
    }

    public async Task CargarFormularioRolAsync(CambiarRolFormulario f, CancellationToken ct = default)
    {
        f.Usuario = await ObtenerAsync(f.UsuarioId, ct);
        f.Roles = (await repository.RolesAdministrablesAsync(ct)).Select(UsuarioMapper.ToDTO).ToList();
    }

    public async Task<string?> CambiarRolAsync(CambiarRolFormulario f, int administradorId, CancellationToken ct = default)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(f, new ValidationContext(f), errores, true))
            return string.Join(" ", errores.Select(e => e.ErrorMessage));
        return await repository.CambiarRolAsync(f.UsuarioId, f.RolId, administradorId, estado =>
        {
            if (!estado.AdministradorActivo) return "Solo un administrador activo puede cambiar roles.";
            if (estado.UsuarioId is null) return "El usuario no existe.";
            if (!estado.UsuarioActivo) return "El usuario debe estar activo para cambiar su rol.";
            if (estado.RolActual is not ("Cliente" or "Encargado")) return "No se permite modificar el rol de un administrador.";
            if (estado.RolDestino is not ("Cliente" or "Encargado")) return "Solo se permite seleccionar Cliente o Encargado.";
            if (estado.RolActualId != f.RolActualId || estado.SucursalId != f.SucursalActualId)
                return "El rol o la sucursal del usuario cambiaron. Vuelva a abrir el formulario antes de guardar.";
            if (estado.RolActualId == f.RolId) return "El usuario ya tiene el rol seleccionado.";
            return null;
        }, ct);
    }
}
