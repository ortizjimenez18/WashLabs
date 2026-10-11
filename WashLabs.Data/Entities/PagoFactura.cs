using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class PagoFactura
{
    public long PagoFacturaId { get; set; }

    public long FacturaId { get; set; }

    public byte MetodoPagoId { get; set; }

    public decimal Monto { get; set; }

    public string? Referencia { get; set; }

    public DateTime FechaPago { get; set; }

    public virtual Factura Factura { get; set; } = null!;

    public virtual MetodoPago MetodoPago { get; set; } = null!;
}
