using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;

namespace WashLabs.Data.Repositories;

public sealed class OperacionRepository(WashLabsDbContext context) : IOperacionRepository
{
    public async Task<IReadOnlyList<Usuario>> ClientesAsync(CancellationToken ct) =>
        await context.Usuarios.AsNoTracking().Where(u => u.Activo && u.Rol.Nombre == "Cliente")
            .OrderBy(u => u.NombreCompleto).ToListAsync(ct);

    public async Task<IReadOnlyList<Vehiculo>> VehiculosAsync(CancellationToken ct, int? clienteId = null) =>
        await context.Vehiculos.AsNoTracking().Where(v => v.Activo && v.Cliente.Activo
            && (!clienteId.HasValue || v.ClienteId == clienteId.Value)).Include(v => v.Cliente)
            .Include(v => v.TarjetasWashPoints).ThenInclude(t => t!.MovimientosWashPoints)
            .OrderBy(v => v.Matricula).AsSplitQuery().ToListAsync(ct);

    public async Task<string?> RegistrarVehiculoAsync(Vehiculo vehiculo, CancellationToken ct)
    {
        if (!await context.Usuarios.AnyAsync(u => u.UsuarioId == vehiculo.ClienteId && u.Activo && u.Rol.Nombre == "Cliente", ct))
            return "El cliente seleccionado no está disponible.";
        if (await context.Vehiculos.AnyAsync(v => v.Matricula == vehiculo.Matricula, ct))
            return "La matrícula ya está registrada.";
        context.Vehiculos.Add(vehiculo);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { context.Entry(vehiculo).State = EntityState.Detached; return "La matrícula ya está registrada."; }
        return null;
    }

    public Task<ConfiguracionWashPoints?> ConfiguracionPuntosAsync(CancellationToken ct) =>
        context.ConfiguracionWashPoints.AsNoTracking().Where(c => c.Activa)
            .OrderBy(c => c.ConfiguracionWashPointsId).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CategoriaAsistente>> CategoriasAsistenteAsync(CancellationToken ct) =>
        await context.CategoriasAsistente.AsNoTracking().Where(c => c.Activa)
            .Include(c => c.PreguntasAsistente.Where(p => p.Activa && p.EsInicial))
            .OrderBy(c => c.Orden).ThenBy(c => c.CategoriaAsistenteId).ToListAsync(ct);

    public Task<PreguntaAsistente?> PreguntaAsync(int id, CancellationToken ct) =>
        context.PreguntasAsistente.AsNoTracking().Include(p => p.CategoriaAsistente)
            .Include(p => p.OpcionesAsistentePreguntaAsistente.Where(o => o.Activa))
            .SingleOrDefaultAsync(p => p.PreguntaAsistenteId == id && p.Activa && p.CategoriaAsistente.Activa, ct);

    public async Task<bool> CompletarCitaAsync(long id, int encargadoId, CancellationToken ct)
    {
        var estado = await context.EstadosCita.Where(e => e.Nombre == "Completada").Select(e => (byte?)e.EstadoCitaId).SingleOrDefaultAsync(ct);
        if (estado is null) return false;
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // El bloqueo se obtiene antes de leer la tarjeta: serializa citas del mismo vehículo.
            var citas = context.Citas.Where(c => c.CitaId == id && context.Usuarios.Any(u =>
                u.UsuarioId == encargadoId && u.Activo && (u.Rol.Nombre == "Administrador"
                || u.Rol.Nombre == "Encargado" && u.Sucursal != null && u.Sucursal.Activa && u.SucursalId == c.SucursalId))
                && c.Cliente.Activo && c.Vehiculo.Activo && c.Vehiculo.ClienteId == c.ClienteId && c.Sucursal.Activa);
            var cambiadas = await citas.Where(c => c.EstadoCita.Nombre == "Pendiente" || c.EstadoCita.Nombre == "Confirmada"
                || c.EstadoCita.Nombre == "Reprogramada").ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.EstadoCitaId, estado.Value).SetProperty(c => c.FechaActualizacion, DateTime.Now), ct);
            if (cambiadas == 0)
                return await citas.AnyAsync(c => c.EstadoCitaId == estado.Value
                    && context.Facturas.Any(f => f.CitaId == id && f.TipoDocumento == "FACTURA"), ct);
            var cita = await citas.AsNoTracking().SingleAsync(ct);
            var factura = await context.Facturas.Include(f => f.DetallesFactura).Include(f => f.EstadoFactura)
                .SingleOrDefaultAsync(f => f.CitaId == id, ct);
            if (factura is null || factura.TipoDocumento != "PROFORMA" || factura.EstadoFactura.Nombre != "Proforma"
                || factura.ClienteId != cita.ClienteId || factura.SucursalId != cita.SucursalId
                || !factura.DetallesFactura.Any(d => d.TipoLinea == "S" && d.ServicioId == cita.ServicioId))
            {
                await transaction.RollbackAsync(ct);
                return false;
            }
            factura.EstadoFacturaId = await context.EstadosFactura.Where(e => e.Nombre == "Pendiente")
                .Select(e => e.EstadoFacturaId).SingleAsync(ct);
            factura.TipoDocumento = "FACTURA";
            factura.Fecha = DateTime.Now;
            await FacturaRepository.ActualizarTotalesAsync(context, factura, ct);
            if (!await context.MovimientosWashPoints.AnyAsync(m => m.CitaId == id && m.TipoMovimiento == "ACUMULA", ct))
            {
                // UPDATE toma un bloqueo exclusivo incluso si la tarjeta todavía no existe.
                await context.Vehiculos.Where(v => v.VehiculoId == cita.VehiculoId)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.Activo, v => v.Activo), ct);
                var tarjeta = await context.TarjetasWashPoints.SingleOrDefaultAsync(t => t.VehiculoId == cita.VehiculoId, ct);
                if (tarjeta is null)
                {
                    tarjeta = new TarjetaWashPoints { VehiculoId = cita.VehiculoId, CicloActual = 1 };
                    context.TarjetasWashPoints.Add(tarjeta);
                }
                tarjeta.LavadosAcumulados = checked(tarjeta.LavadosAcumulados + 1);
                tarjeta.FechaActualizacion = DateTime.Now;
                context.MovimientosWashPoints.Add(new MovimientoWashPoints {
                    TarjetaWashPoints = tarjeta, CitaId = id, TipoMovimiento = "ACUMULA", Cantidad = 1,
                    Ciclo = tarjeta.CicloActual, Descripcion = "Lavado por cita completada" });
            }
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex.GetBaseException() is SqlException { Number: 1205 or 2601 or 2627 })
        {
            await transaction.RollbackAsync(ct);
            context.ChangeTracker.Clear();
            return false;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<bool> AsignarVehiculoAsync(long citaId, int vehiculoId, int encargadoId, CancellationToken ct)
    {
        var cambiadas = await context.Citas.Where(c => c.CitaId == citaId && c.Cliente.Activo
            && (c.EstadoCita.Nombre == "Pendiente" || c.EstadoCita.Nombre == "Confirmada" || c.EstadoCita.Nombre == "Reprogramada")
            && context.Usuarios.Any(u => u.UsuarioId == encargadoId && u.Activo && (u.Rol.Nombre == "Administrador"
                || u.Rol.Nombre == "Encargado" && u.Sucursal != null && u.Sucursal.Activa && u.SucursalId == c.SucursalId))
            && context.Vehiculos.Any(v => v.VehiculoId == vehiculoId && v.Activo && v.ClienteId == c.ClienteId))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.VehiculoId, vehiculoId), ct);
        return cambiadas == 1;
    }

    public async Task<bool> CancelarCitaAsync(long id, int usuarioId, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var estado = await context.EstadosCita.Where(e => e.Nombre == "Cancelada").Select(e => e.EstadoCitaId).SingleAsync(ct);
            var citas = context.Citas.Where(c => c.CitaId == id && context.Usuarios.Any(u => u.UsuarioId == usuarioId && u.Activo
                && (u.Rol.Nombre == "Administrador" || u.Rol.Nombre == "Cliente" && u.UsuarioId == c.ClienteId
                    || u.Rol.Nombre == "Encargado" && u.SucursalId == c.SucursalId && u.Sucursal!.Activa)));
            var cambiadas = await citas.Where(c => c.EstadoCita.Nombre == "Pendiente" || c.EstadoCita.Nombre == "Confirmada"
                || c.EstadoCita.Nombre == "Reprogramada").ExecuteUpdateAsync(s => s.SetProperty(c => c.EstadoCitaId, estado)
                    .SetProperty(c => c.FechaActualizacion, DateTime.Now), ct);
            if (cambiadas == 0) return await citas.AnyAsync(c => c.EstadoCitaId == estado, ct);
            var factura = await context.Facturas.SingleOrDefaultAsync(f => f.CitaId == id, ct);
            if (factura is not null)
            {
                if (factura.TipoDocumento != "PROFORMA") return false;
                factura.EstadoFacturaId = await context.EstadosFactura.Where(e => e.Nombre == "Anulada").Select(e => e.EstadoFacturaId).SingleAsync(ct);
            }
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
    }
}
