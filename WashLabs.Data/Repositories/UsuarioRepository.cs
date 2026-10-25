using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;

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
            FechaRegistro = u.FechaRegistro,
            Rol = new Rol { Nombre = u.Rol.Nombre }
        });

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct) =>
        await Consulta().OrderBy(u => u.UsuarioId).ToListAsync(ct);

    public Task<Usuario?> ObtenerAsync(int id, CancellationToken ct) =>
        Consulta().SingleOrDefaultAsync(u => u.UsuarioId == id, ct);
}
