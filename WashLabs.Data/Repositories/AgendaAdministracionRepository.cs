using System.Data;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Data.Repositories;

public sealed class AgendaAdministracionRepository(WashLabsDbContext db) : IAgendaAdministracionRepository
{
    public async Task<IReadOnlyList<Sucursal>> SucursalesAsync(CancellationToken ct) => await db.Sucursales.AsNoTracking().OrderBy(s=>s.Nombre).ToListAsync(ct);
    public async Task<IReadOnlyList<HorarioSucursal>> HorariosAsync(int id, CancellationToken ct) => await db.HorariosSucursal.AsNoTracking().Include(h=>h.Sucursal).Where(h=>h.SucursalId==id).OrderBy(h=>h.FechaVigenciaDesde).ThenBy(h=>h.DiaSemana).ThenBy(h=>h.HoraInicio).ToListAsync(ct);
    public async Task<IReadOnlyList<BloqueoAgenda>> BloqueosAsync(int id, CancellationToken ct) => await db.BloqueosAgenda.AsNoTracking().Include(b=>b.Sucursal).Include(b=>b.CreadoPorUsuario).Where(b=>b.SucursalId==id).OrderBy(b=>b.Fecha).ThenBy(b=>b.HoraInicio).ToListAsync(ct);
    public Task<HorarioSucursal?> HorarioAsync(int id, CancellationToken ct) => db.HorariosSucursal.AsNoTracking().Include(h=>h.Sucursal).SingleOrDefaultAsync(h=>h.HorarioSucursalId==id,ct);
    public Task<BloqueoAgenda?> BloqueoAsync(long id, CancellationToken ct) => db.BloqueosAgenda.AsNoTracking().Include(b=>b.Sucursal).Include(b=>b.CreadoPorUsuario).SingleOrDefaultAsync(b=>b.BloqueoAgendaId==id,ct);
    public async Task<ResultadoAgendaDTO> GuardarAsync(AgendaFormulario f, int actor, Func<ContextoAgendaDTO,string?> validar, CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var recurso="WashLabs:Agenda:Sucursal:"+f.SucursalId;
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={recurso}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r < 0 THROW 51000, 'No se pudo bloquear la agenda.', 1;",ct);
        var fecha=f.Fecha!.Value;
        var dia=(byte)(((int)fecha.DayOfWeek+6)%7+1);
        var horarios=await db.HorariosSucursal.AsNoTracking().Where(h=>h.SucursalId==f.SucursalId && h.Activo && h.DiaSemana==dia && h.FechaVigenciaDesde<=fecha && (h.FechaVigenciaHasta==null || h.FechaVigenciaHasta>=fecha)).Select(h=>new IntervaloAgendaDTO(h.HoraInicio,h.HoraFin)).ToListAsync(ct);
        var bloqueos=await db.BloqueosAgenda.AsNoTracking().Where(b=>b.SucursalId==f.SucursalId && b.Fecha==fecha).Select(b=>new IntervaloAgendaDTO(b.DiaCompleto ? TimeOnly.MinValue : b.HoraInicio!.Value,b.DiaCompleto ? TimeOnly.MaxValue : b.HoraFin!.Value)).ToListAsync(ct);
        var citas=await db.Citas.AsNoTracking().Where(c=>c.SucursalId==f.SucursalId && c.Fecha==fecha && c.EstadoCita.Nombre!="Cancelada" && c.EstadoCita.Nombre!="No asistió").Select(c=>new IntervaloAgendaDTO(c.HoraInicio,c.HoraFin)).ToListAsync(ct);
        var error=validar(new(await db.Sucursales.AnyAsync(s=>s.SucursalId==f.SucursalId && s.Activa,ct),
            await db.Usuarios.AnyAsync(u=>u.UsuarioId==actor && u.Activo && u.Rol.Nombre=="Administrador",ct),horarios,bloqueos,citas));
        if(error is not null) return new(null,error);
        long id;
        if(f.EsBloqueo)
        {
            var b=new BloqueoAgenda { SucursalId=f.SucursalId,Fecha=fecha,HoraInicio=f.HoraInicio,HoraFin=f.HoraFin,DiaCompleto=false,Motivo=f.Motivo!.Trim(),CreadoPorUsuarioId=actor };
            db.BloqueosAgenda.Add(b); await db.SaveChangesAsync(ct); id=b.BloqueoAgendaId;
        }
        else
        {
            var h=new HorarioSucursal { SucursalId=f.SucursalId,DiaSemana=dia,FechaVigenciaDesde=fecha,FechaVigenciaHasta=fecha,HoraInicio=f.HoraInicio!.Value,HoraFin=f.HoraFin!.Value,Activo=true };
            db.HorariosSucursal.Add(h); await db.SaveChangesAsync(ct); id=h.HorarioSucursalId;
        }
        await tx.CommitAsync(ct); return new(id,null);
    }
}
