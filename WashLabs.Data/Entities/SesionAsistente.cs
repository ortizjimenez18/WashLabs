using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class SesionAsistente
{
    public long SesionAsistenteId { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public virtual ICollection<InteraccionAsistente> InteraccionesAsistente { get; set; } = new List<InteraccionAsistente>();

    public virtual Usuario? Usuario { get; set; }
}
