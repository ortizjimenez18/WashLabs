using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class OpcionCriterioProducto
{
    public int OpcionCriterioId { get; set; }

    public int CriterioProductoId { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal AjustePrecio { get; set; }

    public bool Activa { get; set; }

    public virtual CriterioProducto CriterioProducto { get; set; } = null!;

    public virtual ICollection<DetalleFacturaPersonalizacion> DetalleFacturaPersonalizacion { get; set; } = new List<DetalleFacturaPersonalizacion>();
}
