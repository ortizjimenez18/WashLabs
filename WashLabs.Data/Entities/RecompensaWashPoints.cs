using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class RecompensaWashPoints
{
    public long RecompensaWashPointsId { get; set; }

    public long TarjetaWashPointsId { get; set; }

    public int Ciclo { get; set; }

    public string Descripcion { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaGeneracion { get; set; }

    public DateTime? FechaCanje { get; set; }

    public virtual TarjetaWashPoints TarjetaWashPoints { get; set; } = null!;
}
