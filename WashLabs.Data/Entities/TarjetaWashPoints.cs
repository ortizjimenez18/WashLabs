using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class TarjetaWashPoints
{
    public long TarjetaWashPointsId { get; set; }

    public int VehiculoId { get; set; }

    public int CicloActual { get; set; }

    public int LavadosAcumulados { get; set; }

    public bool RecompensaDisponible { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual ICollection<MovimientoWashPoints> MovimientosWashPoints { get; set; } = new List<MovimientoWashPoints>();

    public virtual ICollection<RecompensaWashPoints> RecompensasWashPoints { get; set; } = new List<RecompensaWashPoints>();

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
