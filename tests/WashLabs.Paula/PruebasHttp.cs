using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Repositories;
using WashLabs.Services.Implementations;

internal static class PruebasHttp
{
    public static async Task Ejecutar(DbContextOptions<WashLabsDbContext> options, Action<bool,string> check, bool productos)
    {
        using var socket = new TcpListener(IPAddress.Loopback,0);
        socket.Start(); var port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        var root = Path.GetFullPath(".");
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(Path.Combine(root,"bin","Paula","WashLabs.dll"));
        start.ArgumentList.Add("--urls"); start.ArgumentList.Add($"http://127.0.0.1:{port}");
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        await using(var db = new WashLabsDbContext(options)) start.Environment["ConnectionStrings__WashLabsDB"] = db.Database.GetConnectionString();
        using var server = Process.Start(start) ?? throw new Exception("No se pudo iniciar WashLabs.");
        var output = server.StandardOutput.ReadToEndAsync(); var errors = server.StandardError.ReadToEndAsync();
        var address = new Uri($"http://127.0.0.1:{port}");
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
        using var client = new HttpClient(handler) { BaseAddress = address };
        var prefix = "PAULA-HTTP-" + Guid.NewGuid().ToString("N")[..10];
        var cargas = new List<string>();
        async Task<string> Get(string route)
        {
            using var r = await client.GetAsync(route);
            if (r.StatusCode != HttpStatusCode.OK) throw new Exception($"GET {route}: {r.StatusCode}");
            return await r.Content.ReadAsStringAsync();
        }
        static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        async Task Cookie(string rol)
        {
            await using var db = new WashLabsDbContext(options);
            var u = await db.Usuarios.Include(u=>u.Rol).FirstAsync(u=>u.Activo && u.Rol.Nombre==rol);
            var session = await new CuentaService(new CuentaRepository(db)).SesionAsync(u.UsuarioId) ?? throw new Exception("Sesión no disponible.");
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier,u.UsuarioId.ToString()), new Claim(ClaimTypes.Name,u.NombreCompleto), new Claim(ClaimTypes.Role,rol), new Claim("VersionAcceso",session.VersionAcceso), new Claim("SucursalId",u.SucursalId?.ToString()??"") };
            var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ASP.NET","DataProtection-Keys")),b=>b.SetApplicationName(Path.TrimEndingDirectorySeparator(root)+Path.DirectorySeparatorChar));
            var format = new TicketDataFormat(provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",CookieAuthenticationDefaults.AuthenticationScheme,"v2"));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IssuedUtc=DateTimeOffset.UtcNow,ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(10) },CookieAuthenticationDefaults.AuthenticationScheme);
            handler.CookieContainer.Add(address,new Cookie("WashLabs.Session",format.Protect(ticket),"/"));
        }
        List<KeyValuePair<string,string>> Fields(string name, int id=0) => [new("ServicioId",id.ToString()),new("Nombre",name),new("Descripcion","Servicio temporal HTTP"),new("TarifaBase","2500.50"),new("DuracionMinutos","45"),new("TipoLavado","EXTERIOR"),new("NivelDetalle","BASICO"),new("Activo","true")];
        async Task<HttpResponseMessage> Post(string route,List<KeyValuePair<string,string>> fields,string? token, bool imagen=false)
        {
            using var content = new MultipartFormDataContent();
            foreach(var field in fields) content.Add(new StringContent(field.Value),field.Key);
            if(token is not null) content.Add(new StringContent(token),"__RequestVerificationToken");
            if(imagen)
            {
                var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jWZkAAAAASUVORK5CYII=");
                var file = new ByteArrayContent(bytes); file.Headers.ContentType = new("image/png"); content.Add(file,"imagen","prueba.png");
            }
            return await client.PostAsync(route,content);
        }
        try
        {
            var ready = false;
            for(var i=0;i<60 && !ready;i++)
            {
                if(server.HasExited) throw new Exception("WashLabs no pudo iniciar.");
                try { using var r=await client.GetAsync("/Cuenta/Entrar"); ready=r.IsSuccessStatusCode; } catch(HttpRequestException) { }
                if(!ready) await Task.Delay(250);
            }
            check(ready,"HTTP: aplicación iniciada");
            using(var r=await client.GetAsync("/Servicios")) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: anónimo requiere sesión");
            await Cookie("Cliente");
            using(var r=await client.GetAsync("/Servicios")) check(r.StatusCode==HttpStatusCode.Redirect && r.Headers.Location!.ToString().Contains("SinPermiso"),"HTTP: cliente sin permiso administrativo");
            await Cookie("Administrador");
            foreach(var route in new[]{"/Servicios","/Servicios/Crear","/Productos","/Productos/Mantenimiento","/Sucursales","/Horarios"})
                check((await Get(route)).Length>0,"HTTP: renderizado " + route);
            using(var r=await Post("/Servicios/Crear",Fields(prefix),null)) check(r.StatusCode==HttpStatusCode.BadRequest,"HTTP: protección CSRF");
            var html=await Get("/Servicios/Crear");
            var invalid=Fields(prefix); invalid.RemoveAll(f=>f.Key=="DuracionMinutos");invalid.Add(new("DuracionMinutos","0"));
            using(var r=await Post("/Servicios/Crear",invalid,Token(html))) check(r.StatusCode==HttpStatusCode.OK && (await r.Content.ReadAsStringAsync()).Contains("mayor que cero"),"HTTP: duración inválida muestra mensaje");
            using(var r=await Post("/Servicios/Crear",Fields(prefix),Token(html),true)) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: crear servicio con imagen");
            int id; string? imagenUrl;
            await using(var db=new WashLabsDbContext(options))
            { var s=await db.Servicios.SingleAsync(s=>s.Nombre==prefix);id=s.ServicioId;imagenUrl=s.ImagenUrl; cargas.Add(imagenUrl!); }
            html=await Get("/Servicios");check(html.Contains(prefix) && html.Contains("Servicio creado correctamente."),"HTTP: listado y confirmación de creación");
            html=await Get($"/Servicios/Editar/{id}");check(html.Contains(prefix) && html.Contains("2500.50"),"HTTP: precarga de valores");
            using(var r=await Post($"/Servicios/Editar/{id}",Fields(prefix+" editado",id),Token(html))) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: editar sin reemplazar imagen");
            html=await Get($"/Servicios/Detalle/{id}");check(html.Contains(prefix+" editado") && html.Contains(imagenUrl!),"HTTP: detalle actualizado conserva imagen");
            using(var r=await client.GetAsync(imagenUrl)) check(r.StatusCode==HttpStatusCode.OK && r.Content.Headers.ContentType?.MediaType=="image/png","HTTP: imagen accesible desde wwwroot");
            html=await Get($"/Servicios/Editar/{id}");
            using(var r=await Post($"/Servicios/Editar/{id}",Fields(prefix+" editado",id),Token(html),true)) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: reemplazar imagen");
            await using(var db=new WashLabsDbContext(options)) { var url=await db.Servicios.Where(s=>s.ServicioId==id).Select(s=>s.ImagenUrl).SingleAsync();cargas.Add(url!);check(url!=imagenUrl,"HTTP: referencia de imagen nueva persistida"); }
            if(productos)
            {
                int categoria;
                await using(var db=new WashLabsDbContext(options)) categoria=await db.CategoriasProducto.Select(c=>c.CategoriaProductoId).FirstAsync();
                List<KeyValuePair<string,string>> ProductFields(int productId=0) => [new("ProductoId",productId.ToString()),new("Nombre",prefix+" producto"),new("Descripcion","Producto temporal HTTP"),new("CategoriaProductoId",categoria.ToString()),new("Precio","1750.25"),new("Marca","WashLabs"),new("Presentacion","250 ml"),new("SKU",prefix),new("Activo","true")];
                html=await Get("/Productos/Crear");check(html.Contains($"value=\"{categoria}\""),"HTTP: selector de categorías desde SQL");
                using(var r=await Post("/Productos/Crear",ProductFields(),null)) check(r.StatusCode==HttpStatusCode.BadRequest,"HTTP: productos requieren CSRF");
                invalid=ProductFields();invalid.RemoveAll(f=>f.Key=="Precio");invalid.Add(new("Precio","-1"));
                using(var r=await Post("/Productos/Crear",invalid,Token(html))) check(r.StatusCode==HttpStatusCode.OK && (await r.Content.ReadAsStringAsync()).Contains("mayor o igual a cero"),"HTTP: precio negativo muestra mensaje");
                invalid=ProductFields();invalid.RemoveAll(f=>f.Key=="CategoriaProductoId");invalid.Add(new("CategoriaProductoId",int.MaxValue.ToString()));
                using(var r=await Post("/Productos/Crear",invalid,Token(html))) check(r.StatusCode==HttpStatusCode.OK && (await r.Content.ReadAsStringAsync()).Contains("no existe"),"HTTP: categoría manipulada rechazada");
                using(var r=await Post("/Productos/Crear",ProductFields(),Token(html),true)) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: crear producto con imagen");
                int productId;
                await using(var db=new WashLabsDbContext(options)) { var p=await db.Productos.SingleAsync(p=>p.SKU==prefix);productId=p.ProductoId;imagenUrl=p.ImagenUrl;cargas.Add(imagenUrl!); }
                html=await Get("/Productos/Mantenimiento");check(html.Contains(prefix) && html.Contains("Producto creado correctamente."),"HTTP: listado administrativo y confirmación");
                check((await Get("/Productos")).Contains(prefix),"HTTP: producto aparece en catálogo público");
                html=await Get($"/Productos/Editar/{productId}");check(html.Contains(prefix) && html.Contains("1750.25") && html.Contains("250 ml"),"HTTP: precarga completa de producto");
                using(var r=await Post($"/Productos/Editar/{productId}",ProductFields(productId),Token(html))) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: editar producto sin cambiar imagen");
                html=await Get($"/Productos/DetalleMantenimiento/{productId}");check(html.Contains(imagenUrl!) && html.Contains("Editar producto"),"HTTP: detalle administrativo conserva imagen");
                html=await Get($"/Productos/Editar/{productId}");
                using(var r=await Post($"/Productos/Editar/{productId}",ProductFields(productId),Token(html),true)) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: reemplazar imagen de producto");
                await using(var db=new WashLabsDbContext(options)) { var url=await db.Productos.Where(p=>p.ProductoId==productId).Select(p=>p.ImagenUrl).SingleAsync();cargas.Add(url!);check(url!=imagenUrl,"HTTP: imagen de producto reemplazada persistida"); }
                html=await Get("/Productos/Crear");
                using(var r=await Post("/Productos/Crear",ProductFields(),Token(html))) check(r.StatusCode==HttpStatusCode.OK && (await r.Content.ReadAsStringAsync()).Contains("ese SKU"),"HTTP: SKU duplicado muestra mensaje");
                await Cookie("Cliente");
                foreach(var route in new[]{"/Productos/Crear","/Productos/Editar/"+productId,"/Productos/Mantenimiento"})
                { using var r=await client.GetAsync(route);check(r.StatusCode==HttpStatusCode.Redirect && r.Headers.Location!.ToString().Contains("SinPermiso"),"HTTP: cliente sin permiso " + route); }
            }
        }
        finally
        {
            if(!server.HasExited) server.Kill(true);
            await server.WaitForExitAsync(); await output; await errors;
            await using var db=new WashLabsDbContext(options);
            cargas.AddRange((await db.Servicios.Where(s=>s.Nombre.StartsWith(prefix)).Select(s=>s.ImagenUrl).ToListAsync()).OfType<string>());
            cargas.AddRange((await db.Productos.Where(p=>p.Nombre.StartsWith(prefix)).Select(p=>p.ImagenUrl).ToListAsync()).OfType<string>());
            await db.Servicios.Where(s=>s.Nombre.StartsWith(prefix)).ExecuteDeleteAsync();
            await db.Productos.Where(p=>p.Nombre.StartsWith(prefix)).ExecuteDeleteAsync();
            foreach(var ruta in cargas) { var archivo=Path.Combine(root,"wwwroot",ruta.TrimStart('/'));if(File.Exists(archivo))File.Delete(archivo); }
        }
    }
}
