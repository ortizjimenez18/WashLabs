using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;

namespace WashLabs.Data.Repositories;

public sealed class ReservaRepository(WashLabsDbContext context) : IReservaRepository
{
    public async Task<IReadOnlyList<Cita>> ListarClienteAsync(int clienteId, CancellationToken ct) =>
        await context.Citas.AsNoTracking().Where(c => c.ClienteId == clienteId).Include(c => c.Cliente)
            .Include(c => c.Servicio).Include(c => c.EstadoCita).OrderByDescending(c => c.Fecha).ThenByDescending(c => c.HoraInicio).ToListAsync(ct);
    public Task<Cita?> ObtenerClienteAsync(long id, int clienteId, CancellationToken ct) =>
        context.Citas.AsNoTracking().Include(c => c.Sucursal).Include(c => c.Servicio).Include(c => c.EstadoCita).Include(c => c.Vehiculo)
            .Include(c => c.RespuestasReserva).ThenInclude(r => r.PreguntaReserva)
            .SingleOrDefaultAsync(c => c.CitaId == id && c.ClienteId == clienteId, ct);
    public async Task<FormularioReservaDTO?> FormularioAsync(int encargadoId, CancellationToken ct)
    {
        var encargado = await ObtenerEncargadoAsync(encargadoId, ct);
        if (encargado?.Sucursal is null) return null;
        var servicios = await context.SucursalServicios.AsNoTracking()
            .Where(s => s.SucursalId == encargado.SucursalId && s.Activo && s.Servicio.Activo)
            .OrderBy(s => s.Servicio.Nombre)
            .Select(s => new ServicioReservaDTO(s.ServicioId, s.Servicio.Nombre, s.Servicio.DuracionMinutos)).ToListAsync(ct);
        var preguntas = await context.PreguntasReserva.AsNoTracking().Where(p => p.Activa).OrderBy(p => p.Orden)
            .Select(p => new PreguntaReservaDTO(p.PreguntaReservaId, p.Pregunta)).ToListAsync(ct);
        return new(encargado.Sucursal.Nombre, servicios, preguntas);
    }

    public async Task<AgendaReservaDTO?> AgendaAsync(int encargadoId, int servicioId, DateOnly fecha, CancellationToken ct)
    {
        var sucursal = await Encargados(encargadoId).Select(u => u.SucursalId).SingleOrDefaultAsync(ct);
        if (!sucursal.HasValue) return null;
        var duracion = await context.SucursalServicios.AsNoTracking()
            .Where(s => s.SucursalId == sucursal && s.ServicioId == servicioId && s.Activo && s.Servicio.Activo)
            .Select(s => (int?)s.Servicio.DuracionMinutos).SingleOrDefaultAsync(ct);
        if (!duracion.HasValue) return null;
        var dia = (byte)(((int)fecha.DayOfWeek + 6) % 7 + 1);
        var horarios = await context.HorariosSucursal.AsNoTracking().Where(h => h.SucursalId == sucursal && h.Activo
            && h.DiaSemana == dia && h.FechaVigenciaDesde <= fecha && (!h.FechaVigenciaHasta.HasValue || h.FechaVigenciaHasta >= fecha))
            .Select(h => new RangoReservaDTO(h.HoraInicio, h.HoraFin)).ToListAsync(ct);
        var ocupados = await context.Citas.AsNoTracking().Where(c => c.SucursalId == sucursal && c.Fecha == fecha
            && c.EstadoCita.Nombre != "Cancelada" && c.EstadoCita.Nombre != "No asistió")
            .Select(c => new RangoReservaDTO(c.HoraInicio, c.HoraFin)).ToListAsync(ct);
        var diaBloqueado = await context.BloqueosAgenda.AnyAsync(b => b.SucursalId == sucursal && b.Fecha == fecha && b.DiaCompleto, ct);
        var bloqueos = await context.BloqueosAgenda.AsNoTracking().Where(b => b.SucursalId == sucursal && b.Fecha == fecha
            && !b.DiaCompleto && b.HoraInicio.HasValue && b.HoraFin.HasValue)
            .Select(b => new RangoReservaDTO(b.HoraInicio!.Value, b.HoraFin!.Value)).ToListAsync(ct);
        return new(duracion.Value, horarios, ocupados.Concat(bloqueos).ToArray(), diaBloqueado);
    }

    public async Task<ResultadoCrearReservaDTO> CrearAsync(CrearReservaDTO solicitud, int encargadoId, int creadorId, CancellationToken ct)
    {
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        try
        {
            var encargado = await ObtenerEncargadoAsync(encargadoId, ct);
            if (encargado?.Sucursal is null) return new(false, null, "El encargado no tiene una sucursal disponible.");
            if (!await context.Usuarios.AnyAsync(u => u.UsuarioId == solicitud.ClienteId && u.Activo && u.Rol.Nombre == "Cliente", ct))
                return new(false, null, "Selecciona un cliente disponible.");
            if (!await context.Usuarios.AnyAsync(u => u.UsuarioId == creadorId && u.Activo
                && (u.UsuarioId == encargadoId || u.UsuarioId == solicitud.ClienteId || u.Rol.Nombre == "Administrador"), ct))
                return new(false, null, "El usuario no puede registrar esta reserva.");
            if (!await context.Vehiculos.AnyAsync(v => v.VehiculoId == solicitud.VehiculoId && v.Activo && v.ClienteId == solicitud.ClienteId, ct))
                return new(false, null, "El vehículo seleccionado no pertenece al cliente.");
            var preguntas = await context.PreguntasReserva.AsNoTracking().Where(p => p.Activa).Select(p => p.PreguntaReservaId).ToListAsync(ct);
            if (solicitud.Respuestas.Select(r => r.PreguntaId).Distinct().Count() != solicitud.Respuestas.Count
                || preguntas.Count != solicitud.Respuestas.Count || solicitud.Respuestas.Any(r => !preguntas.Contains(r.PreguntaId) || string.IsNullOrWhiteSpace(r.Respuesta)))
                return new(false, null, "Responde todas las preguntas de la reserva.");
            var agenda = await AgendaAsync(encargadoId, solicitud.ServicioId, solicitud.Fecha!.Value, ct);
            if (!DisponibilidadReserva.Calcular(agenda, solicitud.Fecha.Value).Contains(solicitud.HoraInicio!.Value))
                return new(false, null, "El horario no está disponible. Selecciona otro horario.");
            var estado = await context.EstadosCita.Where(e => e.Nombre == "Pendiente").Select(e => (byte?)e.EstadoCitaId).SingleOrDefaultAsync(ct);
            if (!estado.HasValue) return new(false, null, "No está configurado el estado inicial de las reservas.");
            var cita = new Cita { ClienteId = solicitud.ClienteId, VehiculoId = solicitud.VehiculoId,
                SucursalId = encargado.SucursalId!.Value, ServicioId = solicitud.ServicioId, Fecha = solicitud.Fecha.Value,
                HoraInicio = solicitud.HoraInicio.Value, HoraFin = solicitud.HoraInicio.Value.AddMinutes(agenda!.DuracionMinutos),
                EstadoCitaId = estado.Value, CreadoPorUsuarioId = creadorId, Observaciones = solicitud.Observaciones?.Trim(),
                RespuestasReserva = solicitud.Respuestas.Select(r => new RespuestaReserva { PreguntaReservaId = r.PreguntaId, Respuesta = r.Respuesta.Trim() }).ToList() };
            context.Citas.Add(cita);
            await context.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return new(true, cita.CitaId, null);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: >= 50001 and <= 50005 })
        { context.ChangeTracker.Clear(); return new(false, null, "No se pudo reservar ese horario. Consulta la disponibilidad nuevamente."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 1205 })
        { context.ChangeTracker.Clear(); return new(false, null, "El horario acaba de cambiar. Consulta la disponibilidad nuevamente."); }
        catch (SqlException ex) when (ex.Number == 1205)
        { context.ChangeTracker.Clear(); return new(false, null, "El horario acaba de cambiar. Consulta la disponibilidad nuevamente."); }
    }
    private IQueryable<Usuario> Encargados(int encargadoId) =>
        context.Usuarios.AsNoTracking().Where(u => u.UsuarioId == encargadoId && u.Activo
            && u.Rol.Nombre == "Encargado" && u.Sucursal != null && u.Sucursal.Activa);

    // El alcance de sucursal se aplica en SQL tanto al historial como al detalle.
    private IQueryable<Cita> DeEncargado(int encargadoId) => context.Citas.AsNoTracking()
        .Where(c => c.Cliente.Activo && c.Vehiculo.Activo && Encargados(encargadoId).Any(u => u.SucursalId == c.SucursalId));

    public Task<Usuario?> ObtenerEncargadoAsync(int encargadoId, CancellationToken cancellationToken) =>
        Encargados(encargadoId).Include(u => u.Sucursal).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Cita>> ListarAsync(int encargadoId, CancellationToken cancellationToken) =>
        await DeEncargado(encargadoId).Include(c => c.Cliente).Include(c => c.Servicio)
            .Include(c => c.EstadoCita).OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.HoraInicio).ThenByDescending(c => c.CitaId).ToListAsync(cancellationToken);

    public Task<Cita?> ObtenerAsync(long id, int encargadoId, CancellationToken cancellationToken) =>
        DeEncargado(encargadoId).Include(c => c.Sucursal).Include(c => c.Servicio).Include(c => c.EstadoCita).Include(c => c.Vehiculo)
            .Include(c => c.RespuestasReserva).ThenInclude(r => r.PreguntaReserva)
            .SingleOrDefaultAsync(c => c.CitaId == id, cancellationToken);
}
