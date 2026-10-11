using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class Vehiculo
{
    public int VehiculoId { get; set; }

    public int ClienteId { get; set; }

    public string Matricula { get; set; } = null!;

    public string Marca { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public short? Anio { get; set; }

    public string? Color { get; set; }

    public string CategoriaTamano { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public virtual Usuario Cliente { get; set; } = null!;

    public virtual TarjetaWashPoints? TarjetasWashPoints { get; set; }
}
