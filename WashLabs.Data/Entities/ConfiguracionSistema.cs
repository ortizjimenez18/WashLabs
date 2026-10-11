using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class ConfiguracionSistema
{
    public byte ConfiguracionSistemaId { get; set; }

    public string NombreNegocio { get; set; } = null!;

    public string DescripcionNegocio { get; set; } = null!;

    public decimal PorcentajeIVA { get; set; }

    public string? CorreoNegocio { get; set; }

    public string? TelefonoNegocio { get; set; }

    public bool Activa { get; set; }
}
