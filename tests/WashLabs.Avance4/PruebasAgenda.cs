using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Repositories;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Implementations;

internal static class PruebasAgenda
{
    public static async Task EjecutarAsync(DbContextOptions<WashLabsDbContext> options,int sucursal,string prefix,Action<bool,string> check)
    {
        int actor,otra;
        await using(var db=new WashLabsDbContext(options))
        {
            actor=await db.Usuarios.Where(u=>u.Activo && u.Rol.Nombre=="Administrador").Select(u=>u.UsuarioId).SingleAsync();
            var s=new Sucursal { Nombre=prefix+"AgendaB", Descripcion="Prueba temporal de agenda", Telefono="22223333", Direccion="Prueba", Correo=prefix+"agendab@example.invalid", Activa=true };
            db.Sucursales.Add(s); await db.SaveChangesAsync(); otra=s.SucursalId;
        }
        var fecha=AgendaAdministracionService.Hoy.AddDays(14);
        AgendaFormulario Form(int sede, int dia,int inicio,int fin,bool bloqueo=false)=>new() { SucursalId=sede, Fecha=fecha.AddDays(dia), HoraInicio=new(inicio,0), HoraFin=new(fin,0), EsBloqueo=bloqueo, Motivo=bloqueo ? "Verificación temporal" : null };
        async Task<ResultadoAgendaDTO> Save(AgendaFormulario f,int? usuario=null)
        { await using var db=new WashLabsDbContext(options); return await new AgendaAdministracionService(new AgendaAdministracionRepository(db)).GuardarAsync(f,usuario??actor); }
        var first=await Save(Form(sucursal,0,8,10));
        check(first.Error is null,"Horario válido menor de ocho horas en fecha puntual");
        check((await Save(Form(sucursal,-15,8,10))).Error!.Contains("fecha"),"Fecha anterior rechazada");
        check((await Save(Form(sucursal,0,11,10))).Error!.Contains("menor"),"Horas invertidas rechazadas");
        check((await Save(Form(sucursal,0,10,10))).Error!.Contains("menor"),"Intervalo vacío rechazado");
        check((await Save(Form(sucursal,0,9,11))).Error!.Contains("otro horario"),"Horario contra horario");
        check((await Save(Form(sucursal,0,8,10,true))).Error!.Contains("un horario"),"Bloqueo contra horario");
        check((await Save(Form(sucursal,0,10,11))).Error is null,"Horarios contiguos permitidos");
        var block=await Save(Form(sucursal,0,12,13,true));
        check(block.Error is null,"Bloqueo válido persistido");
        check((await Save(Form(sucursal,0,12,14,true))).Error!.Contains("otro bloqueo"),"Bloqueo contra bloqueo");
        check((await Save(Form(sucursal,0,12,14))).Error!.Contains("un bloqueo"),"Horario contra bloqueo");
        check((await Save(Form(sucursal,0,11,12,true))).Error is null,"Bloqueo contiguo a horario y a bloqueo permitido");
        check((await Save(Form(otra,0,8,10))).Error is null,"Mismo horario en diferente sucursal permitido");
        check((await Save(Form(int.MaxValue,0,8,10))).Error!.Contains("sucursal"),"Sucursal inexistente rechazada");
        check((await Save(Form(sucursal,1,8,10),int.MaxValue)).Error!.Contains("administrador"),"Actor inexistente rechazado");
        var empty=Form(sucursal,1,8,10,true); empty.Motivo=" ";
        check((await Save(empty)).Error!.Contains("motivo"),"Motivo obligatorio para bloqueo");
        var concurrencia=await Task.WhenAll(Save(Form(sucursal,2,8,10)),Save(Form(sucursal,2,9,11)));
        check(concurrencia.Count(r=>r.Error is null)==1 && concurrencia.Count(r=>r.Error is not null)==1,"Concurrencia SQL: exactamente un horario sobrepuesto guardado");
        var cruzados=await Task.WhenAll(Save(Form(sucursal,3,8,10)),Save(Form(sucursal,3,9,11,true)));
        check(cruzados.Count(r=>r.Error is null)==1 && cruzados.Count(r=>r.Error is not null)==1,"Concurrencia SQL: horario contra bloqueo serializados");
        await using(var db=new WashLabsDbContext(options))
        {
            var fechaRecurrente=fecha.AddDays(4);
            db.HorariosSucursal.Add(new HorarioSucursal { SucursalId=sucursal,DiaSemana=(byte)(((int)fechaRecurrente.DayOfWeek+6)%7+1),FechaVigenciaDesde=fechaRecurrente,HoraInicio=new(8,0),HoraFin=new(17,0),Activo=true });
            db.BloqueosAgenda.Add(new BloqueoAgenda { SucursalId=sucursal,Fecha=fecha.AddDays(5),DiaCompleto=true,Motivo="Día completo de prueba",CreadoPorUsuarioId=actor });
            await db.SaveChangesAsync();
        }
        check((await Save(Form(sucursal,4,9,10))).Error!.Contains("otro horario"),"Horario recurrente existente participa en validación");
        check((await Save(Form(sucursal,4,9,10,true))).Error!.Contains("un horario"),"Bloqueo respeta horario recurrente");
        check((await Save(Form(sucursal,5,9,10))).Error!.Contains("un bloqueo"),"Bloqueo de día completo existente participa en validación");
        await using(var db=new WashLabsDbContext(options))
        {
            var service=new AgendaAdministracionService(new AgendaAdministracionRepository(db));
            var list=await service.ListarAsync(sucursal);
            var detail=await service.HorarioAsync((int)first.Id!.Value);
            var detailBlock=await service.BloqueoAsync(block.Id!.Value);
            check(list.Horarios.Any(h=>h.Id==first.Id) && detail?.Desde==fecha && detail.Hasta==fecha,"Listado y detalle horario después de guardar");
            check(list.Bloqueos.Any(b=>b.Id==block.Id) && detailBlock?.Fecha==fecha,"Listado y detalle bloqueo después de guardar");
            check(list.Horarios.All(h=>h.SucursalId==sucursal) && list.Bloqueos.All(b=>b.SucursalId==sucursal),"Listados separados por sucursal");
        }
    }
}
