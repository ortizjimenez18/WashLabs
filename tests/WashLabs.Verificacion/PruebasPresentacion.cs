using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Repositories;
using WashLabs.Services.Implementations;

// Solo GET y consultas AsNoTracking: no crea reservas ni modifica datos reales.
internal static class PruebasPresentacion
{
    public static async Task EjecutarAsync(WashLabsDbContext db, Action<bool, string> verificar)
    {
        var servidor = new Uri("http://127.0.0.1:5197");
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new() };
        using var http = new HttpClient(handler) { BaseAddress = servidor };
        var producto = await db.Productos.AsNoTracking().Where(p => p.Activo)
            .OrderByDescending(p => p.Nombre.StartsWith("A4 Paula - ")).FirstAsync();
        var servicio = await db.Servicios.AsNoTracking().Where(s => s.Activo)
            .OrderByDescending(s => s.Nombre.StartsWith("A4 Paula - ")).FirstAsync();
        var rutas = new List<string> { "/", "/Productos", $"/Productos/Detalle/{producto.ProductoId}",
            "/Productos?busqueda=Cera", $"/Productos?categoriaId={producto.CategoriaProductoId}", "/Productos?busqueda=sin-coincidencias-visual-2026" };
        var carpeta = Path.GetFullPath("artifacts/precios");
        Directory.CreateDirectory(carpeta);
        async Task Revisar(string ruta)
        {
            using var respuesta = await http.GetAsync(ruta);
            verificar(respuesta.StatusCode == HttpStatusCode.OK, "HTTP GET " + ruta);
            var html = await respuesta.Content.ReadAsStringAsync();
            verificar(!WebUtility.HtmlDecode(html).Contains("A4 Paula - "), "Nombres sin prefijo en " + ruta);
            verificar(html.Contains("/css/precios.css"), "CSS de importes incluido en " + ruta);
            if (ruta.Contains("busqueda=Cera")) verificar(html.Contains("Cera") && html.Contains("product-card"), "Filtro por nombre devuelve Cera");
            if (ruta.Contains("sin-coincidencias")) verificar(html.Contains("No encontramos productos"), "Filtro sin coincidencias");

            // Copia efímera de la vista renderizada: usa los mismos estilos y verifica
            // colores calculados en Edge, sin alterar el HTML servido por la aplicación.
            html = Regex.Replace(html, "<script\\b[^>]*>[\\s\\S]*?</script>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<link[^>]*href=\"/css/([^?\"]+)[^\"]*\"[^>]*>", m =>
                "<style>" + File.ReadAllText(Path.Combine("wwwroot/css", m.Groups[1].Value)) + "</style>");
            html = html.Replace("</body>", "<script>" + File.ReadAllText("wwwroot/js/facturacion.js") + "</script>" + """
                <script>
                const picker = document.getElementById('productoId');
                if (picker && picker.options.length > 1) {
                    picker.selectedIndex = 1;
                    picker.dispatchEvent(new Event('change'));
                    if (document.getElementById('producto-precio').textContent !== picker.selectedOptions[0].dataset.price)
                        throw new Error('El selector no muestra el precio del producto');
                }
                const amounts = [...document.querySelectorAll('.washlabs-price')];
                const wrong = amounts.filter(e => getComputedStyle(e).color !==
                    (e.closest('.experience-service:nth-child(2n)') ? 'rgb(102, 217, 240)' : 'rgb(0, 124, 158)'));
                const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
                let missing = 0;
                while (walker.nextNode()) {
                    const n = walker.currentNode;
                    if (/₡\s*\d/.test(n.textContent) && !n.parentElement.closest('.washlabs-price, script, noscript')) missing++;
                }
                document.body.dataset.priceAudit = wrong.length + ':' + missing;
                </script></body>
                """);
            var archivo = Path.Combine(carpeta, "vista-temporal.html");
            await File.WriteAllTextAsync(archivo, html);
            try
            {
                var inicio = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft/Edge/Application/msedge.exe"))
                    { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var arg in new[] { "--headless=new", "--disable-gpu", "--no-first-run", "--disable-background-networking",
                    "--user-data-dir=" + Path.Combine(carpeta, "edge-profile"), "--dump-dom", new Uri(archivo).AbsoluteUri }) inicio.ArgumentList.Add(arg);
                using var navegador = Process.Start(inicio)!;
                var salida = navegador.StandardOutput.ReadToEndAsync();
                var errores = navegador.StandardError.ReadToEndAsync();
                await navegador.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(40));
                var dom = await salida;
                await errores;
                verificar(dom.Contains("data-price-audit=\"0:0\""), "Edge: importes con color correcto y cobertura completa en " + ruta);
            }
            finally { File.Delete(archivo); }
        }
        foreach (var ruta in rutas) await Revisar(ruta);

        var admin = await db.Usuarios.AsNoTracking().Include(u => u.Rol).FirstAsync(u => u.Activo && u.Rol.Nombre == "Administrador");
        var sesion = (await new CuentaService(new CuentaRepository(db)).SesionAsync(admin.UsuarioId))!;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, admin.UsuarioId.ToString()), new Claim(ClaimTypes.Name, admin.NombreCompleto),
            new Claim(ClaimTypes.Role, sesion.Rol), new Claim("VersionAcceso", sesion.VersionAcceso), new Claim("SucursalId", sesion.SucursalId?.ToString() ?? "") };
        var root = Path.GetFullPath(".");
        var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASP.NET/DataProtection-Keys")),
            b => b.SetApplicationName(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar));
        var formato = new TicketDataFormat(provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", CookieAuthenticationDefaults.AuthenticationScheme, "v2"));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10) }, CookieAuthenticationDefaults.AuthenticationScheme);
        handler.CookieContainer.Add(servidor, new Cookie("WashLabs.Session", formato.Protect(ticket), "/"));
        foreach (var ruta in new[] { "/Servicios", $"/Servicios/Detalle/{servicio.ServicioId}", "/Reservas", "/Reservas/Crear", "/Facturacion" }) await Revisar(ruta);
        var cita = await db.Citas.AsNoTracking().FirstOrDefaultAsync();
        if (cita is not null) await Revisar($"/Reservas/Detalle/{cita.CitaId}");
        var factura = await db.Facturas.AsNoTracking().FirstOrDefaultAsync();
        if (factura is not null) await Revisar($"/Facturacion/Detalle/{factura.FacturaId}");
        var proforma = await db.Facturas.AsNoTracking().FirstOrDefaultAsync(f => f.TipoDocumento == "PROFORMA" && f.EstadoFactura.Nombre == "Proforma"
            && f.Cita != null && (f.Cita.EstadoCita.Nombre == "Pendiente" || f.Cita.EstadoCita.Nombre == "Confirmada" || f.Cita.EstadoCita.Nombre == "Reprogramada"));
        if (proforma is not null) await Revisar($"/Facturacion/Detalle/{proforma.FacturaId}?mostrarProductos=true");
        Console.WriteLine("Verificación de presentación finalizada; SQL consultado sin escrituras.");
    }
}
