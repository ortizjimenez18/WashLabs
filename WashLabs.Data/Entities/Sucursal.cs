using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class Sucursal
{
    public int SucursalId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public bool Activa { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<BloqueoAgenda> BloqueosAgenda { get; set; } = new List<BloqueoAgenda>();

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual ICollection<HorarioSucursal> HorariosSucursal { get; set; } = new List<HorarioSucursal>();

    public virtual ICollection<SucursalServicio> SucursalServicios { get; set; } = new List<SucursalServicio>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
