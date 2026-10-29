using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class PreguntaAsistente
{
    public int PreguntaAsistenteId { get; set; }

    public int CategoriaAsistenteId { get; set; }

    public string TextoPregunta { get; set; } = null!;

    public bool EsInicial { get; set; }

    public int Orden { get; set; }

    public bool Activa { get; set; }

    public virtual CategoriaAsistente CategoriaAsistente { get; set; } = null!;

    public virtual ICollection<InteraccionAsistente> InteraccionesAsistente { get; set; } = new List<InteraccionAsistente>();

    public virtual ICollection<OpcionAsistente> OpcionesAsistentePreguntaAsistente { get; set; } = new List<OpcionAsistente>();

    public virtual ICollection<OpcionAsistente> OpcionesAsistenteSiguientePregunta { get; set; } = new List<OpcionAsistente>();
}
