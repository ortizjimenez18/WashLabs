using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class CategoriaProducto
{
    public int CategoriaProductoId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Activa { get; set; }

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
