using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class Producto
{
    public int ProductoId { get; set; }

    public int CategoriaProductoId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public decimal Precio { get; set; }

    public string? Marca { get; set; }

    public string? Presentacion { get; set; }

    public string? SKU { get; set; }

    public string? ImagenUrl { get; set; }

    public bool EsPersonalizable { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual CategoriaProducto CategoriaProducto { get; set; } = null!;

    public virtual ICollection<CriterioProducto> CriteriosProducto { get; set; } = new List<CriterioProducto>();

    public virtual ICollection<DetalleFactura> DetallesFactura { get; set; } = new List<DetalleFactura>();
}
