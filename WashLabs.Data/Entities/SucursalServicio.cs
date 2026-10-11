using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class SucursalServicio
{
    public int SucursalServicioId { get; set; }

    public int SucursalId { get; set; }

    public int ServicioId { get; set; }

    public decimal? PrecioSucursal { get; set; }

    public bool Activo { get; set; }

    public virtual Servicio Servicio { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;
}
