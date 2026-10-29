using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class EstadoFactura
{
    public byte EstadoFacturaId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
}
