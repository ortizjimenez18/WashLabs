using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class Servicio
{
    public int ServicioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public decimal TarifaBase { get; set; }

    public int DuracionMinutos { get; set; }

    public string TipoLavado { get; set; } = null!;

    public string NivelDetalle { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public virtual ICollection<DetalleFactura> DetallesFactura { get; set; } = new List<DetalleFactura>();

    public virtual ICollection<SucursalServicio> SucursalServicios { get; set; } = new List<SucursalServicio>();
}
