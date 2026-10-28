using System.ComponentModel.DataAnnotations;
using WashLabs.Models.DTOs;

namespace WashLabs.Models.ViewModels;

public sealed class ProductoFormulario
{
    public int ProductoId { get; set; }
    [Required(ErrorMessage = "Ingrese el nombre."), StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Ingrese la descripción."), StringLength(700, ErrorMessage = "La descripción admite hasta 700 caracteres.")]
    public string Descripcion { get; set; } = "";
    [Display(Name = "Categoría"), Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría válida.")]
    public int CategoriaProductoId { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "El precio debe ser mayor o igual a cero.")]
    public decimal Precio { get; set; }
    [StringLength(80, ErrorMessage = "La marca admite hasta 80 caracteres.")]
    public string? Marca { get; set; }
    [Display(Name = "Presentación"), StringLength(100, ErrorMessage = "La presentación admite hasta 100 caracteres.")]
    public string? Presentacion { get; set; }
    [StringLength(50, ErrorMessage = "El SKU admite hasta 50 caracteres."), RegularExpression(@"[A-Za-z0-9._/-]+", ErrorMessage = "El SKU admite letras sin acentos, números, puntos, guiones y barras.")]
    public string? SKU { get; set; }
    public bool Activo { get; set; } = true;
    public string? ImagenUrl { get; set; }
    public IReadOnlyList<CategoriaProductoDTO> Categorias { get; set; } = [];
}
