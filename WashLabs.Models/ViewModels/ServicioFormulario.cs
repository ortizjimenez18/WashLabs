using System.ComponentModel.DataAnnotations;

namespace WashLabs.Models.ViewModels;

public sealed class ServicioFormulario
{
    public int ServicioId { get; set; }
    [Required(ErrorMessage = "Ingrese el nombre."), StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Ingrese la descripción."), StringLength(700, ErrorMessage = "La descripción admite hasta 700 caracteres.")]
    public string Descripcion { get; set; } = "";
    [Display(Name = "Tarifa base"), Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Ingrese una tarifa mayor o igual a cero.")]
    public decimal TarifaBase { get; set; }
    [Display(Name = "Duración en minutos"), Range(1, int.MaxValue, ErrorMessage = "La duración debe ser mayor que cero, en minutos.")]
    public int DuracionMinutos { get; set; } = 30;
    [Required(ErrorMessage = "Ingrese el tipo de lavado."), StringLength(30, ErrorMessage = "El tipo de lavado admite hasta 30 caracteres.")]
    public string TipoLavado { get; set; } = "";
    [Required(ErrorMessage = "Seleccione el nivel de detalle."), RegularExpression("^(BASICO|INTERMEDIO|PROFUNDO|PREMIUM)$", ErrorMessage = "Seleccione un nivel de detalle válido.")]
    public string NivelDetalle { get; set; } = "BASICO";
    public bool Activo { get; set; } = true;
    public string? ImagenUrl { get; set; }
}
