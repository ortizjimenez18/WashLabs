using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Data.Repositories;
using WashLabs.Services.Implementations;
using WashLabs.Services.Mappers;

var pruebas = 0;
void Verificar(bool condicion, string nombre)
{
    if (!condicion) throw new InvalidOperationException("FALLÓ: " + nombre);
    pruebas++;
    Console.WriteLine("APROBADA: " + nombre);
}

// Las pruebas de contraseñas utilizan usuarios en memoria, nunca cuentas de SQL.
var usuario = new Usuario { UsuarioId = 71, Activo = true, NombreCompleto = "Prueba",
    Correo = "prueba@example.invalid", Rol = new Rol { Nombre = "Cliente" } };
var clave = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, clave);
var cuentas = new CuentaService(new CuentaEnMemoria(usuario));
Verificar((await cuentas.IniciarAsync(usuario.Correo, clave))?.UsuarioId == usuario.UsuarioId, "Identity conserva la identidad real del cliente");
Verificar(await cuentas.IniciarAsync(usuario.Correo, clave + "x") is null, "Contraseña incorrecta rechazada");
usuario.PasswordHash = "hash-incompatible";
Verificar(await cuentas.IniciarAsync(usuario.Correo, clave) is null, "Hash incompatible no omite la seguridad");
usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, clave);
usuario.Activo = false;
Verificar(await cuentas.IniciarAsync(usuario.Correo, clave) is null, "Cuenta inactiva rechazada");
usuario.Activo = true;
usuario.Rol.Nombre = "Encargado";
Verificar(await cuentas.IniciarAsync(usuario.Correo, clave) is null, "Encargado sin sucursal rechazado");
usuario.SucursalId = 23;
usuario.Sucursal = new Sucursal { Activa = true };
Verificar((await cuentas.IniciarAsync(usuario.Correo, clave)) is { Rol: "Encargado", SucursalId: 23 }, "Rol y sucursal del encargado conservados");
usuario.Rol.Nombre = "Administrador";
Verificar((await cuentas.IniciarAsync(usuario.Correo, clave))?.Rol == "Administrador", "Administrador reconocido");
Verificar(ReservaMapper.ToDTO(new EstadoCita { Nombre = "Prueba", ColorHex = "javascript:" }).ColorHex == "#73787D", "Color de estado inválido rechazado");

if (args.Contains("--solo-unitarias"))
{
    Console.WriteLine($"Total: {pruebas} pruebas. SQL no consultado.");
    return;
}

var configuracion = new ConfigurationBuilder().AddUserSecrets("84cd4722-f2f7-45ff-80bb-b95a43666266")
    .AddEnvironmentVariables().Build();
if (args.Contains("--script-pruebas"))
{
    var opcionesScript = new DbContextOptionsBuilder<WashLabsDbContext>().UseSqlServer("Server=localhost;Database=WashLabs_Pruebas_Avance3;Integrated Security=True").Options;
    await using var modelo = new WashLabsDbContext(opcionesScript);
    Directory.CreateDirectory("artifacts");
    await File.WriteAllTextAsync("artifacts/EstructuraPruebasAvance3.sql",
        "CREATE DATABASE [WashLabs_Pruebas_Avance3];\nGO\nUSE [WashLabs_Pruebas_Avance3];\nGO\n" + modelo.Database.GenerateCreateScript());
    Console.WriteLine("Script generado en artifacts/EstructuraPruebasAvance3.sql. No se ejecutó SQL.");
    return;
}
if (args.Contains("--preparar-pruebas"))
{
    await PruebasWashPoints.PrepararAsync();
    return;
}
if (args.Contains("--washpoints"))
{
    await PruebasWashPoints.EjecutarAsync();
    return;
}
if (args.Contains("--integracion"))
{
    await PruebasIntegracionFacturacion.EjecutarAsync();
    return;
}
if (args.Contains("--facturador"))
{
    await PruebasFacturador.EjecutarAsync();
    return;
}
if (args.Contains("--http-autenticado"))
{
    await PruebasHttpAutenticadas.EjecutarAsync();
    return;
}
var cadena = configuracion.GetConnectionString("WashLabsDB") ?? throw new InvalidOperationException("Configura ConnectionStrings:WashLabsDB.");
var opciones = new DbContextOptionsBuilder<WashLabsDbContext>().UseSqlServer(cadena, s => s.UseCompatibilityLevel(150)).Options;
await using var db = new WashLabsDbContext(opciones);
if (args.Contains("--usuarios"))
{
    var servicio = new UsuarioService(new UsuarioRepository(db));
    var lista = await servicio.ListarAsync();
    var reales = await db.Usuarios.AsNoTracking().Include(u => u.Rol).OrderBy(u => u.UsuarioId).ToListAsync();
    Verificar(lista.Count >= 3 && lista.Count == reales.Count, "Listado devuelve todos los usuarios de SQL Server (mínimo tres)");
    foreach (var real in reales)
    {
        var dto = lista.Single(u => u.UsuarioId == real.UsuarioId);
        Verificar(dto.NombreCompleto == real.NombreCompleto && dto.Rol == real.Rol.Nombre
            && dto.Correo == real.Correo && dto.Activo == real.Activo, "Nombre, rol, correo y estado coinciden con SQL: " + real.UsuarioId);
        Verificar(await servicio.ObtenerAsync(real.UsuarioId) == dto, "Detalle coincide con listado: " + real.UsuarioId);
    }
    Verificar(await servicio.ObtenerAsync(int.MaxValue) is null && await servicio.ObtenerAsync(0) is null
        && await servicio.ObtenerAsync(-1) is null, "Identificadores inexistentes e inválidos no devuelven usuarios");
    Console.WriteLine($"Total: {pruebas} comprobaciones; {lista.Count} usuarios reales; solo lectura.");
    return;
}
var actores = await db.Usuarios.AsNoTracking().Include(u => u.Rol).Include(u => u.Sucursal).Where(u => u.Activo).ToListAsync();
var facturas = new FacturaRepository(db);
var reservas = new ReservaRepository(db);
var productos = new ProductoRepository(db);
var operaciones = new OperacionRepository(db);
Verificar(db.Model.GetEntityTypes().Count() == 34, "Contexto con 34 entidades");
var catalogo = await productos.ListarActivosAsync(null, null, default);
Verificar(catalogo.Count > 0 && catalogo.All(p => p.Activo && p.CategoriaProducto is not null), "Catálogo activo con categorías desde LINQ");
foreach (var p in catalogo)
    Verificar((await productos.ObtenerActivoAsync(p.ProductoId, default))?.ProductoId == p.ProductoId, "Detalle público de producto");
Verificar(await productos.ObtenerActivoAsync(int.MaxValue, default) is null, "Producto inexistente");
var categorias = await operaciones.CategoriasAsistenteAsync(default);
Verificar(categorias.Count == 5, "Cinco categorías del asistente");
foreach (var categoria in categorias)
    foreach (var pregunta in categoria.PreguntasAsistente)
        Verificar((await operaciones.PreguntaAsync(pregunta.PreguntaAsistenteId, default))?.OpcionesAsistentePreguntaAsistente.Count > 0, "Pregunta inicial con opciones");
Verificar(await operaciones.PreguntaAsync(int.MaxValue, default) is null, "Pregunta inexistente");

var documentos = await db.Facturas.AsNoTracking().Where(f => f.TipoDocumento == "FACTURA").Include(f => f.DetallesFactura).ToListAsync();
foreach (var f in documentos)
{
    Verificar(f.Subtotal == f.DetallesFactura.Sum(d => d.Subtotal ?? 0m)
        && f.Impuesto == f.DetallesFactura.Sum(d => d.Impuesto ?? 0m) && f.Total == f.Subtotal + f.Impuesto, "Totales persistidos de factura coherentes");
    Verificar(f.DetallesFactura.All(d => d.PorcentajeImpuesto == 13m
        && d.Subtotal == decimal.Round(d.Cantidad * d.PrecioUnitario - d.Descuento, 2, MidpointRounding.AwayFromZero)
        && d.Impuesto == decimal.Round((d.Subtotal ?? 0m) * 0.13m, 2, MidpointRounding.AwayFromZero)), "Subtotal por línea e IVA del 13%");
}
foreach (var n in new[] { 1, 2, 3 })
    Verificar(documentos.Any(f => f.DetallesFactura.Count == n), $"Factura existente con {n} líneas");
Verificar(documentos.Any(f => f.DetallesFactura.Any(d => d.TipoLinea == "P") && f.DetallesFactura.Any(d => d.TipoLinea == "S")), "Factura mixta existente");

var todasCitas = await db.Citas.AsNoTracking().ToListAsync();
foreach (var actor in actores)
{
    var visibles = await facturas.ListarAsync(default, actor.UsuarioId);
    Verificar(visibles.All(f => actor.Rol.Nombre == "Administrador"
        || actor.Rol.Nombre == "Cliente" && f.ClienteId == actor.UsuarioId
        || actor.Rol.Nombre == "Encargado" && f.SucursalId == actor.SucursalId), "Facturas filtradas en SQL por rol");
    if (actor.Rol.Nombre == "Administrador") Verificar(visibles.Count >= 4, "Administrador consulta cuatro facturas sin reactivar usuarios");
    foreach (var f in documentos)
    {
        var permitido = actor.Rol.Nombre == "Administrador" || actor.Rol.Nombre == "Cliente" && f.ClienteId == actor.UsuarioId
            || actor.Rol.Nombre == "Encargado" && f.SucursalId == actor.SucursalId;
        Verificar((await facturas.ObtenerAsync(f.FacturaId, default, actor.UsuarioId) is not null) == permitido, "Detalle de factura respeta propiedad y sucursal");
    }
    var citas = actor.Rol.Nombre == "Cliente" ? await reservas.ListarClienteAsync(actor.UsuarioId, default)
        : await reservas.ListarAsync(actor.UsuarioId, default);
    Verificar(citas.All(c => actor.Rol.Nombre == "Administrador" || actor.Rol.Nombre == "Cliente" && c.ClienteId == actor.UsuarioId
        || actor.Rol.Nombre == "Encargado" && c.SucursalId == actor.SucursalId), "Reservas filtradas en SQL por rol");
    if (actor.Rol.Nombre == "Encargado") Verificar(citas.Count >= 3, "Encargado consulta al menos tres citas");
    if (actor.Rol.Nombre == "Administrador") Verificar(citas.Count == todasCitas.Count, "Administrador consulta todas las citas");
    foreach (var cita in todasCitas)
    {
        var detalle = actor.Rol.Nombre == "Cliente" ? await reservas.ObtenerClienteAsync(cita.CitaId, actor.UsuarioId, default)
            : await reservas.ObtenerAsync(cita.CitaId, actor.UsuarioId, default);
        var permitido = actor.Rol.Nombre == "Administrador" || actor.Rol.Nombre == "Cliente" && cita.ClienteId == actor.UsuarioId
            || actor.Rol.Nombre == "Encargado" && cita.SucursalId == actor.SucursalId;
        Verificar((detalle is not null) == permitido, "Reserva individual respeta propiedad y sucursal");
        if (detalle is not null) Verificar(detalle.RespuestasReserva.Count == 3, "Tres preguntas y respuestas desde SQL");
    }
    var formulario = await reservas.FormularioAsync(actor.UsuarioId, default);
    Verificar(formulario is not null && (actor.Rol.Nombre != "Encargado" || formulario.Sucursales.All(s => s.SucursalId == actor.SucursalId)), "Sucursales del formulario según permisos");
}
Verificar((await facturas.ListarAsync(default, int.MaxValue)).Count == 0, "Usuario inexistente sin facturas");
Verificar(await facturas.ObtenerAsync(long.MaxValue, default, actores[0].UsuarioId) is null, "Factura inexistente");
Verificar(await reservas.ObtenerAsync(long.MaxValue, actores.First(u => u.Rol.Nombre == "Administrador").UsuarioId, default) is null, "Reserva inexistente");
var puntos = await new OperacionService(operaciones).WashPointsAsync();
Verificar(puntos.Count == await db.Vehiculos.CountAsync(v => v.Activo && v.Cliente.Activo), "Consulta WashPoints por vehículo activo");
Console.WriteLine($"Total: {pruebas} pruebas. Consultas SQL de solo lectura; no se completaron citas ni se cambiaron cuentas.");

sealed class CuentaEnMemoria(Usuario usuario) : ICuentaRepository
{
    public Task<Usuario?> PorCorreoAsync(string correo, CancellationToken ct) => Task.FromResult<Usuario?>(correo == usuario.Correo ? usuario : null);
    public Task<Usuario?> PorIdAsync(int id, CancellationToken ct) => Task.FromResult<Usuario?>(id == usuario.UsuarioId ? usuario : null);
    public Task<string?> RegistrarAsync(Usuario nuevo, CancellationToken ct) => throw new NotSupportedException();
    public Task GuardarClaveAsync(int id, string hash, CancellationToken ct) { usuario.PasswordHash = hash; return Task.CompletedTask; }
}
