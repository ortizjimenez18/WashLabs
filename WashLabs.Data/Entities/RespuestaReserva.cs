using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class RespuestaReserva
{
    public long RespuestaReservaId { get; set; }

    public long CitaId { get; set; }

    public int PreguntaReservaId { get; set; }

    public string Respuesta { get; set; } = null!;

    public virtual Cita Cita { get; set; } = null!;

    public virtual PreguntaReserva PreguntaReserva { get; set; } = null!;
}
