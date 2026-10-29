using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Repositories;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Implementations;

var config = new ConfigurationBuilder().AddUserSecrets("84cd4722-f2f7-45ff-80bb-b95a43666266").AddEnvironmentVariables().Build();
var options = new DbContextOptionsBuilder<WashLabsDbContext>().UseSqlServer(config.GetConnectionString("WashLabsDB"), sql => sql.UseCompatibilityLevel(150)).Options;
var prefix = "A4-TEST-" + Guid.NewGuid().ToString("N")[..12];
var passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FALLÓ: " + name); Console.WriteLine("APROBADA: " + name); passed++; }
async Task<T> WithService<T>(Func<SucursalAdministracionService,Task<T>> action)
{ await using var db = new WashLabsDbContext(options); return await action(new(new SucursalAdministracionRepository(db))); }
SucursalFormulario Form(string suffix) => new() { Nombre = prefix + suffix, Correo = prefix + suffix + "@example.invalid", Descripcion = "Registro temporal de verificación", Direccion = "Dirección temporal de prueba", Telefono = "2222-3333" };
var users = new List<int>();
try
{
    await using (var db = new WashLabsDbContext(options))
    {
        var role = await db.Roles.SingleAsync(r => r.Nombre == "Encargado");
        for (var i=0;i<3;i++)
        {
            var u = new Usuario { RolId = role.RolId, NombreCompleto = prefix+i, Correo = prefix+i+"@example.invalid", Telefono="22223333", Direccion="Prueba", Activo=true, PasswordHash="!SIN-ACCESO-"+Guid.NewGuid() };
            db.Usuarios.Add(u); await db.SaveChangesAsync(); users.Add(u.UsuarioId);
        }
    }
    var f=Form("A"); f.EncargadoIds=users.Take(2).ToList();
    Check(await WithService(s=>s.GuardarAsync(f,true)) is null,"Crear sucursal con dos encargados en SQL Server");
    var saved=(await WithService(s=>s.ListarAsync())).Single(s=>s.Nombre==f.Nombre);
    f.SucursalId=saved.SucursalId;
    Check(saved.Encargados.Count==2,"Listado confirma asignación múltiple persistida");
    var preload=await WithService(s=>s.FormularioAsync(saved.SucursalId));
    Check(preload!.EncargadoIds.Count==2 && preload.Nombre==f.Nombre,"Editar precarga valores y encargados asignados");
    var other=Form("B"); other.EncargadoIds=[users[0]];
    Check(await WithService(s=>s.GuardarAsync(other,true)) is not null,"Encargado ya asignado rechazado");
    Check(!(await WithService(s=>s.FormularioAsync(null)))!.Disponibles.Any(u=>u.UsuarioId==users[0]),"Crear excluye encargados ocupados");
    f.Nombre+=" editada"; f.EncargadoIds=[users[1],users[2]];
    Check(await WithService(s=>s.GuardarAsync(f,false)) is null,"Editar y actualizar selección múltiple");
    var detail=await WithService(s=>s.ObtenerAsync(saved.SucursalId));
    Check(detail!.Nombre==f.Nombre && detail.Encargados.Select(u=>u.UsuarioId).Order().SequenceEqual(f.EncargadoIds.Order()),"Detalle refleja edición y selección");
    Check((await WithService(s=>s.FormularioAsync(null)))!.Disponibles.Any(u=>u.UsuarioId==users[0]),"Encargado retirado vuelve a estar disponible");
    var invalid=Form("invalida"); invalid.Correo="correo-invalido";
    Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"Correo inválido rechazado en servicio");
    invalid=Form("invalida"); invalid.Telefono="abc";
    Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"Teléfono inválido rechazado en servicio");
    invalid=Form("invalida"); invalid.EncargadoIds=[users[0],users[0]];
    Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"IDs duplicados rechazados");
    invalid=Form("invalida"); invalid.EncargadoIds=[int.MaxValue];
    Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"ID inexistente rechazado");
    await using (var db=new WashLabsDbContext(options))
    {
        Check(await db.Usuarios.CountAsync(u=>u.SucursalId==saved.SucursalId)==2,"SQL confirma exactamente dos asignaciones");
        Check(await db.Facturas.AnyAsync() && await db.Citas.AnyAsync(),"Datos de facturación y reservas Avance 3 conservados");
    }
    foreach (var campo in new[] { "nombre", "descripción", "dirección" })
    {
        invalid=Form("requerida");
        if(campo=="nombre") invalid.Nombre=" ";
        if(campo=="descripción") invalid.Descripcion=" ";
        if(campo=="dirección") invalid.Direccion=" ";
        Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"Campo obligatorio: "+campo);
    }
    invalid=Form("larga");invalid.Nombre=new string('N',101);
    Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"Longitud máxima de nombre respetada");
    invalid=Form("A");
    Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"Correo de sucursal duplicado rechazado");
    await using(var db=new WashLabsDbContext(options))
    {
        var admin=await db.Usuarios.Where(u=>u.Activo && u.Rol.Nombre=="Administrador").Select(u=>u.UsuarioId).SingleAsync();
        invalid=Form("rol");invalid.EncargadoIds=[admin];
        Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"Otro rol no puede asignarse como encargado");
        await db.Usuarios.Where(u=>u.UsuarioId==users[0]).ExecuteUpdateAsync(s=>s.SetProperty(u=>u.Activo,false));
    }
    invalid=Form("inactiva");invalid.EncargadoIds=[users[0]];
    Check(await WithService(s=>s.GuardarAsync(invalid,true)) is not null,"Encargado inactivo rechazado");
    Check(!(await WithService(s=>s.FormularioAsync(null)))!.Disponibles.Any(u=>u.UsuarioId==users[0]),"Crear excluye encargados inactivos");
    await using(var db=new WashLabsDbContext(options))
        await db.Usuarios.Where(u=>u.UsuarioId==users[0]).ExecuteUpdateAsync(s=>s.SetProperty(u=>u.Activo,true));
    var concurrenteA=Form("ConcurrenteA");concurrenteA.EncargadoIds=[users[0]];
    var concurrenteB=Form("ConcurrenteB");concurrenteB.EncargadoIds=[users[0]];
    var resultados=await Task.WhenAll(WithService(s=>s.GuardarAsync(concurrenteA,true)),WithService(s=>s.GuardarAsync(concurrenteB,true)));
    Check(resultados.Count(e=>e is null)==1,"Concurrencia SQL: un encargado se asigna a una sola sucursal");
    await PruebasAgenda.EjecutarAsync(options,saved.SucursalId,prefix,Check);
    await PruebasHttp.EjecutarAsync(options,prefix,Check);
    Console.WriteLine($"Total: {passed} pruebas aprobadas.");
}
finally
{
    await using var db=new WashLabsDbContext(options);
    await db.BloqueosAgenda.Where(b=>b.Sucursal.Nombre.StartsWith(prefix)).ExecuteDeleteAsync();
    await db.HorariosSucursal.Where(h=>h.Sucursal.Nombre.StartsWith(prefix)).ExecuteDeleteAsync();
    await db.Usuarios.Where(u=>u.NombreCompleto.StartsWith(prefix)).ExecuteDeleteAsync();
    await db.Sucursales.Where(s=>s.Nombre.StartsWith(prefix)).ExecuteDeleteAsync();
}
