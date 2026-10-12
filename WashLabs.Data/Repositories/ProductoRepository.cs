using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;

namespace WashLabs.Data.Repositories;

public sealed class ProductoRepository(WashLabsDbContext context) : IProductoRepository
{
    public async Task<IReadOnlyList<Producto>> ListarTodosAsync(CancellationToken cancellationToken) =>
        await context.Productos.AsNoTracking().Include(p => p.CategoriaProducto).OrderBy(p => p.Nombre).ToListAsync(cancellationToken);

    public Task<Producto?> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        context.Productos.AsNoTracking().Include(p => p.CategoriaProducto).SingleOrDefaultAsync(p => p.ProductoId == id, cancellationToken);
    public async Task<IReadOnlyList<Producto>> ListarActivosAsync(int? categoriaId, string? busqueda, CancellationToken cancellationToken)
    {
        var query = context.Productos.AsNoTracking().Include(p => p.CategoriaProducto).Where(p => p.Activo);
        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaProductoId == categoriaId.Value);
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda));
        return await query.OrderBy(p => p.Nombre).ToListAsync(cancellationToken);
    }

    public Task<Producto?> ObtenerActivoAsync(int id, CancellationToken cancellationToken) =>
        context.Productos.AsNoTracking().Include(p => p.CategoriaProducto)
            .SingleOrDefaultAsync(p => p.ProductoId == id && p.Activo, cancellationToken);

    public async Task<IReadOnlyList<CategoriaProducto>> ListarCategoriasAsync(CancellationToken cancellationToken) =>
        await context.CategoriasProducto.AsNoTracking()
            .Where(c => c.Activa || c.Productos.Any(p => p.Activo))
            .OrderBy(c => c.Nombre).ToListAsync(cancellationToken);
}
