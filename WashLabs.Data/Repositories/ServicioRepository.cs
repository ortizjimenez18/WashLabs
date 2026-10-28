using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;

namespace WashLabs.Data.Repositories;

public sealed class ServicioRepository(WashLabsDbContext db) : IServicioRepository
{
    public async Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken ct) =>
        await db.Servicios.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync(ct);
    public Task<Servicio?> ObtenerAsync(int id, CancellationToken ct) =>
        db.Servicios.AsNoTracking().SingleOrDefaultAsync(s => s.ServicioId == id, ct);
    public Task<bool> NombreExisteAsync(string nombre, int exceptoId, CancellationToken ct) =>
        db.Servicios.AnyAsync(s => s.Nombre == nombre && s.ServicioId != exceptoId, ct);
    public async Task<bool> GuardarAsync(Servicio servicio, bool crear, CancellationToken ct)
    {
        if (crear) db.Servicios.Add(servicio);
        else
        {
            var actual = await db.Servicios.FindAsync([servicio.ServicioId], ct);
            if (actual is null) return false;
            actual.Nombre = servicio.Nombre; actual.Descripcion = servicio.Descripcion;
            actual.TarifaBase = servicio.TarifaBase; actual.DuracionMinutos = servicio.DuracionMinutos;
            actual.TipoLavado = servicio.TipoLavado; actual.NivelDetalle = servicio.NivelDetalle;
            actual.Activo = servicio.Activo; actual.ImagenUrl = servicio.ImagenUrl;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }
}
