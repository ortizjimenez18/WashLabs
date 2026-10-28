using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WashLabs.Data.Context;
using WashLabs.Data.Repositories;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Implementations;

var config = new ConfigurationBuilder().AddUserSecrets("84cd4722-f2f7-45ff-80bb-b95a43666266").AddEnvironmentVariables().Build();
var options = new DbContextOptionsBuilder<WashLabsDbContext>().UseSqlServer(config.GetConnectionString("WashLabsDB")).Options;
await using var db = new WashLabsDbContext(options);
await using var transaction = await db.Database.BeginTransactionAsync();
var service = new ServicioService(new ServicioRepository(db));
var prefix = "PAULA-TEST-" + Guid.NewGuid().ToString("N")[..10];
var passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FALLÓ: " + name); Console.WriteLine("APROBADA: " + name); passed++; }
ServicioFormulario Form() => new() { Nombre = prefix, Descripcion = "Verificación temporal", TarifaBase = 3500, DuracionMinutos = 45, TipoLavado = "EXTERIOR", NivelDetalle = "BASICO" };
try
{
    var count = await db.Servicios.CountAsync();
    var form = Form();
    Check(await service.GuardarAsync(form, true, "/images/productos/sin-imagen.svg") is null, "Crear servicio en SQL Server");
    var saved = (await service.ListarAsync()).Single(s => s.Nombre == prefix);
    var preload = await service.FormularioAsync(saved.ServicioId);
    Check(preload!.TarifaBase == 3500 && preload.DuracionMinutos == 45 && preload.TipoLavado == "EXTERIOR" && preload.NivelDetalle == "BASICO", "Precarga del servicio");
    preload.Nombre += " editado"; preload.TarifaBase = 0; preload.DuracionMinutos = 90;
    Check(await service.GuardarAsync(preload, false, null) is null, "Editar servicio con tarifa cero");
    var detail = await service.ObtenerAsync(saved.ServicioId);
    Check(detail!.TarifaBase == 0 && detail.DuracionMinutos == 90 && detail.ImagenUrl == "/images/productos/sin-imagen.svg", "Persistencia, detalle e imagen conservada");
    Check(await service.GuardarAsync(Form(), true, null) is null, "Nombre original disponible después de editar");
    Check(await service.GuardarAsync(Form(), true, null) is not null, "Nombre duplicado rechazado");
    foreach (var value in new[] { 0, -1 }) { var f = Form(); f.DuracionMinutos = value; Check(await service.GuardarAsync(f, true, null) is not null, "Duración inválida " + value); }
    foreach (var value in new[] { -1m, 1.001m }) { var f = Form(); f.TarifaBase = value; Check(await service.GuardarAsync(f, true, null) is not null, "Tarifa inválida " + value); }
    foreach (var value in new[] { "", " ", new string('N',121) }) { var f = Form(); f.Nombre = value; Check(await service.GuardarAsync(f,true,null) is not null,"Nombre vacío o excesivo"); }
    var invalid = Form(); invalid.Descripcion = " "; Check(await service.GuardarAsync(invalid,true,null) is not null,"Descripción obligatoria");
    invalid = Form(); invalid.Descripcion = new string('D',701); Check(await service.GuardarAsync(invalid,true,null) is not null,"Descripción máxima");
    invalid = Form(); invalid.NivelDetalle = "INVENTADO"; Check(await service.GuardarAsync(invalid,true,null) is not null,"Nivel válido según SQL Server");
    Check(await db.Servicios.CountAsync() == count + 2,"Listado actualizado");
    await PruebasImagen.Ejecutar(Check);
    if (!args.Contains("--solo-servicios")) await PruebasProductos.Ejecutar(db, Check, prefix);
    Console.WriteLine($"Total: {passed} pruebas aprobadas. Transacción revertida al finalizar.");
}
finally { await transaction.RollbackAsync(); }
await PruebasHttp.Ejecutar(options, Check, !args.Contains("--solo-servicios"));
Console.WriteLine($"Total completo: {passed} pruebas aprobadas.");
