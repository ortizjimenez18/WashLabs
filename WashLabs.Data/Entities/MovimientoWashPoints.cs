using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class MovimientoWashPoints
{
    public long MovimientoWashPointsId { get; set; }

    public long TarjetaWashPointsId { get; set; }

    public long? CitaId { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public int Cantidad { get; set; }

    public int Ciclo { get; set; }

    public string Descripcion { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public virtual Cita? Cita { get; set; }

    public virtual TarjetaWashPoints TarjetaWashPoints { get; set; } = null!;
}
