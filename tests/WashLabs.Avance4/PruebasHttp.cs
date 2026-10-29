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
using WashLabs.Data.Entities;
using WashLabs.Data.Repositories;
using WashLabs.Services.Implementations;

internal static class PruebasHttp
{
    public static async Task EjecutarAsync(DbContextOptions<WashLabsDbContext> options,string prefix,Action<bool,string> check)
    {
        using var socket=new TcpListener(IPAddress.Loopback,0);
        socket.Start(); var port=((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        var root=Path.GetFullPath(".");
        var start=new ProcessStartInfo("dotnet") { WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true };
        start.ArgumentList.Add(Path.GetFullPath(Environment.GetEnvironmentVariable("WASHLABS_AVANCE4_APP_PATH")??"bin/Release/net8.0/WashLabs.dll"));
        start.ArgumentList.Add("--urls"); start.ArgumentList.Add($"http://127.0.0.1:{port}");
        start.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
        await using(var db=new WashLabsDbContext(options)) start.Environment["ConnectionStrings__WashLabsDB"]=db.Database.GetConnectionString();
        using var server=Process.Start(start)??throw new Exception("No se pudo iniciar el servidor de pruebas.");
        var output=server.StandardOutput.ReadToEndAsync(); var errors=server.StandardError.ReadToEndAsync();
        var address=new Uri($"http://127.0.0.1:{port}");
        using var handler=new HttpClientHandler { AllowAutoRedirect=false,CookieContainer=new CookieContainer() };
        using var client=new HttpClient(handler) { BaseAddress=address,Timeout=TimeSpan.FromSeconds(30) };
        try
        {
            var ready=false;
            for(var i=0;i<50 && !ready;i++)
            {
                if(server.HasExited) throw new Exception("El servidor terminó antes de iniciar.");
                try { using var r=await client.GetAsync("/Cuenta/Entrar"); ready=r.IsSuccessStatusCode; }
                catch(HttpRequestException) { }
                if(!ready) await Task.Delay(250);
            }
            check(ready,"HTTP: servidor iniciado sin cambiar credenciales");
            foreach(var route in new[]{"/Sucursales","/Horarios","/Sucursales/Crear","/Horarios/CrearBloqueo"})
            {
                using var r=await client.GetAsync(route);
                check(r.StatusCode==HttpStatusCode.Redirect && r.Headers.Location!.ToString().Contains("/Cuenta/Entrar"),"HTTP: anónimo requiere sesión "+route);
            }
            int admin,cliente;
            await using(var db=new WashLabsDbContext(options))
            {
                admin=await db.Usuarios.Where(u=>u.Activo && u.Rol.Nombre=="Administrador").Select(u=>u.UsuarioId).SingleAsync();
                cliente=await db.Usuarios.Where(u=>u.Activo && u.Rol.Nombre=="Cliente").Select(u=>u.UsuarioId).FirstAsync();
            }
            // Se firma una sesión efímera con las claves locales de desarrollo. No se
            // cambia ningún hash, rol o cuenta ni se registra la cookie en disco/salida.
            async Task CookieAsync(int id)
            {
                await using var db=new WashLabsDbContext(options);
                var u=await db.Usuarios.Include(u=>u.Rol).SingleAsync(u=>u.UsuarioId==id);
                var session=await new CuentaService(new CuentaRepository(db)).SesionAsync(id)??throw new Exception("Cuenta no disponible para pruebas.");
                var claims=new[]{new Claim(ClaimTypes.NameIdentifier,id.ToString()),new Claim(ClaimTypes.Name,u.NombreCompleto),new Claim(ClaimTypes.Role,u.Rol.Nombre),new Claim("VersionAcceso",session.VersionAcceso),new Claim("SucursalId",u.SucursalId?.ToString()??"")};
                var principal=new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme));
                var provider=DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ASP.NET","DataProtection-Keys")),b=>b.SetApplicationName(Path.TrimEndingDirectorySeparator(root)+Path.DirectorySeparatorChar));
                var format=new TicketDataFormat(provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",CookieAuthenticationDefaults.AuthenticationScheme,"v2"));
                var ticket=new AuthenticationTicket(principal,new AuthenticationProperties { IssuedUtc=DateTimeOffset.UtcNow,ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(10) },CookieAuthenticationDefaults.AuthenticationScheme);
                handler.CookieContainer.Add(address,new Cookie("WashLabs.Session",format.Protect(ticket),"/"));
            }
            await CookieAsync(cliente);
            foreach(var route in new[]{"/Sucursales","/Horarios"})
            {
                using var r=await client.GetAsync(route);
                check(r.StatusCode==HttpStatusCode.Redirect && r.Headers.Location!.ToString().Contains("SinPermiso"),"HTTP: cliente sin permisos de administración "+route);
            }
            await CookieAsync(admin);
            foreach(var route in new[]{"/Sucursales","/Horarios","/Productos","/Reservas","/Facturacion","/Usuarios"})
            { using var r=await client.GetAsync(route); check(r.StatusCode==HttpStatusCode.OK,"HTTP: administrador y regresión Avance 3 "+route); }
            static string Token(string html)
            {
                var m=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
                return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value) : throw new Exception("Falta token CSRF en formulario.");
            }
            async Task<string> Get(string route)
            { using var r=await client.GetAsync(route); if(r.StatusCode!=HttpStatusCode.OK) throw new Exception($"GET {route}: {r.StatusCode}"); return await r.Content.ReadAsStringAsync(); }
            async Task<HttpResponseMessage> Post(string route,List<KeyValuePair<string,string>> fields,string? token)
            { if(token is not null) fields.Add(new("__RequestVerificationToken",token)); return await client.PostAsync(route,new FormUrlEncodedContent(fields)); }
            List<KeyValuePair<string,string>> Fields(string name,string mail)=>[new("SucursalId","0"),new("Nombre",name),new("Descripcion","Sucursal temporal verificada por HTTP"),new("Telefono","2222-3333"),new("Direccion","Dirección de pruebas HTTP"),new("Correo",mail)];
            using(var r=await Post("/Sucursales/Crear",Fields(prefix+"CSRF",prefix+"csrf@example.invalid"),null)) check(r.StatusCode==HttpStatusCode.BadRequest,"HTTP: sucursal rechaza POST sin CSRF");
            var html=await Get("/Sucursales/Crear");
            var nombre=prefix+"HTTP"; var correo=prefix+"http@example.invalid";
            var users=new List<int>();
            await using(var db=new WashLabsDbContext(options))
            {
                var rol=await db.Roles.SingleAsync(r=>r.Nombre=="Encargado");
                for(var i=0;i<2;i++)
                { var u=new Usuario { NombreCompleto=prefix+"HTTP"+i,Correo=prefix+"http"+i+"@example.invalid",Telefono="22223333",Direccion="Temporal",RolId=rol.RolId,Activo=true,PasswordHash="!SIN-ACCESO-"+Guid.NewGuid() };db.Usuarios.Add(u);await db.SaveChangesAsync();users.Add(u.UsuarioId); }
            }
            html=await Get("/Sucursales/Crear");
            var fields=Fields(nombre,correo); foreach(var id in users) fields.Add(new("EncargadoIds",id.ToString()));
            using(var r=await Post("/Sucursales/Crear",fields,Token(html))) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: crear sucursal y dos encargados");
            html=await Get("/Sucursales");
            check(html.Contains(nombre) && html.Contains("Sucursal creada correctamente."),"HTTP: confirmación visible y listado después de crear");
            int sede;
            await using(var db=new WashLabsDbContext(options)) sede=await db.Sucursales.Where(s=>s.Correo==correo).Select(s=>s.SucursalId).SingleAsync();
            html=await Get($"/Sucursales/Editar/{sede}");
            check(html.Contains(nombre) && Regex.Matches(html,"checked=\"checked\"").Count==2,"HTTP: editar precarga campos y dos casillas seleccionadas");
            fields=Fields(nombre+" editada",correo);fields[0]=new("SucursalId",sede.ToString());fields.Add(new("EncargadoIds",users[1].ToString()));
            using(var r=await Post($"/Sucursales/Editar/{sede}",fields,Token(html))) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: editar sucursal y retirar encargado");
            html=await Get($"/Sucursales/Detalle/{sede}");
            check(html.Contains(nombre+" editada") && html.Contains(prefix+"HTTP1") && !html.Contains(prefix+"HTTP0"),"HTTP: detalle confirma campos y asignación editados");
            var today=AgendaAdministracionService.Hoy.ToString("yyyy-MM-dd");
            List<KeyValuePair<string,string>> Agenda(string from,string to)=>[new("SucursalId",sede.ToString()),new("Fecha",today),new("HoraInicio",from),new("HoraFin",to),new("Motivo","Prueba HTTP")];
            html=await Get($"/Horarios/CrearHorario?sucursalId={sede}");
            using(var r=await Post("/Horarios/CrearHorario",Agenda("08:00","10:00"),Token(html)))
            {
                check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: horario con fecha actual permitido");
                html=await Get(r.Headers.Location!.ToString());
                check(html.Contains("Horario guardado correctamente.") && html.Contains(nombre+" editada"),"HTTP: detalle y confirmación visible de horario");
            }
            html=await Get($"/Horarios/CrearBloqueo?sucursalId={sede}");
            using(var r=await Post("/Horarios/CrearBloqueo",Agenda("10:00","11:00"),Token(html)))
            {
                check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: bloqueo contiguo permitido");
                html=await Get(r.Headers.Location!.ToString());
                check(html.Contains("Bloqueo guardado correctamente.") && html.Contains("Prueba HTTP"),"HTTP: detalle y confirmación visible de bloqueo");
            }
            html=await Get($"/Horarios/CrearBloqueo?sucursalId={sede}");
            using(var r=await Post("/Horarios/CrearBloqueo",Agenda("09:00","11:00"),Token(html)))
            { var body=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()); check(r.StatusCode==HttpStatusCode.OK && body.Contains("bloqueo se sobrepone con un horario"),"HTTP: conflicto exacto visible al usuario"); }
            using(var r=await Post("/Horarios/CrearHorario",Agenda("12:00","13:00"),null)) check(r.StatusCode==HttpStatusCode.BadRequest,"HTTP: horario rechaza POST sin CSRF");
            using(var r=await Post("/Horarios/CrearBloqueo",Agenda("12:00","13:00"),null)) check(r.StatusCode==HttpStatusCode.BadRequest,"HTTP: bloqueo rechaza POST sin CSRF");
            html=await Get($"/Horarios/CrearHorario?sucursalId={sede}");
            var malformed=Agenda("12:00","13:00");malformed[1]=new("Fecha","fecha-inválida");
            using(var r=await Post("/Horarios/CrearHorario",malformed,Token(html)))
            { var body=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());check(r.StatusCode==HttpStatusCode.OK && body.Contains("El valor ingresado en Fecha no es válido."),"HTTP: formato de fecha inválido muestra mensaje en español"); }
            html=await Get($"/Horarios?sucursalId={sede}");
            check(html.Contains("08:00") && html.Contains("10:00") && html.Contains("Prueba HTTP"),"HTTP: selector y ambos listados reflejan guardado");
            foreach(var route in new[]{"/Sucursales/Detalle/2147483647","/Horarios/DetalleHorario/2147483647","/Horarios/DetalleBloqueo/9223372036854775807","/Horarios?sucursalId=2147483647"})
            { using var r=await client.GetAsync(route); check(r.StatusCode==HttpStatusCode.NotFound,"HTTP: recurso inexistente devuelve 404 "+route); }
            int rolCliente,rolEncargado,rolAdmin;
            await using(var db=new WashLabsDbContext(options))
            {
                var roles=await db.Roles.ToDictionaryAsync(r=>r.Nombre,r=>(int)r.RolId);
                rolCliente=roles["Cliente"];rolEncargado=roles["Encargado"];rolAdmin=roles["Administrador"];
            }
            var clave=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
            var correoRol=prefix+"rol@example.invalid";
            html=await Get("/Cuenta/Registrarse");
            List<KeyValuePair<string,string>> registro=[new("Nombre",prefix+"ROLHTTP"),new("Identificacion",prefix+"ROL"),new("Telefono","22223333"),new("Direccion","Verificación temporal"),new("Correo",correoRol),new("FechaNacimiento","1995-01-01"),new("Clave",clave),new("ConfirmarClave",clave),new("RolId",rolAdmin.ToString()),new("SucursalId",sede.ToString())];
            using(var r=await Post("/Cuenta/Registrarse",registro,Token(html))) check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: registro público con intento de sobreasignar rol");
            int nuevoCliente;
            await using(var db=new WashLabsDbContext(options))
            {
                var u=await db.Usuarios.AsNoTracking().SingleAsync(u=>u.Correo==correoRol);
                nuevoCliente=u.UsuarioId;
                check(u.RolId==rolCliente && u.SucursalId is null,"HTTP: registro ignora rol Administrador y sucursal enviados");
            }
            var cookieCliente=handler.CookieContainer.GetCookies(address)["WashLabs.Session"]!.Value;
            using(var r=await client.GetAsync($"/Usuarios/CambiarRol/{nuevoCliente}"))
                check(r.StatusCode==HttpStatusCode.Redirect && r.Headers.Location!.ToString().Contains("SinPermiso"),"HTTP: Cliente no accede al cambio de rol");
            html=await Get("/Cuenta/Registrarse");
            List<KeyValuePair<string,string>> Cambio(int destino)=>[new("UsuarioId",nuevoCliente.ToString()),new("RolActualId",rolCliente.ToString()),new("RolId",destino.ToString()),new("SucursalActualId","")];
            using(var r=await Post($"/Usuarios/CambiarRol/{nuevoCliente}",Cambio(rolEncargado),Token(html)))
                check(r.StatusCode==HttpStatusCode.Redirect && r.Headers.Location!.ToString().Contains("SinPermiso"),"HTTP: Cliente no puede enviar cambios de rol");
            await CookieAsync(admin);
            html=await Get($"/Usuarios/CambiarRol/{nuevoCliente}");
            check(!Regex.IsMatch(html,$"<option[^>]*value=\"{rolAdmin}\""),"HTTP: selector de roles no ofrece Administrador");
            using(var r=await Post($"/Usuarios/CambiarRol/{nuevoCliente}",Cambio(rolEncargado),null))
                check(r.StatusCode==HttpStatusCode.BadRequest,"HTTP: cambio de rol exige CSRF");
            using(var r=await Post($"/Usuarios/CambiarRol/{nuevoCliente}",Cambio(rolAdmin),Token(html)))
                check(r.StatusCode==HttpStatusCode.OK && WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()).Contains("Solo se permite seleccionar Cliente o Encargado"),"HTTP: promoción a Administrador rechazada en backend");
            using(var r=await Post($"/Usuarios/CambiarRol/{nuevoCliente}",Cambio(rolEncargado),Token(html)))
                check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: administrador cambia Cliente a Encargado");
            html=await Get($"/Usuarios/Detalle/{nuevoCliente}");
            check(html.Contains("Rol actualizado correctamente.") && html.Contains("Encargado"),"HTTP: detalle confirma nuevo rol persistido");
            handler.CookieContainer.Add(address,new Cookie("WashLabs.Session",cookieCliente,"/"));
            using(var r=await client.GetAsync("/Reservas"))
                check(r.StatusCode==HttpStatusCode.Redirect && r.Headers.Location!.ToString().Contains("/Cuenta/Entrar"),"HTTP: sesión anterior revocada al cambiar el rol");
            await CookieAsync(admin);
            html=await Get($"/Sucursales/Editar/{sede}");
            check(Regex.IsMatch(html,$"name=\"EncargadoIds\"[^>]*value=\"{nuevoCliente}\""),"HTTP: nuevo encargado aparece disponible para asignación");
            fields=Fields(nombre+" editada",correo);fields[0]=new("SucursalId",sede.ToString());fields.Add(new("EncargadoIds",users[1].ToString()));fields.Add(new("EncargadoIds",nuevoCliente.ToString()));
            using(var r=await Post($"/Sucursales/Editar/{sede}",fields,Token(html)))
                check(r.StatusCode==HttpStatusCode.Redirect,"HTTP: asignar nuevo encargado junto a otro existente");
            await using(var db=new WashLabsDbContext(options))
            {
                check(await db.Usuarios.CountAsync(u=>u.SucursalId==sede)==2,"HTTP: dos encargados persistidos en la sucursal");
                check((await new CuentaService(new CuentaRepository(db)).IniciarAsync(correoRol,clave))?.Rol=="Encargado","HTTP: encargado asignado conserva su acceso original");
            }
        }
        finally
        {
            if(!server.HasExited) server.Kill(true);
            await server.WaitForExitAsync();
            await output; await errors;
        }
    }
}
