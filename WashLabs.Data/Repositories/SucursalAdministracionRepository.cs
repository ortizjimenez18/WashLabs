using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;

namespace WashLabs.Data.Repositories;

public sealed class SucursalAdministracionRepository(WashLabsDbContext db) : ISucursalAdministracionRepository
{
    public async Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken ct) =>
        await db.Sucursales.AsNoTracking().Include(s => s.Usuarios.Where(u => u.Rol.Nombre == "Encargado")).OrderBy(s => s.Nombre).ToListAsync(ct);
    public Task<Sucursal?> ObtenerAsync(int id, CancellationToken ct) => db.Sucursales.AsNoTracking()
        .Include(s => s.Usuarios.Where(u => u.Rol.Nombre == "Encargado")).SingleOrDefaultAsync(s => s.SucursalId == id, ct);
    public async Task<IReadOnlyList<Usuario>> EncargadosAsync(int? sucursalId, CancellationToken ct) =>
        await db.Usuarios.AsNoTracking().Where(u => u.Rol.Nombre == "Encargado" &&
            (u.Activo && u.SucursalId == null || sucursalId != null && u.SucursalId == sucursalId)).OrderBy(u => u.NombreCompleto).ToListAsync(ct);

    public async Task<string?> GuardarAsync(Sucursal datos, IReadOnlyList<int> ids, bool crear,
        Func<IReadOnlyList<EncargadoElegibilidadDTO>, string?> validar, CancellationToken ct)
    {
        try { return await GuardarTransaccionAsync(datos, ids, crear, validar, ct); }
        catch (SqlException ex) when (ex.Number is 1205 or 1222 or 51000)
        { return "Las asignaciones están siendo actualizadas. Intente guardar de nuevo."; }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 1205 or 1222)
        { return "Las asignaciones están siendo actualizadas. Intente guardar de nuevo."; }
    }

    private async Task<string?> GuardarTransaccionAsync(Sucursal datos, IReadOnlyList<int> ids, bool crear,
        Func<IReadOnlyList<EncargadoElegibilidadDTO>, string?> validar, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Un único bloqueo de aplicación ordena las asignaciones concurrentes, incluso entre sucursales.
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'WashLabs:Sucursales:Encargados', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r < 0 THROW 51000, 'No se pudo bloquear la asignación.', 1;", ct);
        var seleccion = await db.Usuarios.Where(u => ids.Contains(u.UsuarioId)).Include(u => u.Rol).ToListAsync(ct);
        var error = validar(seleccion.Select(u => new EncargadoElegibilidadDTO(u.UsuarioId, u.Rol.Nombre, u.SucursalId, u.Activo)).ToList());
        if (error is not null) return error;
        if (await db.Sucursales.AnyAsync(s => (crear || s.SucursalId != datos.SucursalId) && (s.Nombre == datos.Nombre || s.Correo == datos.Correo), ct))
            return "Ya existe una sucursal con ese nombre o correo.";
        var destino = crear ? new Sucursal { Activa = true } : await db.Sucursales.SingleOrDefaultAsync(s => s.SucursalId == datos.SucursalId, ct);
        if (destino is null) return "La sucursal ya no existe.";
        destino.Nombre = datos.Nombre; destino.Descripcion = datos.Descripcion; destino.Telefono = datos.Telefono;
        destino.Correo = datos.Correo; destino.Direccion = datos.Direccion;
        if (crear) db.Sucursales.Add(destino);
        else
        {
            var anteriores = await db.Usuarios.Where(u => u.SucursalId == destino.SucursalId && u.Rol.Nombre == "Encargado").ToListAsync(ct);
            foreach (var u in anteriores.Where(u => !ids.Contains(u.UsuarioId))) u.SucursalId = null;
        }
        foreach (var u in seleccion) u.Sucursal = destino;
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return null; }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        { return "Otra operación registró el mismo nombre o correo. Revise los datos."; }
    }
}
