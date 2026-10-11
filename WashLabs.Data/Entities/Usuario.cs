using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public byte RolId { get; set; }

    public int? SucursalId { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public DateOnly FechaNacimiento { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual ICollection<BloqueoAgenda> BloqueosAgenda { get; set; } = new List<BloqueoAgenda>();

    public virtual ICollection<Cita> CitasCliente { get; set; } = new List<Cita>();

    public virtual ICollection<Cita> CitasCreadoPorUsuario { get; set; } = new List<Cita>();

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual ICollection<HistorialEstadoCita> HistorialEstadoCita { get; set; } = new List<HistorialEstadoCita>();

    public virtual Rol Rol { get; set; } = null!;

    public virtual ICollection<SesionAsistente> SesionesAsistente { get; set; } = new List<SesionAsistente>();

    public virtual Sucursal? Sucursal { get; set; }

    public virtual ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}
