using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;

namespace WashLabs.Data.Repositories;

public sealed class RecordatorioRepository(WashLabsDbContext db)
{
    private IQueryable<Cita> Pendientes(DateOnly fecha) => db.Citas.Where(c =>
        c.Fecha == fecha && !c.Notificado && c.Cliente.Activo && c.Sucursal.Activa &&
        (c.EstadoCita.Nombre == "Pendiente" || c.EstadoCita.Nombre == "Confirmada" || c.EstadoCita.Nombre == "Reprogramada"));

    public Task<List<long>> CitasPorNotificarAsync(DateOnly fecha, CancellationToken ct) =>
        Pendientes(fecha).OrderBy(c => c.HoraInicio).Select(c => c.CitaId).ToListAsync(ct);

    public async Task<bool> NotificarAsync(long id, DateOnly fecha, DateTime ahora, Func<Cita, CancellationToken, Task> enviar, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        // El UPDATE toma el bloqueo antes del envío. Si SMTP falla, la transacción revierte la marca.
        var reservada = await Pendientes(fecha).Where(c => c.CitaId == id).ExecuteUpdateAsync(
            s => s.SetProperty(c => c.Notificado, true).SetProperty(c => c.FechaNotificacion, ahora), ct);
        if (reservada != 1) return false;

        var cita = await db.Citas.AsNoTracking().Include(c => c.Cliente).Include(c => c.Sucursal)
            .Include(c => c.Servicio).SingleAsync(c => c.CitaId == id, ct);
        await enviar(cita, ct);
        await transaccion.CommitAsync(CancellationToken.None);
        return true;
    }
}
