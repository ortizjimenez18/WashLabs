using System.ComponentModel.DataAnnotations;
namespace WashLabs.Models.DTOs;

public sealed class CrearReservaDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una sucursal.")]
    public int SucursalId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un cliente.")]
    public int ClienteId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un vehículo.")]
    public int VehiculoId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un servicio.")]
    public int ServicioId { get; set; }
    [Required(ErrorMessage = "Selecciona una fecha.")]
    public DateOnly? Fecha { get; set; }
    [Required(ErrorMessage = "Selecciona un horario disponible.")]
    public TimeOnly? HoraInicio { get; set; }
    [StringLength(700, ErrorMessage = "Las observaciones no pueden superar los 700 caracteres.")]
    public string? Observaciones { get; set; }
    public List<CrearRespuestaReservaDTO> Respuestas { get; set; } = [];
}
public sealed class CrearRespuestaReservaDTO
{
    public int PreguntaId { get; set; }
    [Required(ErrorMessage = "Responde las preguntas de la reserva.")]
    [StringLength(700, ErrorMessage = "Cada respuesta admite hasta 700 caracteres.")]
    public string Respuesta { get; set; } = "";
}
public sealed record ServicioReservaDTO(int ServicioId, string Nombre, int DuracionMinutos);
public sealed record PreguntaReservaDTO(int PreguntaId, string Pregunta);
public sealed record FormularioReservaDTO(string Sucursal, IReadOnlyList<ServicioReservaDTO> Servicios,
    IReadOnlyList<PreguntaReservaDTO> Preguntas, int SucursalId, IReadOnlyList<SucursalReservaDTO> Sucursales);
public sealed record SucursalReservaDTO(int SucursalId, string Nombre);
public sealed record RangoReservaDTO(TimeOnly Inicio, TimeOnly Fin);
public sealed record AgendaReservaDTO(int DuracionMinutos, IReadOnlyList<RangoReservaDTO> Horarios,
    IReadOnlyList<RangoReservaDTO> Ocupados, bool DiaBloqueado);
public sealed record ResultadoCrearReservaDTO(bool Exito, long? CitaId, string? Error);
