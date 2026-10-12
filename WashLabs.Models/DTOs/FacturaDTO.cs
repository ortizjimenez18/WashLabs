namespace WashLabs.Models.DTOs;

public sealed record FacturaResumenDTO(long FacturaId, DateTime Fecha, string Cliente, decimal Total, int ClienteId, int SucursalId);
public sealed record FacturaLineaDTO(string Descripcion, string Tipo, decimal PrecioUnitario,
    decimal Cantidad, decimal Descuento, decimal Subtotal);
public sealed record ImpuestoFacturaDTO(decimal Porcentaje, decimal Monto);
public sealed record FacturaDTO(long FacturaId, DateTime Fecha, int ClienteId, string Cliente,
    SucursalDTO Sucursal, string Estado, IReadOnlyList<FacturaLineaDTO> Lineas,
    decimal Subtotal, IReadOnlyList<ImpuestoFacturaDTO> Impuestos, decimal Total, string? IdentificacionCliente);
