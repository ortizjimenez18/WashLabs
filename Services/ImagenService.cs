namespace WashLabs.Services;

public sealed class ImagenService(IWebHostEnvironment environment) : IImagenService
{
    public async Task<string?> GuardarAsync(IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null) return null;
        if (archivo.Length <= 0 || archivo.Length > 2 * 1024 * 1024)
            throw new ArgumentException("La imagen debe contener datos y pesar como máximo 2 MB.");
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg"))
            throw new ArgumentException("Seleccione una imagen PNG o JPEG.");
        await using var input = archivo.OpenReadStream();
        using var contenido = new MemoryStream();
        var buffer = new byte[8192];
        int leidos;
        while ((leidos = await input.ReadAsync(buffer, ct)) > 0)
        {
            if (contenido.Length + leidos > 2 * 1024 * 1024) throw new ArgumentException("La imagen supera los 2 MB.");
            await contenido.WriteAsync(buffer.AsMemory(0, leidos), ct);
        }
        var bytes = contenido.ToArray();
        var png = bytes.Length >= 33 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})
            && bytes.AsSpan(12,4).SequenceEqual("IHDR"u8) && bytes.AsSpan(bytes.Length - 8,4).SequenceEqual("IEND"u8);
        var jpeg = bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255
            && bytes[^2] == 255 && bytes[^1] == 217;
        if (!(png && extension == ".png" && archivo.ContentType == "image/png")
            && !(jpeg && extension is ".jpg" or ".jpeg" && archivo.ContentType == "image/jpeg"))
            throw new ArgumentException("El contenido de la imagen no coincide con su extensión y tipo PNG o JPEG.");
        var nombre = Guid.NewGuid().ToString("N") + (png ? ".png" : ".jpg");
        var directorio = Path.Combine(environment.WebRootPath, "images", "cargas");
        Directory.CreateDirectory(directorio);
        var destino = Path.Combine(directorio, nombre);
        try { await File.WriteAllBytesAsync(destino, bytes, ct); }
        catch { if (File.Exists(destino)) File.Delete(destino); throw; }
        return "/images/cargas/" + nombre;
    }
    public void EliminarNueva(string? ruta)
    {
        if (ruta is null || !ruta.StartsWith("/images/cargas/", StringComparison.Ordinal)) return;
        var nombre = ruta["/images/cargas/".Length..];
        if (nombre != Path.GetFileName(nombre) || nombre.Contains('\\')) return;
        var archivo = Path.Combine(environment.WebRootPath, "images", "cargas", nombre);
        if (File.Exists(archivo)) File.Delete(archivo);
    }
}
