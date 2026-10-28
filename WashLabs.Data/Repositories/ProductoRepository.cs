using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;

namespace WashLabs.Data.Repositories;

public sealed class ProductoRepository(WashLabsDbContext context) : IProductoRepository
{
    public async Task<IReadOnlyList<CategoriaProducto>> ListarCategoriasMantenimientoAsync(CancellationToken ct) =>
        await context.CategoriasProducto.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(ct);
    public Task<bool> CategoriaExisteAsync(int id, CancellationToken ct) =>
        context.CategoriasProducto.AnyAsync(c => c.CategoriaProductoId == id, ct);
    public Task<bool> SkuExisteAsync(string sku, int exceptoId, CancellationToken ct) =>
        context.Productos.AnyAsync(p => p.SKU == sku && p.ProductoId != exceptoId, ct);
    public async Task<bool> GuardarAsync(Producto producto, bool crear, CancellationToken ct)
    {
        if (crear) context.Productos.Add(producto);
        else
        {
            var actual = await context.Productos.FindAsync([producto.ProductoId], ct);
            if (actual is null) return false;
            actual.Nombre = producto.Nombre; actual.Descripcion = producto.Descripcion;
            actual.CategoriaProductoId = producto.CategoriaProductoId; actual.Precio = producto.Precio;
            actual.Marca = producto.Marca; actual.Presentacion = producto.Presentacion;
            actual.SKU = producto.SKU; actual.ImagenUrl = producto.ImagenUrl; actual.Activo = producto.Activo;
        }
        await context.SaveChangesAsync(ct);
        return true;
    }
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
