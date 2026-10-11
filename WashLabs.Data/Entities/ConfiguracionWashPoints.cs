using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class ConfiguracionWashPoints
{
    public byte ConfiguracionWashPointsId { get; set; }

    public byte MetaLavados { get; set; }

    public string DescripcionRecompensa { get; set; } = null!;

    public bool Activa { get; set; }
}
