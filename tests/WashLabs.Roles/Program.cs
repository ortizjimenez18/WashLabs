using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WashLabs.Data.Context;
using WashLabs.Data.Repositories;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Implementations;

var config = new ConfigurationBuilder().AddUserSecrets("84cd4722-f2f7-45ff-80bb-b95a43666266").AddEnvironmentVariables().Build();
var options = new DbContextOptionsBuilder<WashLabsDbContext>().UseSqlServer(config.GetConnectionString("WashLabsDB"), s => s.UseCompatibilityLevel(150)).Options;
var prefix = "WL-ROL-" + Guid.NewGuid().ToString("N")[..12];
var clave = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
var total = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FALLÓ: " + name); total++; Console.WriteLine("APROBADA: " + name); }
async Task<T> Usuarios<T>(Func<UsuarioService, Task<T>> action)
{ await using var db = new WashLabsDbContext(options); return await action(new(new UsuarioRepository(db))); }
async Task<CambiarRolFormulario> Form(int id, int rol)
{ var f = await Usuarios(s => s.FormularioRolAsync(id)) ?? throw new Exception("Formulario no disponible."); f.RolId = rol; return f; }
try
{
    int admin, clienteRol, encargadoRol, adminRol;
    await using (var db = new WashLabsDbContext(options))
    {
        var roles = await db.Roles.ToDictionaryAsync(r => r.Nombre, r => (int)r.RolId);
        clienteRol = roles["Cliente"]; encargadoRol = roles["Encargado"]; adminRol = roles["Administrador"];
        admin = await db.Usuarios.Where(u => u.Activo && u.Rol.Nombre == "Administrador").Select(u => u.UsuarioId).SingleAsync();
        Check((await new CuentaService(new CuentaRepository(db)).SesionAsync(admin))?.Rol == "Administrador", "Administrador real activo reconocido por autenticación");
    }
    var ids = new List<int>();
    for (var i = 0; i < 2; i++)
    {
        await using var db = new WashLabsDbContext(options);
        var cuentas = new CuentaService(new CuentaRepository(db));
        var registro = new RegistroCuentaDTO { Nombre = prefix + i, Correo = prefix + i + "@example.invalid", Identificacion = prefix + i,
            Telefono = "22223333", Direccion = "Verificación temporal", FechaNacimiento = new(1995, 1, 1), Clave = clave, ConfirmarClave = clave };
        Check((await cuentas.RegistrarAsync(registro)).Exito, "Registro público de Cliente " + i);
        var sesion = await cuentas.IniciarAsync(registro.Correo, clave);
        Check(sesion?.Rol == "Cliente" && sesion.SucursalId is null, "Registro crea exclusivamente Cliente sin sucursal " + i);
        ids.Add(sesion!.UsuarioId);
    }
    var id = ids[0];
    var f = await Form(id, encargadoRol);
    Check(f.Roles.Select(r => r.Nombre).Order().SequenceEqual(new[] { "Cliente", "Encargado" }), "Selector usa roles reales y excluye Administrador");
    Check(await Usuarios(s => s.CambiarRolAsync(f, 0)) is not null, "Cambio sin administrador rechazado");
    Check(await Usuarios(s => s.CambiarRolAsync(f, id)) is not null, "Cliente no puede cambiar roles desde backend");
    var elevado = await Form(id, adminRol);
    Check(await Usuarios(s => s.CambiarRolAsync(elevado, admin)) is not null, "Asignación de Administrador rechazada");
    var protegido = new CambiarRolFormulario { UsuarioId = admin, RolActualId = adminRol, RolId = clienteRol };
    Check(await Usuarios(s => s.CambiarRolAsync(protegido, admin)) is not null, "Cuenta administradora protegida");
    Check(await Usuarios(s => s.FormularioRolAsync(admin)) is null, "No se ofrece formulario para modificar administrador");
    string hash;
    await using (var db = new WashLabsDbContext(options)) hash = await db.Usuarios.Where(u => u.UsuarioId == id).Select(u => u.PasswordHash).SingleAsync();
    Check(await Usuarios(s => s.CambiarRolAsync(f, admin)) is null, "Administrador promueve Cliente a Encargado");
    await using (var db = new WashLabsDbContext(options))
    {
        var u = await db.Usuarios.AsNoTracking().Include(u => u.Rol).SingleAsync(u => u.UsuarioId == id);
        Check(u.Rol.Nombre == "Encargado" && u.SucursalId is null && u.PasswordHash == hash, "Cambio persistido sin alterar contraseña ni asignar sucursal automáticamente");
        Check(await new CuentaService(new CuentaRepository(db)).SesionAsync(id) is null, "Encargado sin sucursal aún no puede iniciar sesión");
        var service = new SucursalAdministracionService(new SucursalAdministracionRepository(db));
        Check((await service.FormularioAsync(null))!.Disponibles.Any(u => u.UsuarioId == id), "Nuevo encargado aparece disponible en Sucursales");
    }
    var segundo = await Form(ids[1], encargadoRol);
    Check(await Usuarios(s => s.CambiarRolAsync(segundo, admin)) is null, "Segundo Cliente promovido a Encargado");
    int sucursal;
    await using (var db = new WashLabsDbContext(options))
    {
        var service = new SucursalAdministracionService(new SucursalAdministracionRepository(db));
        var sede = new SucursalFormulario { Nombre = prefix, Correo = prefix + "@example.invalid", Descripcion = "Verificación temporal", Direccion = "Verificación temporal", Telefono = "2222-3333", EncargadoIds = ids };
        Check(await service.GuardarAsync(sede, true) is null, "Asignar dos nuevos encargados a una sucursal");
        sucursal = (await service.ListarAsync()).Single(s => s.Nombre == prefix).SucursalId;
    }
    await using (var db = new WashLabsDbContext(options))
    {
        Check(await db.Usuarios.CountAsync(u => ids.Contains(u.UsuarioId) && u.SucursalId == sucursal) == 2, "SQL confirma múltiples encargados y una FK por usuario");
        Check((await new CuentaService(new CuentaRepository(db)).IniciarAsync(prefix + "0@example.invalid", clave))?.Rol == "Encargado", "Encargado asignado inicia sesión con la contraseña original");
    }
    var baja = await Form(id, clienteRol);
    Check(await Usuarios(s => s.CambiarRolAsync(baja, admin)) is null, "Administrador cambia Encargado a Cliente");
    Check(await Usuarios(s => s.CambiarRolAsync(baja, admin)) is not null, "Formulario obsoleto rechazado");
    await using (var db = new WashLabsDbContext(options))
    {
        var u = await db.Usuarios.AsNoTracking().Include(u => u.Rol).SingleAsync(u => u.UsuarioId == id);
        Check(u.Rol.Nombre == "Cliente" && u.SucursalId is null && u.PasswordHash == hash, "Retorno a Cliente libera la sucursal y conserva contraseña");
        await db.Usuarios.Where(u => u.UsuarioId == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false));
    }
    Check(await Usuarios(s => s.CambiarRolAsync(new() { UsuarioId = id, RolActualId = clienteRol, RolId = encargadoRol }, admin)) is not null, "Usuario inactivo no cambia de rol");
    await using (var db = new WashLabsDbContext(options)) await db.Usuarios.Where(u => u.UsuarioId == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, true));
    var a = await Form(id, encargadoRol); var b = await Form(id, encargadoRol);
    var resultados = await Task.WhenAll(Usuarios(s => s.CambiarRolAsync(a, admin)), Usuarios(s => s.CambiarRolAsync(b, admin)));
    Check(resultados.Count(r => r is null) == 1, "Cambios concurrentes serializados: solo uno confirmado");
    Console.WriteLine($"Total: {total} pruebas de roles aprobadas.");
}
finally
{
    await using var db = new WashLabsDbContext(options);
    await db.Usuarios.Where(u => u.NombreCompleto.StartsWith(prefix)).ExecuteDeleteAsync();
    await db.Sucursales.Where(s => s.Nombre == prefix).ExecuteDeleteAsync();
}
