using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class NotificacionCita
{
    public long NotificacionCitaId { get; set; }

    public long CitaId { get; set; }

    public string Canal { get; set; } = null!;

    public string Destinatario { get; set; } = null!;

    public DateTime FechaProgramada { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public string Estado { get; set; } = null!;

    public string? DetalleError { get; set; }

    public virtual Cita Cita { get; set; } = null!;
}
