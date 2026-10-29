using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Services.Mappers;

public static class UsuarioMapper
{
    public static UsuarioDTO ToDTO(Usuario u) => new(u.UsuarioId, u.NombreCompleto, u.Rol.Nombre,
        u.Correo, u.Activo, u.Telefono, u.FechaRegistro, u.SucursalId, u.Sucursal?.Nombre);
    public static RolAdministrableDTO ToDTO(Rol r) => new(r.RolId, r.Nombre);
}
