using System;
using System.Collections.Generic;

namespace WashLabs.Data.Entities;

public partial class Cita
{
    public long CitaId { get; set; }

    public int ClienteId { get; set; }

    public int SucursalId { get; set; }

    public int ServicioId { get; set; }

    public int VehiculoId { get; set; }

    public byte EstadoCitaId { get; set; }

    public DateOnly Fecha { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public string? Observaciones { get; set; }

    public bool Notificado { get; set; }

    public DateTime? FechaNotificacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Usuario Cliente { get; set; } = null!;

    public virtual Usuario CreadoPorUsuario { get; set; } = null!;

    public virtual EstadoCita EstadoCita { get; set; } = null!;

    public virtual Factura? Facturas { get; set; }

    public virtual ICollection<HistorialEstadoCita> HistorialEstadoCita { get; set; } = new List<HistorialEstadoCita>();

    public virtual MovimientoWashPoints? MovimientosWashPoints { get; set; }

    public virtual ICollection<NotificacionCita> NotificacionesCita { get; set; } = new List<NotificacionCita>();

    public virtual ICollection<RespuestaReserva> RespuestasReserva { get; set; } = new List<RespuestaReserva>();

    public virtual Servicio Servicio { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
