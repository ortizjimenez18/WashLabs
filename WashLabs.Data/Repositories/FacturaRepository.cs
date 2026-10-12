using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;

namespace WashLabs.Data.Repositories;

public sealed class FacturaRepository(WashLabsDbContext context) : IFacturaRepository
{
    public async Task<IReadOnlyList<Factura>> ListarAsync(CancellationToken cancellationToken) =>
        await context.Facturas.AsNoTracking().Include(f => f.Cliente)
            .Where(f => f.TipoDocumento == "FACTURA" && f.Cliente.Activo && f.Sucursal.Activa)
            .OrderByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId)
            .ToListAsync(cancellationToken);

    public Task<Factura?> ObtenerAsync(long id, CancellationToken cancellationToken) =>
        context.Facturas.AsNoTracking().Include(f => f.Cliente).Include(f => f.Sucursal)
            .Include(f => f.EstadoFactura).Include(f => f.DetallesFactura)
            .SingleOrDefaultAsync(f => f.FacturaId == id && f.TipoDocumento == "FACTURA" && f.Cliente.Activo && f.Sucursal.Activa, cancellationToken);
}
