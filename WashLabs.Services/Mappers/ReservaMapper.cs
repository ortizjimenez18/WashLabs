using System.Text.RegularExpressions;
using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Services.Mappers;

public static partial class ReservaMapper
{
    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorHexValido();

    public static EstadoReservaDTO ToDTO(EstadoCita estado)
    {
        var color = estado.ColorHex.Trim();
        return new(estado.Nombre, ColorHexValido().IsMatch(color) ? color : "#73787D");
    }

    public static ReservaResumenDTO ToResumenDTO(Cita cita) => new(cita.CitaId, cita.Fecha,
        cita.HoraInicio, cita.Cliente.NombreCompleto, cita.Servicio.Nombre, ToDTO(cita.EstadoCita));

    public static ReservaDTO ToDTO(Cita cita) => new(cita.CitaId, cita.FechaCreacion, cita.Fecha,
        cita.HoraInicio, cita.HoraFin, cita.Sucursal.Nombre, cita.Servicio.Nombre, ToDTO(cita.EstadoCita),
        cita.RespuestasReserva.OrderBy(r => r.PreguntaReserva.Orden).ThenBy(r => r.PreguntaReservaId)
            .Select(r => new RespuestaReservaDTO(r.PreguntaReserva.Pregunta, r.Respuesta)).ToArray(), cita.Vehiculo.Matricula, cita.ClienteId, cita.Facturas?.FacturaId);
}
