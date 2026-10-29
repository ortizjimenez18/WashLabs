using System.ComponentModel.DataAnnotations;
using WashLabs.Models.DTOs;

namespace WashLabs.Models.ViewModels;

public sealed class AgendaFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sucursal."), Display(Name = "Sucursal")]
    public int SucursalId { get; set; }
    [Required(ErrorMessage = "La fecha es obligatoria."), DataType(DataType.Date)]
    public DateOnly? Fecha { get; set; }
    [Required(ErrorMessage = "La hora inicial es obligatoria."), DataType(DataType.Time), Display(Name = "Hora inicial")]
    public TimeOnly? HoraInicio { get; set; }
    [Required(ErrorMessage = "La hora final es obligatoria."), DataType(DataType.Time), Display(Name = "Hora final")]
    public TimeOnly? HoraFin { get; set; }
    [StringLength(300, ErrorMessage = "El motivo admite hasta 300 caracteres.")]
    public string? Motivo { get; set; }
    public bool EsBloqueo { get; set; }
    public IReadOnlyList<AgendaSucursalDTO> Sucursales { get; set; } = [];
}
public sealed class AgendaListado
{
    public int? SucursalId { get; set; }
    public IReadOnlyList<AgendaSucursalDTO> Sucursales { get; set; } = [];
    public IReadOnlyList<HorarioAdministracionDTO> Horarios { get; set; } = [];
    public IReadOnlyList<BloqueoAdministracionDTO> Bloqueos { get; set; } = [];
}
