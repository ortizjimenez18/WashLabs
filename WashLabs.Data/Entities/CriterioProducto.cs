using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class CriterioProducto
{
    public int CriterioProductoId { get; set; }

    public int ProductoId { get; set; }

    public string Nombre { get; set; } = null!;

    public bool EsModificable { get; set; }

    public bool Obligatorio { get; set; }

    public int Orden { get; set; }

    public virtual ICollection<DetalleFacturaPersonalizacion> DetalleFacturaPersonalizacion { get; set; } = new List<DetalleFacturaPersonalizacion>();

    public virtual ICollection<OpcionCriterioProducto> OpcionesCriterioProducto { get; set; } = new List<OpcionCriterioProducto>();

    public virtual Producto Producto { get; set; } = null!;
}
