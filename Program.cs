using WashLabs.Services.DependencyInjection;
using WashLabs.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.RateLimiting;

var configurarAcceso = args.Contains("--configurar-acceso");
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--configurar-acceso").ToArray());

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddWashLabsServices(builder.Configuration);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Cuenta/Entrar";
    options.AccessDeniedPath = "/Cuenta/SinPermiso";
    options.Cookie.Name = "WashLabs.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async context =>
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuario = int.TryParse(id, out var usuarioId) ? await context.HttpContext.RequestServices
            .GetRequiredService<ICuentaService>().SesionAsync(usuarioId, context.HttpContext.RequestAborted) : null;
        if (usuario is null || usuario.VersionAcceso != context.Principal?.FindFirstValue("VersionAcceso")
            || usuario.Rol != context.Principal?.FindFirstValue(ClaimTypes.Role)
            || (usuario.SucursalId?.ToString() ?? "") != context.Principal?.FindFirstValue("SucursalId"))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    };
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("acceso", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = async (context, ct) => {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Demasiados intentos. Espera un minuto y vuelve a intentar.", ct);
    };
});

var app = builder.Build();
if (configurarAcceso)
{
    using var scope = app.Services.CreateScope();
    Console.Write("Correo de un usuario activo existente: ");
    var correo = Console.ReadLine() ?? "";
    static string LeerClave(string texto)
    {
        Console.Write(texto);
        var clave = new System.Text.StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(true);
            if (tecla.Key == ConsoleKey.Enter) { Console.WriteLine(); return clave.ToString(); }
            if (tecla.Key == ConsoleKey.Backspace && clave.Length > 0) { clave.Length--; Console.Write("\b \b"); }
            else if (!char.IsControl(tecla.KeyChar) && clave.Length < 128) { clave.Append(tecla.KeyChar); Console.Write('*'); }
        }
    }
    var clave = LeerClave("Nueva contraseña (12–128 caracteres): ");
    var confirmacion = LeerClave("Repite la contraseña: ");
    var guardada = clave == confirmacion && await scope.ServiceProvider.GetRequiredService<ICuentaService>().ConfigurarClaveLocalAsync(correo, clave);
    Console.WriteLine(guardada ? "Acceso configurado." : "No se pudo configurar. Revisa el usuario y la contraseña.");
    Environment.ExitCode = guardada ? 0 : 1;
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/NoEncontrado", "?codigo={0}");
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
