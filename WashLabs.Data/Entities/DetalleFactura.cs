using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class DetalleFactura
{
    public long DetalleFacturaId { get; set; }

    public long FacturaId { get; set; }

    public string TipoLinea { get; set; } = null!;

    public int? ServicioId { get; set; }

    public int? ProductoId { get; set; }

    public string Descripcion { get; set; } = null!;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal PorcentajeImpuesto { get; set; }

    public decimal Descuento { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? Impuesto { get; set; }

    public decimal? Total { get; set; }

    public virtual ICollection<DetalleFacturaPersonalizacion> DetalleFacturaPersonalizacion { get; set; } = new List<DetalleFacturaPersonalizacion>();

    public virtual Factura Factura { get; set; } = null!;

    public virtual Producto? Producto { get; set; }

    public virtual Servicio? Servicio { get; set; }
}
