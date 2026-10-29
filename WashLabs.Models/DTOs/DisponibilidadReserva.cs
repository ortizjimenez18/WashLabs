namespace WashLabs.Models.DTOs;

public static class DisponibilidadReserva
{
    public static DateTime Ahora => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Central America Standard Time");

    public static IReadOnlyList<TimeOnly> Calcular(AgendaReservaDTO? agenda, DateOnly fecha)
    {
        var ahora = Ahora;
        if (agenda is null || agenda.DiaBloqueado || agenda.DuracionMinutos <= 0 || fecha < DateOnly.FromDateTime(ahora)) return [];
        var horas = new SortedSet<TimeOnly>();
        foreach (var rango in agenda.Horarios)
        {
            var inicio = rango.Inicio.Hour * 60 + rango.Inicio.Minute;
            var fin = rango.Fin.Hour * 60 + rango.Fin.Minute;
            for (var minuto = inicio; minuto + agenda.DuracionMinutos <= fin; minuto += 15)
            {
                var hora = new TimeOnly(minuto / 60, minuto % 60);
                var final = hora.AddMinutes(agenda.DuracionMinutos);
                if (fecha.ToDateTime(hora) <= ahora || agenda.Ocupados.Any(o => hora < o.Fin && final > o.Inicio)) continue;
                horas.Add(hora);
            }
        }
        return horas.ToArray();
    }
}
