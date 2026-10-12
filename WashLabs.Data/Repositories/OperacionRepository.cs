using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
        // Una transición atómica. El trigger existente acredita el lavado y evita duplicados.
        var citas = context.Citas.Where(c => c.CitaId == id && context.Usuarios.Any(u =>
            u.UsuarioId == encargadoId && u.Activo && u.Rol.Nombre == "Encargado"
            && u.Sucursal != null && u.Sucursal.Activa && u.SucursalId == c.SucursalId));
        var cambiadas = await citas.Where(c => c.EstadoCita.Nombre == "Pendiente" || c.EstadoCita.Nombre == "Confirmada"
            || c.EstadoCita.Nombre == "Reprogramada").ExecuteUpdateAsync(s => s.SetProperty(c => c.EstadoCitaId, estado.Value), ct);
        return cambiadas == 1 || await citas.AnyAsync(c => c.EstadoCitaId == estado.Value, ct);
    }

    public async Task<bool> AsignarVehiculoAsync(long citaId, int vehiculoId, int encargadoId, CancellationToken ct)
    {
        var cambiadas = await context.Citas.Where(c => c.CitaId == citaId && c.Cliente.Activo
            && (c.EstadoCita.Nombre == "Pendiente" || c.EstadoCita.Nombre == "Confirmada" || c.EstadoCita.Nombre == "Reprogramada")
            && context.Usuarios.Any(u => u.UsuarioId == encargadoId && u.Activo && u.Rol.Nombre == "Encargado"
                && u.Sucursal != null && u.Sucursal.Activa && u.SucursalId == c.SucursalId)
            && context.Vehiculos.Any(v => v.VehiculoId == vehiculoId && v.Activo && v.ClienteId == c.ClienteId))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.VehiculoId, vehiculoId), ct);
        return cambiadas == 1;
    }
}
