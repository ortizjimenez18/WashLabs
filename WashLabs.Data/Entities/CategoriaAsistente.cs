using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class CategoriaAsistente
{
    public int CategoriaAsistenteId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public bool Activa { get; set; }

    public virtual ICollection<PreguntaAsistente> PreguntasAsistente { get; set; } = new List<PreguntaAsistente>();
}
