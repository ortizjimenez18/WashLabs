using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class Rol
{
    public byte RolId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual Usuario? Usuarios { get; set; }
}
