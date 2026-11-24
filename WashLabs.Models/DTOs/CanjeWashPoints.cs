namespace WashLabs.Models.DTOs;

public static class CanjeWashPoints
{
    public const string Servicio = "Wash Express";
    public const string Concepto = "Canje WashPoints";

    public static bool EsWashExpress(string nombre) =>
        string.Equals(nombre.Trim(), Servicio, StringComparison.OrdinalIgnoreCase);
}
