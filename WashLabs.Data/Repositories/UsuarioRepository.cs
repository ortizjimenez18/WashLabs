using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;

namespace WashLabs.Data.Repositories;

public sealed class UsuarioRepository(WashLabsDbContext context) : IUsuarioRepository
{
    // EF traduce la navegación Rol a JOIN. Solo se seleccionan campos de consulta;
    // PasswordHash, identificación, dirección y fecha de nacimiento no se cargan.
    private IQueryable<Usuario> Consulta() => context.Usuarios.AsNoTracking()
        .Select(u => new Usuario
        {
            UsuarioId = u.UsuarioId, NombreCompleto = u.NombreCompleto,
            Correo = u.Correo, Activo = u.Activo, Telefono = u.Telefono,
            FechaRegistro = u.FechaRegistro, RolId = u.RolId, SucursalId = u.SucursalId,
            Sucursal = u.Sucursal == null ? null : new Sucursal { SucursalId = u.Sucursal.SucursalId, Nombre = u.Sucursal.Nombre },
            Rol = new Rol { RolId = u.RolId, Nombre = u.Rol.Nombre }
        });

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct) =>
        await Consulta().OrderBy(u => u.UsuarioId).ToListAsync(ct);

    public Task<Usuario?> ObtenerAsync(int id, CancellationToken ct) =>
        Consulta().SingleOrDefaultAsync(u => u.UsuarioId == id, ct);

    public async Task<IReadOnlyList<Rol>> RolesAdministrablesAsync(CancellationToken ct) =>
        await context.Roles.AsNoTracking().Where(r => r.Nombre == "Cliente" || r.Nombre == "Encargado").OrderBy(r => r.Nombre).ToListAsync(ct);

    public async Task<string?> CambiarRolAsync(int usuarioId, int rolId, int administradorId,
        Func<ContextoCambioRolDTO, string?> validar, CancellationToken ct)
    {
        try
        {
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await context.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'WashLabs:Sucursales:Encargados', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r < 0 THROW 51000, 'No se pudo reservar el cambio de rol.', 1;", ct);
            var actor = await context.Usuarios.AnyAsync(u => u.UsuarioId == administradorId && u.Activo && u.Rol.Nombre == "Administrador", ct);
            var usuario = await ObtenerAsync(usuarioId, ct);
            var destino = await context.Roles.AsNoTracking().SingleOrDefaultAsync(r => r.RolId == rolId, ct);
            var error = validar(new(actor, usuario?.UsuarioId, usuario?.Activo ?? false, usuario?.RolId ?? 0,
                usuario?.Rol.Nombre ?? "", usuario?.SucursalId, destino?.RolId, destino?.Nombre));
            if (error is not null) return error;
            await context.Usuarios.Where(u => u.UsuarioId == usuarioId).ExecuteUpdateAsync(s => s
                .SetProperty(u => u.RolId, destino!.RolId)
                .SetProperty(u => u.SucursalId, (int?)null)
                .SetProperty(u => u.FechaActualizacion, DateTime.Now), ct);
            await tx.CommitAsync(ct);
            return null;
        }
        catch (SqlException ex) when (ex.Number is 1205 or 1222 or 51000)
        { return "El usuario está siendo actualizado. Revise sus datos e intente de nuevo."; }
    }
}
