using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class BloqueoAgenda
{
    public long BloqueoAgendaId { get; set; }

    public int SucursalId { get; set; }

    public DateOnly Fecha { get; set; }

    public bool DiaCompleto { get; set; }

    public TimeOnly? HoraInicio { get; set; }

    public TimeOnly? HoraFin { get; set; }

    public string Motivo { get; set; } = null!;

    public int CreadoPorUsuarioId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Usuario CreadoPorUsuario { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;
}
