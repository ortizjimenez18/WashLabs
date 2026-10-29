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
            .Include(c => c.Facturas)
            .Include(c => c.RespuestasReserva).ThenInclude(r => r.PreguntaReserva)
            .SingleOrDefaultAsync(c => c.CitaId == id && c.ClienteId == clienteId, ct);
    private IQueryable<Sucursal> SucursalesPermitidas(int usuarioId) => context.Sucursales.AsNoTracking()
        .Where(s => s.Activa && context.Usuarios.Any(u => u.UsuarioId == usuarioId && u.Activo
            && (u.Rol.Nombre == "Administrador" || u.Rol.Nombre == "Cliente"
                || u.Rol.Nombre == "Encargado" && u.SucursalId == s.SucursalId)));

    public async Task<FormularioReservaDTO?> FormularioAsync(int encargadoId, CancellationToken ct, int? sucursalId = null)
    {
        var sucursales = await SucursalesPermitidas(encargadoId).OrderBy(s => s.Nombre)
            .Select(s => new SucursalReservaDTO(s.SucursalId, s.Nombre)).ToListAsync(ct);
        var sucursal = sucursalId.HasValue ? sucursales.SingleOrDefault(s => s.SucursalId == sucursalId)
            : sucursales.FirstOrDefault();
        if (sucursal is null) return null;
        var servicios = await context.SucursalServicios.AsNoTracking()
            .Where(s => s.SucursalId == sucursal.SucursalId && s.Activo && s.Servicio.Activo)
            .OrderBy(s => s.Servicio.Nombre)
            .Select(s => new ServicioReservaDTO(s.ServicioId, s.Servicio.Nombre, s.Servicio.DuracionMinutos)).ToListAsync(ct);
        var preguntas = await context.PreguntasReserva.AsNoTracking().Where(p => p.Activa).OrderBy(p => p.Orden)
            .Select(p => new PreguntaReservaDTO(p.PreguntaReservaId, p.Pregunta)).ToListAsync(ct);
        return new(sucursal.Nombre, servicios, preguntas, sucursal.SucursalId, sucursales);
    }

    public async Task<AgendaReservaDTO?> AgendaAsync(int encargadoId, int servicioId, DateOnly fecha, CancellationToken ct, int? sucursalId = null)
    {
        var sucursal = await SucursalesPermitidas(encargadoId).Where(s => s.SucursalId == sucursalId)
            .Select(s => (int?)s.SucursalId).SingleOrDefaultAsync(ct);
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
            if (creadorId != encargadoId || !await SucursalesPermitidas(creadorId).AnyAsync(s => s.SucursalId == solicitud.SucursalId, ct))
                return new(false, null, "La sucursal seleccionada no está disponible para este usuario.");
            if (!await context.Usuarios.AnyAsync(u => u.UsuarioId == solicitud.ClienteId && u.Activo && u.Rol.Nombre == "Cliente", ct))
                return new(false, null, "Selecciona un cliente disponible.");
            if (!await context.Usuarios.AnyAsync(u => u.UsuarioId == creadorId && u.Activo
                && (u.Rol.Nombre == "Encargado" || u.UsuarioId == solicitud.ClienteId && u.Rol.Nombre == "Cliente" || u.Rol.Nombre == "Administrador"), ct))
                return new(false, null, "El usuario no puede registrar esta reserva.");
            var vehiculoPermitido = await context.Vehiculos.Where(v => v.VehiculoId == solicitud.VehiculoId
                && v.Activo && v.ClienteId == solicitud.ClienteId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Activo, v => v.Activo), ct);
            if (vehiculoPermitido != 1) return new(false, null, "El vehículo seleccionado no pertenece al cliente.");
            var repetida = await context.Citas.Where(c => c.ClienteId == solicitud.ClienteId
                && c.VehiculoId == solicitud.VehiculoId && c.SucursalId == solicitud.SucursalId
                && c.ServicioId == solicitud.ServicioId && c.Fecha == solicitud.Fecha && c.HoraInicio == solicitud.HoraInicio
                && c.EstadoCita.Nombre != "Cancelada" && c.EstadoCita.Nombre != "No asistió"
                && c.Facturas != null).Select(c => (long?)c.CitaId).SingleOrDefaultAsync(ct);
            if (repetida.HasValue) return new(true, repetida.Value, null);
            var preguntas = await context.PreguntasReserva.AsNoTracking().Where(p => p.Activa).Select(p => p.PreguntaReservaId).ToListAsync(ct);
            if (solicitud.Respuestas.Select(r => r.PreguntaId).Distinct().Count() != solicitud.Respuestas.Count
                || preguntas.Count != solicitud.Respuestas.Count || solicitud.Respuestas.Any(r => !preguntas.Contains(r.PreguntaId) || string.IsNullOrWhiteSpace(r.Respuesta)))
                return new(false, null, "Responde todas las preguntas de la reserva.");
            var agenda = await AgendaAsync(encargadoId, solicitud.ServicioId, solicitud.Fecha!.Value, ct, solicitud.SucursalId);
            if (!DisponibilidadReserva.Calcular(agenda, solicitud.Fecha.Value).Contains(solicitud.HoraInicio!.Value))
                return new(false, null, "El horario no está disponible. Selecciona otro horario.");
            var fin = solicitud.HoraInicio.Value.AddMinutes(agenda!.DuracionMinutos);
            if (await context.Citas.AnyAsync(c => c.VehiculoId == solicitud.VehiculoId && c.Fecha == solicitud.Fecha
                && c.HoraInicio < fin && c.HoraFin > solicitud.HoraInicio
                && c.EstadoCita.Nombre != "Cancelada" && c.EstadoCita.Nombre != "No asistió", ct))
                return new(false, null, "El vehículo ya tiene una reserva en ese horario.");
            var tarifa = await context.SucursalServicios.Include(s => s.Servicio).SingleAsync(s =>
                s.SucursalId == solicitud.SucursalId && s.ServicioId == solicitud.ServicioId && s.Activo && s.Servicio.Activo, ct);
            var precio = tarifa.PrecioSucursal ?? tarifa.Servicio.TarifaBase;
            if (precio < 0) return new(false, null, "El servicio no tiene un precio válido.");
            var descripcion = string.IsNullOrWhiteSpace(tarifa.Servicio.Descripcion) ? tarifa.Servicio.Nombre
                : tarifa.Servicio.Nombre + ": " + tarifa.Servicio.Descripcion;
            var estadoProforma = await context.EstadosFactura.Where(e => e.Nombre == "Proforma")
                .Select(e => e.EstadoFacturaId).SingleAsync(ct);
            var estado = await context.EstadosCita.Where(e => e.Nombre == "Pendiente").Select(e => (byte?)e.EstadoCitaId).SingleOrDefaultAsync(ct);
            if (!estado.HasValue) return new(false, null, "No está configurado el estado inicial de las reservas.");
            var cita = new Cita { ClienteId = solicitud.ClienteId, VehiculoId = solicitud.VehiculoId,
                SucursalId = solicitud.SucursalId, ServicioId = solicitud.ServicioId, Fecha = solicitud.Fecha.Value,
                HoraInicio = solicitud.HoraInicio.Value, HoraFin = solicitud.HoraInicio.Value.AddMinutes(agenda!.DuracionMinutos),
                EstadoCitaId = estado.Value, CreadoPorUsuarioId = creadorId, Observaciones = solicitud.Observaciones?.Trim(),
                RespuestasReserva = solicitud.Respuestas.Select(r => new RespuestaReserva { PreguntaReservaId = r.PreguntaId, Respuesta = r.Respuesta.Trim() }).ToList() };
            context.Citas.Add(cita);
            await context.SaveChangesAsync(ct);
            var proforma = new Factura { CitaId = cita.CitaId, ClienteId = cita.ClienteId,
                SucursalId = cita.SucursalId, EstadoFacturaId = estadoProforma, TipoDocumento = "PROFORMA", Origen = "CITA",
                DetallesFactura = [new DetalleFactura { TipoLinea = "S", ServicioId = cita.ServicioId,
                    Descripcion = descripcion.Length > 300 ? descripcion[..300] : descripcion,
                    Cantidad = 1m, PrecioUnitario = precio, PorcentajeImpuesto = 13m }] };
            context.Facturas.Add(proforma);
            await context.SaveChangesAsync(ct);
            await FacturaRepository.ActualizarTotalesAsync(context, proforma, ct);
            await context.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return new(true, cita.CitaId, null);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: >= 50001 and <= 50005 })
        { context.ChangeTracker.Clear(); return new(false, null, "No se pudo reservar ese horario. Consulta la disponibilidad nuevamente."); }
        catch (Exception ex) when (ex.GetBaseException() is SqlException { Number: 1205 })
        { context.ChangeTracker.Clear(); return new(false, null, "El horario acaba de cambiar. Consulta la disponibilidad nuevamente."); }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
    }
    private IQueryable<Usuario> Encargados(int encargadoId) =>
        context.Usuarios.AsNoTracking().Where(u => u.UsuarioId == encargadoId && u.Activo
            && (u.Rol.Nombre == "Administrador" || u.Rol.Nombre == "Encargado" && u.Sucursal != null && u.Sucursal.Activa));

    // El alcance de sucursal se aplica en SQL tanto al historial como al detalle.
    private IQueryable<Cita> DeEncargado(int encargadoId) => context.Citas.AsNoTracking()
        .Where(c => Encargados(encargadoId).Any(u => u.Rol.Nombre == "Administrador" || u.SucursalId == c.SucursalId));

    public Task<Usuario?> ObtenerEncargadoAsync(int encargadoId, CancellationToken cancellationToken) =>
        Encargados(encargadoId).Include(u => u.Sucursal).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Cita>> ListarAsync(int encargadoId, CancellationToken cancellationToken) =>
        await DeEncargado(encargadoId).Include(c => c.Cliente).Include(c => c.Servicio)
            .Include(c => c.EstadoCita).OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.HoraInicio).ThenByDescending(c => c.CitaId).ToListAsync(cancellationToken);

    public Task<Cita?> ObtenerAsync(long id, int encargadoId, CancellationToken cancellationToken) =>
        DeEncargado(encargadoId).Include(c => c.Sucursal).Include(c => c.Servicio).Include(c => c.EstadoCita).Include(c => c.Vehiculo)
            .Include(c => c.Facturas)
            .Include(c => c.RespuestasReserva).ThenInclude(r => r.PreguntaReserva)
            .SingleOrDefaultAsync(c => c.CitaId == id, cancellationToken);
}
