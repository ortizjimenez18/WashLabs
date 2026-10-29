using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class HistorialEstadoCita
{
    public long HistorialEstadoCitaId { get; set; }

    public long CitaId { get; set; }

    public byte? EstadoAnteriorId { get; set; }

    public byte EstadoNuevoId { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime FechaCambio { get; set; }

    public string? Observacion { get; set; }

    public virtual Cita Cita { get; set; } = null!;

    public virtual EstadoCita? EstadoAnterior { get; set; }

    public virtual EstadoCita EstadoNuevo { get; set; } = null!;

    public virtual Usuario? Usuario { get; set; }
}
