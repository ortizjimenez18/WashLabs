using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Services.Mappers;

public static class AgendaAdministracionMapper
{
    public static AgendaSucursalDTO ToDTO(Sucursal s)=>new(s.SucursalId,s.Nombre,s.Activa);
    public static HorarioAdministracionDTO ToDTO(HorarioSucursal h)=>new(h.HorarioSucursalId,h.SucursalId,h.Sucursal.Nombre,h.DiaSemana,h.FechaVigenciaDesde,h.FechaVigenciaHasta,h.HoraInicio,h.HoraFin,h.Activo);
    public static BloqueoAdministracionDTO ToDTO(BloqueoAgenda b)=>new(b.BloqueoAgendaId,b.SucursalId,b.Sucursal.Nombre,b.Fecha,b.HoraInicio,b.HoraFin,b.DiaCompleto,b.Motivo,b.CreadoPorUsuario.NombreCompleto);
}
