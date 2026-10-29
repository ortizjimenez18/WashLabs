using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class Factura
{
    public long FacturaId { get; set; }

    public long? CitaId { get; set; }

    public int ClienteId { get; set; }

    public int SucursalId { get; set; }

    public byte EstadoFacturaId { get; set; }

    public string TipoDocumento { get; set; } = null!;

    public string Origen { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string? RutaPdf { get; set; }

    public bool EnviadaCorreo { get; set; }

    public DateTime? FechaEnvioCorreo { get; set; }

    public string? Observaciones { get; set; }

    public virtual Cita? Cita { get; set; }

    public virtual Usuario Cliente { get; set; } = null!;

    public virtual ICollection<DetalleFactura> DetallesFactura { get; set; } = new List<DetalleFactura>();

    public virtual EstadoFactura EstadoFactura { get; set; } = null!;

    public virtual ICollection<PagoFactura> PagosFactura { get; set; } = new List<PagoFactura>();

    public virtual Sucursal Sucursal { get; set; } = null!;
}
