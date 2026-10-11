using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class OpcionAsistente
{
    public int OpcionAsistenteId { get; set; }

    public int PreguntaAsistenteId { get; set; }

    public string TextoOpcion { get; set; } = null!;

    public string? RespuestaFinal { get; set; }

    public int? SiguientePreguntaId { get; set; }

    public int Orden { get; set; }

    public bool Activa { get; set; }

    public virtual ICollection<InteraccionAsistente> InteraccionesAsistente { get; set; } = new List<InteraccionAsistente>();

    public virtual PreguntaAsistente PreguntaAsistente { get; set; } = null!;

    public virtual PreguntaAsistente? SiguientePregunta { get; set; }
}
