namespace WashLabs.Services.Mappers;

/// <summary>Normaliza únicamente el prefijo de datos de prueba al presentar el catálogo.</summary>
public static class NombreCatalogo
{
    public static string ParaMostrar(string nombre)
    {
        const string prefijo = "A4 Paula - ";
        return nombre.StartsWith(prefijo, StringComparison.Ordinal) ? nombre[prefijo.Length..] : nombre;
    }
}
