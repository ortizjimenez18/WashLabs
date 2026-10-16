namespace WashLabs.Models.DTOs;

public sealed record EstadoReservaDTO(string Nombre, string ColorHex);
public sealed record ReservaResumenDTO(long CitaId, DateOnly Fecha, TimeOnly HoraInicio,
    string Cliente, string Servicio, EstadoReservaDTO Estado);
public sealed record ReservasHistorialDTO(string Encargado, string Sucursal,
    IReadOnlyList<ReservaResumenDTO> Reservas, bool EsCliente = false);
public sealed record RespuestaReservaDTO(string Pregunta, string Respuesta);
public sealed record ReservaDTO(long CitaId, DateTime FechaCreacion, DateOnly Fecha,
    TimeOnly HoraInicio, TimeOnly HoraFin, string Sucursal, string Servicio,
    EstadoReservaDTO Estado, IReadOnlyList<RespuestaReservaDTO> Respuestas, string Matricula, int ClienteId, long? FacturaId = null);
