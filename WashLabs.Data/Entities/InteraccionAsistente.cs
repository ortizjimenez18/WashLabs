using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class InteraccionAsistente
{
    public long InteraccionAsistenteId { get; set; }

    public long SesionAsistenteId { get; set; }

    public int PreguntaAsistenteId { get; set; }

    public int OpcionAsistenteId { get; set; }

    public DateTime Fecha { get; set; }

    public virtual OpcionAsistente OpcionAsistente { get; set; } = null!;

    public virtual PreguntaAsistente PreguntaAsistente { get; set; } = null!;

    public virtual SesionAsistente SesionAsistente { get; set; } = null!;
}
