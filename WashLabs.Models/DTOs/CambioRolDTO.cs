namespace WashLabs.Models.DTOs;

public sealed record RolAdministrableDTO(byte RolId, string Nombre);
public sealed record ContextoCambioRolDTO(bool AdministradorActivo, int? UsuarioId, bool UsuarioActivo,
    byte RolActualId, string RolActual, int? SucursalId, byte? RolDestinoId, string? RolDestino);
