namespace WashLabs.Models.DTOs;

public sealed record AgendaSucursalDTO(int SucursalId, string Nombre, bool Activa);
public sealed record HorarioAdministracionDTO(int Id, int SucursalId, string Sucursal, byte DiaSemana,
    DateOnly Desde, DateOnly? Hasta, TimeOnly Inicio, TimeOnly Fin, bool Activo);
public sealed record BloqueoAdministracionDTO(long Id, int SucursalId, string Sucursal, DateOnly Fecha,
    TimeOnly? Inicio, TimeOnly? Fin, bool DiaCompleto, string Motivo, string CreadoPor);
public sealed record IntervaloAgendaDTO(TimeOnly Inicio, TimeOnly Fin);
public sealed record ContextoAgendaDTO(bool SucursalActiva, bool AdministradorActivo,
    IReadOnlyList<IntervaloAgendaDTO> Horarios, IReadOnlyList<IntervaloAgendaDTO> Bloqueos,
    IReadOnlyList<IntervaloAgendaDTO> Citas);
public sealed record ResultadoAgendaDTO(long? Id, string? Error);
