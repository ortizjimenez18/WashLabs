using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class DetalleFacturaPersonalizacion
{
    public long DetalleFacturaPersonalizacionId { get; set; }

    public long DetalleFacturaId { get; set; }

    public int CriterioProductoId { get; set; }

    public int OpcionCriterioId { get; set; }

    public virtual CriterioProducto CriterioProducto { get; set; } = null!;

    public virtual DetalleFactura DetalleFactura { get; set; } = null!;

    public virtual OpcionCriterioProducto OpcionCriterio { get; set; } = null!;
}
