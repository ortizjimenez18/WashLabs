namespace WashLabs.Models.DTOs;

public sealed record FacturaResumenDTO(long FacturaId, DateTime Fecha, string Cliente, decimal Total, int ClienteId, int SucursalId, string TipoDocumento = "FACTURA", string Estado = "");
public sealed record FacturaLineaDTO(string Descripcion, string Tipo, decimal PrecioUnitario,
    decimal Cantidad, decimal Descuento, decimal Subtotal, decimal Impuesto = 0m, decimal Total = 0m,
    long DetalleFacturaId = 0, int? ProductoId = null, decimal PorcentajeImpuesto = 13m);
public sealed record ImpuestoFacturaDTO(decimal Porcentaje, decimal Monto);
public sealed record FacturaDTO(long FacturaId, DateTime Fecha, int ClienteId, string Cliente,
    SucursalDTO Sucursal, string Estado, IReadOnlyList<FacturaLineaDTO> Lineas,
    decimal Subtotal, IReadOnlyList<ImpuestoFacturaDTO> Impuestos, decimal Total, string? IdentificacionCliente,
    string TipoDocumento = "FACTURA", long? CitaId = null, bool PermiteProductos = false, string? NombreNegocio = null, decimal MontoPagado = 0m);
