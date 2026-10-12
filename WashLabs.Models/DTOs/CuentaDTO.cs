using System.ComponentModel.DataAnnotations;
namespace WashLabs.Models.DTOs;

public sealed class RegistroCuentaDTO
{
    [Required(ErrorMessage = "Indica tu nombre."), StringLength(150)] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Indica tu identificación."), StringLength(30)] public string Identificacion { get; set; } = "";
    [Required(ErrorMessage = "Indica tu teléfono."), StringLength(25)] public string Telefono { get; set; } = "";
    [Required(ErrorMessage = "Indica tu correo."), EmailAddress(ErrorMessage = "Indica un correo válido."), StringLength(150)] public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Indica tu dirección."), StringLength(300)] public string Direccion { get; set; } = "";
    [Required(ErrorMessage = "Indica tu fecha de nacimiento.")] public DateOnly? FechaNacimiento { get; set; }
    [Required(ErrorMessage = "Indica una contraseña."), StringLength(128, MinimumLength = 12, ErrorMessage = "Usa una contraseña de entre 12 y 128 caracteres.")]
    public string Clave { get; set; } = "";
    [Compare(nameof(Clave), ErrorMessage = "Las contraseñas no coinciden.")] public string ConfirmarClave { get; set; } = "";
}
public sealed record UsuarioSesionDTO(int UsuarioId, string Nombre, string Correo, string Rol, int? SucursalId, string VersionAcceso);
public sealed record RegistroCuentaResultadoDTO(bool Exito, string? Error);
