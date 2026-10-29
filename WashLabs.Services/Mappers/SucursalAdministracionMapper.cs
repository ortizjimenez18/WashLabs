using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Services.Mappers;

public static class SucursalAdministracionMapper
{
    public static EncargadoSucursalDTO ToDTO(Usuario u) => new(u.UsuarioId, u.NombreCompleto, u.SucursalId, u.Activo);
    public static SucursalAdministracionDTO ToDTO(Sucursal s) => new(s.SucursalId, s.Nombre, s.Descripcion,
        s.Telefono, s.Direccion, s.Correo, s.Activa, s.Usuarios.Select(ToDTO).ToList());
    public static Sucursal ToEntity(SucursalFormulario f) => new() { SucursalId = f.SucursalId,
        Nombre = f.Nombre.Trim(), Descripcion = f.Descripcion.Trim(), Telefono = f.Telefono.Trim(),
        Direccion = f.Direccion.Trim(), Correo = f.Correo.Trim() };
    public static SucursalFormulario ToFormulario(SucursalAdministracionDTO s) => new() { SucursalId = s.SucursalId,
        Nombre = s.Nombre, Descripcion = s.Descripcion, Telefono = s.Telefono, Direccion = s.Direccion,
        Correo = s.Correo, EncargadoIds = s.Encargados.Select(u => u.UsuarioId).ToList() };
}
