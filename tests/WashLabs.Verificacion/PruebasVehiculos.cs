using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Repositories;
using WashLabs.Models.DTOs;
using WashLabs.Services.Implementations;

internal static class PruebasVehiculos
{
    public static async Task EjecutarAsync()
    {
        var cadena = Environment.GetEnvironmentVariable("WASHLABS_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Configura WASHLABS_TEST_CONNECTION.");
        if (new SqlConnectionStringBuilder(cadena).InitialCatalog != "WashLabs_Pruebas_Avance3")
            throw new InvalidOperationException("Solo se permite la base aislada WashLabs_Pruebas_Avance3.");
        await using var db = new WashLabsDbContext(new DbContextOptionsBuilder<WashLabsDbContext>().UseSqlServer(cadena).Options);
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var cliente = await db.Usuarios.FirstAsync(u => u.Activo && u.Rol.Nombre == "Cliente");
        var marca = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var primero = new Vehiculo { ClienteId = cliente.UsuarioId, Matricula = "VA" + marca,
            Marca = "Hyundai", Modelo = "Accent", CategoriaTamano = "MEDIANO", Activo = true };
        var segundo = new Vehiculo { ClienteId = cliente.UsuarioId, Matricula = "VB" + marca,
            Marca = "Kia", Modelo = "Rio", CategoriaTamano = "MEDIANO", Activo = true };
        db.Vehiculos.AddRange(primero, segundo);
        await db.SaveChangesAsync();
        var servicio = new OperacionService(new OperacionRepository(db));
        var total = 0;
        void Verificar(bool ok, string mensaje)
        {
            if (!ok) throw new InvalidOperationException("FALLÓ: " + mensaje);
            total++;
            Console.WriteLine("APROBADA: " + mensaje);
        }
        Verificar((await servicio.VehiculoAsync(primero.VehiculoId, cliente.UsuarioId))?.Matricula == primero.Matricula,
            "Detalle devuelve el vehículo del cliente");
        Verificar(await servicio.VehiculoAsync(primero.VehiculoId, int.MaxValue) is null,
            "Cliente ajeno no puede consultar el detalle");
        var solicitud = new RegistroVehiculoDTO { ClienteId = cliente.UsuarioId, Matricula = "vc" + marca.ToLowerInvariant(),
            Marca = " Toyota ", Modelo = " Yaris ", Anio = 2020, Color = " Blanco ", CategoriaTamano = "PEQUENO" };
        Verificar(!(await servicio.EditarVehiculoAsync(primero.VehiculoId, solicitud, int.MaxValue)).Exito,
            "Cliente ajeno no puede editar");
        solicitud.Matricula = segundo.Matricula;
        Verificar(!(await servicio.EditarVehiculoAsync(primero.VehiculoId, solicitud, cliente.UsuarioId)).Exito,
            "Matrícula duplicada rechazada");
        solicitud.Matricula = "vc" + marca.ToLowerInvariant();
        solicitud.Anio = 1800;
        Verificar(!(await servicio.EditarVehiculoAsync(primero.VehiculoId, solicitud, cliente.UsuarioId)).Exito,
            "Año fuera de rango rechazado");
        solicitud.Anio = 2020;
        Verificar((await servicio.EditarVehiculoAsync(primero.VehiculoId, solicitud, cliente.UsuarioId)).Exito,
            "Edición válida guardada");
        var editado = await servicio.VehiculoAsync(primero.VehiculoId, null);
        Verificar(editado is { Marca: "Toyota", Modelo: "Yaris", Anio: 2020, Color: "Blanco", CategoriaTamano: "PEQUENO" }
            && editado.Matricula == "VC" + marca && editado.ClienteId == cliente.UsuarioId,
            "Detalle refleja cambios normalizados y conserva propietario e identificador");
        await transaccion.RollbackAsync();
        Console.WriteLine($"Total: {total} comprobaciones. Transacción revertida.");
    }
}
