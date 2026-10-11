using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class MetodoPago
{
    public byte MetodoPagoId { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<PagoFactura> PagosFactura { get; set; } = new List<PagoFactura>();
}
