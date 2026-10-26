using System.ComponentModel.DataAnnotations;
using WashLabs.Models.DTOs;

namespace WashLabs.Models.ViewModels;

public sealed class SucursalFormulario
{
    public int SucursalId { get; set; }
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "La descripción es obligatoria."), StringLength(500), Display(Name = "Descripción")]
    public string Descripcion { get; set; } = "";
    [Required(ErrorMessage = "El teléfono es obligatorio."), StringLength(25), Display(Name = "Teléfono")]
    [RegularExpression(@"^(?:\+506[ -]?)?[2-9]\d{3}[ -]?\d{4}$", ErrorMessage = "Ingrese un teléfono de Costa Rica de 8 dígitos, con prefijo +506 opcional.")]
    public string Telefono { get; set; } = "";
    [Required(ErrorMessage = "La dirección es obligatoria."), StringLength(300), Display(Name = "Dirección exacta")]
    public string Direccion { get; set; } = "";
    [Required(ErrorMessage = "El correo es obligatorio."), StringLength(150), EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido."), Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = "";
    [Display(Name = "Usuarios encargados asignados")]
    public List<int> EncargadoIds { get; set; } = [];
    public IReadOnlyList<EncargadoSucursalDTO> Disponibles { get; set; } = [];
}
