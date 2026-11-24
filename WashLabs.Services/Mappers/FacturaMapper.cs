using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Services.Mappers;

public static class FacturaMapper
{
    public static FacturaResumenDTO ToResumenDTO(Factura factura) =>
        new(factura.FacturaId, factura.Fecha, factura.Cliente.NombreCompleto, factura.Total, factura.ClienteId, factura.SucursalId, factura.TipoDocumento, factura.EstadoFactura?.Nombre ?? "");

    public static FacturaDTO ToDTO(Factura factura) => new(factura.FacturaId, factura.Fecha,
        factura.ClienteId, factura.Cliente.NombreCompleto, InicioMapper.ToDTO(factura.Sucursal),
        factura.EstadoFactura.Nombre,
        factura.DetallesFactura.OrderBy(d => d.DetalleFacturaId).Select(d => new FacturaLineaDTO(
            NombreCatalogo.ParaMostrar(d.Descripcion), d.TipoLinea == "P" ? "Producto" : "Servicio", d.PrecioUnitario,
            d.Cantidad, d.Descuento, d.Subtotal ?? 0m, d.Impuesto ?? 0m, d.Total ?? 0m,
            d.DetalleFacturaId, d.ProductoId, d.PorcentajeImpuesto,
            d.TipoLinea == "P" || d.TipoLinea == "S" && d.ServicioId != factura.Cita?.ServicioId)).ToArray(),
        factura.Subtotal,
        factura.DetallesFactura.GroupBy(d => d.PorcentajeImpuesto).OrderBy(g => g.Key)
            .Select(g => new ImpuestoFacturaDTO(g.Key, g.Sum(d => d.Impuesto ?? 0m))).ToArray(),
        factura.Total, factura.Cliente.Identificacion, factura.TipoDocumento, factura.CitaId,
        factura.TipoDocumento == "PROFORMA" && factura.EstadoFactura.Nombre == "Proforma"
            && factura.Cita?.EstadoCita.Nombre is ("Pendiente" or "Confirmada" or "Reprogramada"),
        MontoPagado: factura.PagosFactura.Sum(p => p.Monto), CorreoCliente: factura.Cliente.Correo,
        TipoCambio: factura.TipoCambioVenta is > 0 && factura.FechaTipoCambioVenta is { } dia
            ? new TipoCambioVentaDTO(factura.TipoCambioVenta.Value, dia) : null,
        Encargado: factura.EmitidaPorUsuario?.NombreCompleto,
        TieneCanjeWashPoints: factura.Observaciones == CanjeWashPoints.Concepto);
}
