using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Services.Mappers;

public static class FacturaMapper
{
    public static FacturaResumenDTO ToResumenDTO(Factura factura) =>
        new(factura.FacturaId, factura.Fecha, factura.Cliente.NombreCompleto, factura.Total, factura.ClienteId, factura.SucursalId);

    public static FacturaDTO ToDTO(Factura factura) => new(factura.FacturaId, factura.Fecha,
        factura.ClienteId, factura.Cliente.NombreCompleto, InicioMapper.ToDTO(factura.Sucursal),
        factura.EstadoFactura.Nombre,
        factura.DetallesFactura.OrderBy(d => d.DetalleFacturaId).Select(d => new FacturaLineaDTO(
            d.Descripcion, d.TipoLinea == "P" ? "Producto" : "Servicio", d.PrecioUnitario,
            d.Cantidad, d.Descuento, d.Subtotal ?? 0m)).ToArray(),
        factura.Subtotal,
        factura.DetallesFactura.GroupBy(d => d.PorcentajeImpuesto).OrderBy(g => g.Key)
            .Select(g => new ImpuestoFacturaDTO(g.Key, g.Sum(d => d.Impuesto ?? 0m))).ToArray(),
        factura.Total, factura.Cliente.Identificacion);
}
