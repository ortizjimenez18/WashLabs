using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;

namespace WashLabs.Data.Repositories;

public sealed class InicioRepository(WashLabsDbContext context) : IInicioRepository
{
    public Task<ConfiguracionSistema?> ObtenerNegocioAsync(CancellationToken cancellationToken) =>
        context.ConfiguracionSistema.AsNoTracking().Where(c => c.Activa)
            .OrderBy(c => c.ConfiguracionSistemaId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Servicio>> ListarServiciosAsync(CancellationToken cancellationToken) =>
        await context.Servicios.AsNoTracking().Where(s => s.Activo)
            .OrderBy(s => s.TarifaBase).ThenBy(s => s.ServicioId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Sucursal>> ListarSucursalesAsync(CancellationToken cancellationToken) =>
        await context.Sucursales.AsNoTracking().Where(s => s.Activa)
            .OrderBy(s => s.SucursalId).ToListAsync(cancellationToken);
}
