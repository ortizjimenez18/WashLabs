using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using WashLabs.Services;

internal static class PruebasImagen
{
    public static async Task Ejecutar(Action<bool,string> check)
    {
        var root = Path.Combine(Path.GetTempPath(), "washlabs-imagen-" + Guid.NewGuid().ToString("N"));
        var service = new ImagenService(new Entorno(root));
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jWZkAAAAASUVORK5CYII=");
        IFormFile File(byte[] bytes, string name, string mime) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "imagen", name) { Headers = new HeaderDictionary(), ContentType = mime };
        var ruta = await service.GuardarAsync(File(png, "../../foto.png", "image/png"), default);
        check(ruta!.StartsWith("/images/cargas/") && System.IO.File.Exists(Path.Combine(root,ruta.TrimStart('/'))),"Imagen válida y nombre seguro");
        foreach (var f in new[] { File(png,"foto.svg","image/svg+xml"), File(png,"foto.jpg","image/jpeg"), File(png,"foto.png","text/plain"), File([1,2,3],"foto.png","image/png"), File(new byte[2*1024*1024+1],"foto.png","image/png"), File([],"foto.png","image/png") })
        {
            var rechazado = false;
            try { await service.GuardarAsync(f,default); } catch (ArgumentException) { rechazado = true; }
            check(rechazado,"Rechazo de imagen inválida: " + f.FileName + " / " + f.ContentType + " / " + f.Length);
        }
        check(await service.GuardarAsync(null,default) is null,"Sin archivo no se reemplaza imagen");
        service.EliminarNueva(ruta);
        check(!System.IO.File.Exists(Path.Combine(root,ruta.TrimStart('/'))),"Retirar carga nueva cuando falla el guardado");
        Directory.Delete(Path.Combine(root,"images","cargas")); Directory.Delete(Path.Combine(root,"images")); Directory.Delete(root);
    }
    private sealed class Entorno(string root) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = root;
        public string ApplicationName { get; set; } = "Pruebas";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
