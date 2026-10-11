using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class EstadoCita
{
    public byte EstadoCitaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string ColorHex { get; set; } = null!;

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public virtual ICollection<HistorialEstadoCita> HistorialEstadoCitaEstadoAnterior { get; set; } = new List<HistorialEstadoCita>();

    public virtual ICollection<HistorialEstadoCita> HistorialEstadoCitaEstadoNuevo { get; set; } = new List<HistorialEstadoCita>();
}
