using System.ComponentModel.DataAnnotations;
namespace WashLabs.ViewModels;
public sealed class EntrarViewModel
{
    [Required(ErrorMessage = "Indica tu correo."), EmailAddress(ErrorMessage = "Indica un correo válido."), StringLength(150)]
    public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Indica tu contraseña."), StringLength(128)] public string Clave { get; set; } = "";
    public bool Recordarme { get; set; }
    public string? ReturnUrl { get; set; }
}
