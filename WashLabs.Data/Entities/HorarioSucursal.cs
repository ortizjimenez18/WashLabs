using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class HorarioSucursal
{
    public int HorarioSucursalId { get; set; }

    public int SucursalId { get; set; }

    public byte DiaSemana { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public DateOnly FechaVigenciaDesde { get; set; }

    public DateOnly? FechaVigenciaHasta { get; set; }

    public bool Activo { get; set; }

    public virtual Sucursal Sucursal { get; set; } = null!;
}
