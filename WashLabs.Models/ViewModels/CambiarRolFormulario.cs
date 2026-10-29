using System.ComponentModel.DataAnnotations;
using WashLabs.Models.DTOs;

namespace WashLabs.Models.ViewModels;

public sealed class CambiarRolFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un usuario válido.")]
    public int UsuarioId { get; set; }
    [Range(1, byte.MaxValue, ErrorMessage = "Seleccione un rol válido."), Display(Name = "Nuevo rol")]
    public int RolId { get; set; }
    [Range(1, byte.MaxValue, ErrorMessage = "El rol actual no es válido.")]
    public int RolActualId { get; set; }
    public int? SucursalActualId { get; set; }
    public UsuarioDTO? Usuario { get; set; }
    public IReadOnlyList<RolAdministrableDTO> Roles { get; set; } = [];
}
