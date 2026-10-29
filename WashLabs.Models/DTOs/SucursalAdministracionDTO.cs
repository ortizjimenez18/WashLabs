namespace WashLabs.Models.DTOs;

public sealed record EncargadoSucursalDTO(int UsuarioId, string Nombre, int? SucursalId, bool Activo);
public sealed record EncargadoElegibilidadDTO(int UsuarioId, string Rol, int? SucursalId, bool Activo);
public sealed record SucursalAdministracionDTO(int SucursalId, string Nombre, string Descripcion,
    string Telefono, string Direccion, string Correo, bool Activa, IReadOnlyList<EncargadoSucursalDTO> Encargados);
