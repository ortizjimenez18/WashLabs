using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class PreguntaReserva
{
    public int PreguntaReservaId { get; set; }

    public string Pregunta { get; set; } = null!;

    public string TipoRespuesta { get; set; } = null!;

    public bool Obligatoria { get; set; }

    public int Orden { get; set; }

    public bool Activa { get; set; }

    public virtual ICollection<RespuestaReserva> RespuestasReserva { get; set; } = new List<RespuestaReserva>();
}
