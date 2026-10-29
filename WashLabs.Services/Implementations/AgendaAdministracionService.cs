using System.ComponentModel.DataAnnotations;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;

namespace WashLabs.Services.Implementations;

public sealed class AgendaAdministracionService(IAgendaAdministracionRepository repo) : IAgendaAdministracionService
{
    public static DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,"Central America Standard Time").DateTime);
    public async Task<AgendaListado> ListarAsync(int? id,CancellationToken ct=default)
    {
        var sucursales=(await repo.SucursalesAsync(ct)).Select(AgendaAdministracionMapper.ToDTO).ToList();
        return new() { SucursalId=id,Sucursales=sucursales,
            Horarios=id.HasValue ? (await repo.HorariosAsync(id.Value,ct)).Select(AgendaAdministracionMapper.ToDTO).ToList() : [],
            Bloqueos=id.HasValue ? (await repo.BloqueosAsync(id.Value,ct)).Select(AgendaAdministracionMapper.ToDTO).ToList() : [] };
    }
    public async Task CargarSucursalesAsync(AgendaFormulario f,CancellationToken ct=default)=>f.Sucursales=(await repo.SucursalesAsync(ct)).Where(s=>s.Activa).Select(AgendaAdministracionMapper.ToDTO).ToList();
    public async Task<HorarioAdministracionDTO?> HorarioAsync(int id,CancellationToken ct=default)=>await repo.HorarioAsync(id,ct) is {} h ? AgendaAdministracionMapper.ToDTO(h) : null;
    public async Task<BloqueoAdministracionDTO?> BloqueoAsync(long id,CancellationToken ct=default)=>await repo.BloqueoAsync(id,ct) is {} b ? AgendaAdministracionMapper.ToDTO(b) : null;
    public async Task<ResultadoAgendaDTO> GuardarAsync(AgendaFormulario f,int actor,CancellationToken ct=default)
    {
        var errores=new List<ValidationResult>();
        if(!Validator.TryValidateObject(f,new ValidationContext(f),errores,true)) return new(null,string.Join(" ",errores.Select(e=>e.ErrorMessage)));
        if(f.Fecha!.Value<Hoy) return new(null,"La fecha debe ser igual o posterior a la fecha actual de Costa Rica.");
        if(f.HoraInicio>=f.HoraFin) return new(null,"La hora inicial debe ser menor que la hora final.");
        if(f.HoraInicio!.Value.Ticks%TimeSpan.TicksPerMinute!=0 || f.HoraFin!.Value.Ticks%TimeSpan.TicksPerMinute!=0) return new(null,"Ingrese horas con precisión de minutos, sin segundos.");
        if(f.EsBloqueo && string.IsNullOrWhiteSpace(f.Motivo)) return new(null,"El motivo del bloqueo es obligatorio.");
        return await repo.GuardarAsync(f,actor,context=>ValidarConflictos(f,context),ct);
    }
    public static string? ValidarConflictos(AgendaFormulario f,ContextoAgendaDTO c)
    {
        if(f.Fecha<Hoy) return "La fecha debe ser igual o posterior a la fecha actual de Costa Rica.";
        if(!c.SucursalActiva) return "La sucursal no existe o está inactiva.";
        if(!c.AdministradorActivo) return "La sesión no corresponde a un administrador activo.";
        bool Cruza(IntervaloAgendaDTO i)=>f.HoraInicio!.Value<i.Fin && i.Inicio<f.HoraFin!.Value;
        if(c.Horarios.Any(Cruza)) return f.EsBloqueo ? "El bloqueo se sobrepone con un horario de la sucursal." : "El horario se sobrepone con otro horario de la sucursal.";
        if(c.Bloqueos.Any(Cruza)) return f.EsBloqueo ? "El bloqueo se sobrepone con otro bloqueo de la sucursal." : "El horario se sobrepone con un bloqueo de la sucursal.";
        if(f.EsBloqueo && c.Citas.Any(Cruza)) return "El bloqueo se sobrepone con una cita existente de la sucursal.";
        return null;
    }
}
