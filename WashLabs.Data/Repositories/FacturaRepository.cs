using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;

namespace WashLabs.Data.Repositories;

public sealed class FacturaRepository(WashLabsDbContext context) : IFacturaRepository
{
    public async Task<long?> EmitirAsync(EmitirFacturaDTO solicitud, int usuarioId, TipoCambioVentaDTO? tasa, CancellationToken ct)
    {
        if (solicitud.SolicitudId == Guid.Empty || solicitud.Lineas.Count is < 1 or > 50 ||
            solicitud.Lineas.Any(l => l.Cantidad < 0 || l.Cantidad > 99999999)) return null;
        var lineas = solicitud.Lineas.Where(l => l.Cantidad > 0).ToArray();
        if (lineas.Length == 0 || lineas.Select(l => l.Articulo).Distinct(StringComparer.Ordinal).Count() != lineas.Length) return null;

        async Task<long?> Emitida() => await Autorizadas(usuarioId)
            .Where(f => f.EmitidaPorUsuarioId == usuarioId && f.SolicitudEmisionId == solicitud.SolicitudId)
            .Select(f => (long?)f.FacturaId).SingleOrDefaultAsync(ct);

        try
        {
            await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var anterior = await Emitida();
            if (anterior is not null) return anterior;
            if (!await context.Sucursales.AnyAsync(s => s.SucursalId == solicitud.SucursalId && s.Activa &&
                context.Usuarios.Any(u => u.UsuarioId == usuarioId && u.Activo &&
                    (u.Rol.Nombre == "Administrador" || u.Rol.Nombre == "Encargado" && u.SucursalId == s.SucursalId)), ct) ||
                !await context.Usuarios.AnyAsync(u => u.UsuarioId == solicitud.ClienteId && u.Activo && u.Rol.Nombre == "Cliente", ct))
                return null;

            var factura = new Factura
            {
                ClienteId = solicitud.ClienteId, SucursalId = solicitud.SucursalId, EmitidaPorUsuarioId = usuarioId,
                SolicitudEmisionId = solicitud.SolicitudId, TipoDocumento = "FACTURA", Fecha = DisponibilidadReserva.Ahora,
                TipoCambioVenta = tasa?.Valor, FechaTipoCambioVenta = tasa?.Fecha,
                EstadoFacturaId = await context.EstadosFactura.Where(e => e.Nombre == "Pendiente").Select(e => e.EstadoFacturaId).SingleAsync(ct)
            };
            foreach (var linea in lineas)
            {
                var partes = linea.Articulo?.Split(':', 2);
                if (partes is not { Length: 2 } || !int.TryParse(partes[1], out var id) || id <= 0 ||
                    linea.Articulo != $"{partes[0]}:{id}") return null;
                DetalleFactura? detalle = partes[0] switch
                {
                    "P" => await context.Productos.Where(p => p.ProductoId == id && p.Activo)
                        .Select(p => new DetalleFactura { TipoLinea = "P", ProductoId = id, Descripcion = p.Nombre, PrecioUnitario = p.Precio })
                        .SingleOrDefaultAsync(ct),
                    "S" => await context.SucursalServicios.Where(s => s.SucursalId == solicitud.SucursalId && s.ServicioId == id && s.Activo && s.Servicio.Activo)
                        .Select(s => new DetalleFactura { TipoLinea = "S", ServicioId = id, Descripcion = s.Servicio.Nombre,
                            PrecioUnitario = s.PrecioSucursal ?? s.Servicio.TarifaBase }).SingleOrDefaultAsync(ct),
                    _ => null
                };
                if (detalle is null || detalle.PrecioUnitario < 0) return null;
                detalle.Cantidad = linea.Cantidad;
                detalle.PorcentajeImpuesto = 13m;
                factura.DetallesFactura.Add(detalle);
            }
            var productos = factura.DetallesFactura.Count(d => d.TipoLinea == "P");
            factura.Origen = productos == lineas.Length ? "PRODUCTOS" : productos > 0 ? "MIXTA" : "CITA";
            context.Facturas.Add(factura);
            await context.SaveChangesAsync(ct);
            await ActualizarTotalesAsync(context, factura, ct);
            await context.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return factura.FacturaId;
        }
        catch (Exception ex) when (ex.GetBaseException() is SqlException { Number: 1205 or 2601 or 2627 or 8115 })
        {
            context.ChangeTracker.Clear();
            return await Emitida();
        }
    }
    private IQueryable<Factura> Autorizadas(int usuarioId) => context.Facturas.AsNoTracking()
        .Where(f => context.Usuarios.Any(u => u.UsuarioId == usuarioId && u.Activo
            && (u.Rol.Nombre == "Administrador" || u.Rol.Nombre == "Cliente" && f.ClienteId == u.UsuarioId
                || u.Rol.Nombre == "Encargado" && u.Sucursal != null && u.Sucursal.Activa && u.SucursalId == f.SucursalId)));

    public async Task<IReadOnlyList<Factura>> ListarAsync(CancellationToken cancellationToken, int usuarioId) =>
        await Autorizadas(usuarioId).Include(f => f.Cliente).Include(f => f.EstadoFactura)
            .OrderByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId)
            .ToListAsync(cancellationToken);

    public Task<Factura?> ObtenerAsync(long id, CancellationToken cancellationToken, int usuarioId) =>
        Autorizadas(usuarioId).Include(f => f.Cliente).Include(f => f.Sucursal).Include(f => f.EmitidaPorUsuario)
            .Include(f => f.Cita).ThenInclude(c => c!.EstadoCita)
            .Include(f => f.EstadoFactura).Include(f => f.DetallesFactura).Include(f => f.PagosFactura).AsSplitQuery()
            .SingleOrDefaultAsync(f => f.FacturaId == id, cancellationToken);

    public Task<string?> NombreNegocioAsync(CancellationToken ct) => context.ConfiguracionSistema.AsNoTracking()
        .Where(c => c.Activa).OrderBy(c => c.ConfiguracionSistemaId).Select(c => (string?)c.NombreNegocio).FirstOrDefaultAsync(ct);

    public async Task<HistorialFacturacionDTO> ConsultarAsync(FiltroFacturacionDTO filtro, int usuarioId, CancellationToken ct)
    {
        var query = Autorizadas(usuarioId);
        if (!string.IsNullOrEmpty(filtro.TipoDocumento)) query = query.Where(f => f.TipoDocumento == filtro.TipoDocumento);
        if (filtro.EstadoFacturaId.HasValue) query = query.Where(f => f.EstadoFacturaId == filtro.EstadoFacturaId);
        if (filtro.FechaInicial.HasValue)
        {
            var inicio = filtro.FechaInicial.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(f => f.Fecha >= inicio);
        }
        if (filtro.FechaFinal.HasValue)
        {
            var fin = filtro.FechaFinal.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(f => f.Fecha <= fin);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Cliente))
        {
            var cliente = filtro.Cliente.Trim();
            query = query.Where(f => f.Cliente.NombreCompleto.Contains(cliente)
                || f.Cliente.Identificacion != null && f.Cliente.Identificacion.Contains(cliente));
        }
        if (filtro.NumeroDocumento.HasValue) query = query.Where(f => f.FacturaId == filtro.NumeroDocumento);
        if (filtro.SucursalId.HasValue) query = query.Where(f => f.SucursalId == filtro.SucursalId);

        var resumen = await query.GroupBy(f => 1).Select(g => new ResumenFacturacionDTO(
            g.Count(f => f.TipoDocumento == "FACTURA"),
            g.Count(f => f.TipoDocumento == "PROFORMA" && f.EstadoFactura.Nombre == "Proforma"),
            g.Sum(f => f.TipoDocumento == "FACTURA" && f.EstadoFactura.Nombre != "Anulada" ? f.Total : 0m)))
            .SingleOrDefaultAsync(ct) ?? new ResumenFacturacionDTO(0, 0, 0m);
        var ordenadas = filtro.Orden switch
        {
            "antiguas" => query.OrderBy(f => f.Fecha).ThenBy(f => f.FacturaId),
            "mayor" => query.OrderByDescending(f => f.Total).ThenByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId),
            "menor" => query.OrderBy(f => f.Total).ThenByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId),
            _ => query.OrderByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId)
        };
        var documentos = await ordenadas.Select(f => new FacturaResumenDTO(f.FacturaId, f.Fecha,
            f.Cliente.NombreCompleto, f.Total, f.ClienteId, f.SucursalId, f.TipoDocumento, f.EstadoFactura.Nombre)).ToListAsync(ct);
        var opciones = await OpcionesAsync(usuarioId, ct);
        return opciones with { Documentos = documentos, Resumen = resumen };
    }

    public async Task<HistorialFacturacionDTO> OpcionesAsync(int usuarioId, CancellationToken ct)
    {
        var estados = await context.EstadosFactura.AsNoTracking().OrderBy(e => e.EstadoFacturaId)
            .Select(e => new EstadoFacturaOpcionDTO(e.EstadoFacturaId, e.Nombre)).ToListAsync(ct);
        var sucursales = await context.Sucursales.AsNoTracking().Where(s => context.Usuarios.Any(u =>
            u.UsuarioId == usuarioId && u.Activo && (u.Rol.Nombre == "Administrador"
                || u.Rol.Nombre == "Encargado" && u.SucursalId == s.SucursalId && s.Activa)))
            .OrderBy(s => s.Nombre).Select(s => new SucursalReservaDTO(s.SucursalId, s.Nombre)).ToListAsync(ct);
        return new([], new(0, 0, 0m), estados, sucursales);
    }

    internal static async Task ActualizarTotalesAsync(WashLabsDbContext db, Factura factura, CancellationToken ct)
    {
        var lineas = await db.DetallesFactura.AsNoTracking().Where(d => d.FacturaId == factura.FacturaId).ToListAsync(ct);
        factura.Subtotal = lineas.Sum(d => d.Subtotal ?? 0m);
        factura.Impuesto = lineas.Sum(d => d.Impuesto ?? 0m);
        factura.Total = factura.Subtotal + factura.Impuesto;
    }

    private static bool CantidadValida(decimal cantidad) => cantidad > 0 && cantidad <= 99999999m && decimal.Truncate(cantidad) == cantidad;

    public Task<bool> AgregarProductoAsync(long id, int productoId, decimal cantidad, int usuarioId, CancellationToken ct)
    {
        if (!CantidadValida(cantidad)) return Task.FromResult(false);
        return ModificarOrdenAsync(id, usuarioId, async (factura, token) =>
        {
            var producto = await context.Productos.AsNoTracking().SingleOrDefaultAsync(p => p.ProductoId == productoId && p.Activo, token);
            if (producto is null || producto.Precio < 0) return false;
            if (await context.DetallesFactura.AnyAsync(d => d.FacturaId == id && d.ProductoId == productoId && d.Cantidad == cantidad, token))
                return true;
            context.DetallesFactura.Add(new DetalleFactura { FacturaId = id, TipoLinea = "P", ProductoId = productoId,
                Descripcion = producto.Nombre, Cantidad = cantidad, PrecioUnitario = producto.Precio, PorcentajeImpuesto = 13m });
            return true;
        }, ct);
    }

    public Task<bool> CambiarCantidadAsync(long id, long detalleId, decimal cantidad, int usuarioId, CancellationToken ct)
    {
        if (cantidad == 0) return QuitarProductoAsync(id, detalleId, usuarioId, ct);
        if (!CantidadValida(cantidad)) return Task.FromResult(false);
        return ModificarOrdenAsync(id, usuarioId, async (factura, token) =>
        {
            var linea = await LineasEditables(factura).SingleOrDefaultAsync(d => d.DetalleFacturaId == detalleId, token);
            if (linea is null || linea.PrecioUnitario < 0 || linea.Descuento > cantidad * linea.PrecioUnitario) return false;
            linea.Cantidad = cantidad;
            return true;
        }, ct);
    }

    public Task<bool> QuitarProductoAsync(long id, long detalleId, int usuarioId, CancellationToken ct) =>
        ModificarOrdenAsync(id, usuarioId, async (factura, token) =>
        {
            if (detalleId <= 0) return false;
            var linea = await LineasEditables(factura).Include(d => d.DetalleFacturaPersonalizacion)
                .SingleOrDefaultAsync(d => d.DetalleFacturaId == detalleId, token);
            if (linea is null) return !await context.DetallesFactura.AnyAsync(d => d.DetalleFacturaId == detalleId, token);
            context.DetalleFacturaPersonalizacion.RemoveRange(linea.DetalleFacturaPersonalizacion);
            context.DetallesFactura.Remove(linea);
            return true;
        }, ct);

    public Task<bool> AgregarServicioAsync(long id, int servicioId, decimal cantidad, int usuarioId, CancellationToken ct)
    {
        if (!CantidadValida(cantidad)) return Task.FromResult(false);
        return ModificarOrdenAsync(id, usuarioId, async (factura, token) =>
        {
            var servicio = await context.SucursalServicios.AsNoTracking().Include(s => s.Servicio)
                .SingleOrDefaultAsync(s => s.SucursalId == factura.SucursalId && s.ServicioId == servicioId
                    && s.Activo && s.Servicio.Activo, token);
            if (servicio is null) return false;
            var precio = servicio.PrecioSucursal ?? servicio.Servicio.TarifaBase;
            if (precio < 0) return false;
            if (await context.DetallesFactura.AnyAsync(d => d.FacturaId == id && d.ServicioId == servicioId, token))
                return false;
            context.DetallesFactura.Add(new DetalleFactura { FacturaId = id, TipoLinea = "S", ServicioId = servicioId,
                Descripcion = servicio.Servicio.Nombre, Cantidad = cantidad, PrecioUnitario = precio, PorcentajeImpuesto = 13m });
            return true;
        }, ct);
    }

    private IQueryable<DetalleFactura> LineasEditables(Factura factura) => context.DetallesFactura
        .Where(d => d.FacturaId == factura.FacturaId && (d.TipoLinea == "P" ||
            d.TipoLinea == "S" && !context.Citas.Any(c => c.CitaId == factura.CitaId && c.ServicioId == d.ServicioId)));

    private async Task<bool> ModificarOrdenAsync(long id, int usuarioId, Func<Factura, CancellationToken, Task<bool>> modificar, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var citaId = await Autorizadas(usuarioId).Where(f => f.FacturaId == id).Select(f => f.CitaId).SingleOrDefaultAsync(ct);
            if (!citaId.HasValue) return false;
            var bloqueo = await context.Citas.Where(c => c.CitaId == citaId &&
                c.Sucursal.Activa && c.Cliente.Activo && c.Vehiculo.Activo && c.Vehiculo.ClienteId == c.ClienteId &&
                context.Facturas.Any(f => f.FacturaId == id && f.CitaId == c.CitaId && f.ClienteId == c.ClienteId && f.SucursalId == c.SucursalId) &&
                (c.EstadoCita.Nombre == "Pendiente" || c.EstadoCita.Nombre == "Confirmada" || c.EstadoCita.Nombre == "Reprogramada")
                && context.Usuarios.Any(u => u.UsuarioId == usuarioId && u.Activo &&
                    (u.Rol.Nombre == "Administrador" || u.Rol.Nombre == "Encargado" && u.SucursalId == c.SucursalId && u.Sucursal!.Activa)))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.FechaActualizacion, c => c.FechaActualizacion), ct);
            if (bloqueo != 1) return false;
            var factura = await context.Facturas.Include(f => f.EstadoFactura).SingleAsync(f => f.FacturaId == id, ct);
            if (factura.TipoDocumento != "PROFORMA" || factura.EstadoFactura.Nombre != "Proforma" || !await modificar(factura, ct)) return false;
            await context.SaveChangesAsync(ct);
            factura.Origen = await context.DetallesFactura.AnyAsync(d => d.FacturaId == id && d.TipoLinea == "P", ct) ? "MIXTA" : "CITA";
            await ActualizarTotalesAsync(context, factura, ct);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex.GetBaseException() is SqlException { Number: 1205 or 2601 or 2627 })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            return false;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
    }
}
