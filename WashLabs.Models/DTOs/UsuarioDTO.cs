namespace WashLabs.Models.DTOs;

// Contrato de lectura: nunca contiene credenciales ni datos de sesión.
public sealed record UsuarioDTO(int UsuarioId, string NombreCompleto, string Rol,
    string Correo, bool Activo, string Telefono, DateTime FechaRegistro,
    int? SucursalId = null, string? Sucursal = null);
