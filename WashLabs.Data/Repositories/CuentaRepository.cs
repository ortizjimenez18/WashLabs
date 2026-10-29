using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
namespace WashLabs.Data.Repositories;
public sealed class CuentaRepository(WashLabsDbContext context) : ICuentaRepository
{
    public Task<Usuario?> PorCorreoAsync(string correo, CancellationToken ct) => context.Usuarios.AsNoTracking()
        .Include(u => u.Rol).Include(u => u.Sucursal).SingleOrDefaultAsync(u => u.Correo == correo, ct);
    public Task<Usuario?> PorIdAsync(int id, CancellationToken ct) => context.Usuarios.AsNoTracking()
        .Include(u => u.Rol).Include(u => u.Sucursal).SingleOrDefaultAsync(u => u.UsuarioId == id, ct);
    public async Task<string?> RegistrarAsync(Usuario usuario, CancellationToken ct)
    {
        if (await context.Usuarios.AnyAsync(u => u.Correo == usuario.Correo || u.Identificacion == usuario.Identificacion, ct))
            return "No se puede registrar una cuenta con esos datos. Revisa el correo y la identificación.";
        var rol = await context.Roles.Where(r => r.Nombre == "Cliente").Select(r => (byte?)r.RolId).SingleOrDefaultAsync(ct);
        if (!rol.HasValue) return "El registro de clientes no está disponible.";
        usuario.RolId = rol.Value;
        usuario.Activo = true;
        context.Usuarios.Add(usuario);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { context.Entry(usuario).State = EntityState.Detached; return "No se puede registrar una cuenta con esos datos."; }
        return null;
    }
    public async Task GuardarClaveAsync(int id, string hash, CancellationToken ct) =>
        _ = await context.Usuarios.Where(u => u.UsuarioId == id).ExecuteUpdateAsync(s => s
            .SetProperty(u => u.PasswordHash, hash).SetProperty(u => u.FechaActualizacion, DateTime.Now), ct);
}
