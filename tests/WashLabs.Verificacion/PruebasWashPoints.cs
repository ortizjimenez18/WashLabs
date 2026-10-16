using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Repositories;
using WashLabs.Services.Implementations;

internal static class PruebasWashPoints
{
    public static async Task PrepararAsync()
    {
        var cadena = Environment.GetEnvironmentVariable("WASHLABS_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Configura WASHLABS_TEST_CONNECTION.");
        var configuracion = new SqlConnectionStringBuilder(cadena);
        if (configuracion.InitialCatalog != "WashLabs_Pruebas_Avance3")
            throw new InvalidOperationException("La preparación solo acepta WashLabs_Pruebas_Avance3.");
        var catalogo = configuracion.InitialCatalog;
        configuracion.InitialCatalog = "master";
        await using var conexion = new SqlConnection(configuracion.ConnectionString);
        await conexion.OpenAsync();
        await using var consulta = new SqlCommand("SELECT DB_ID(@nombre)", conexion);
        consulta.Parameters.AddWithValue("@nombre", catalogo);
        var existe = await consulta.ExecuteScalarAsync() is not DBNull;
        await using var db = new WashLabsDbContext(new DbContextOptionsBuilder<WashLabsDbContext>().UseSqlServer(cadena).Options);
        if (!existe) await db.Database.EnsureCreatedAsync();
        else if (await db.Usuarios.AnyAsync() || await db.Roles.AnyAsync() || await db.Sucursales.AnyAsync()
            || await db.EstadosCita.AnyAsync() || await db.Servicios.AnyAsync() || await db.ConfiguracionWashPoints.AnyAsync())
            throw new InvalidOperationException("La base de pruebas ya contiene datos. No se sobrescriben; usa --washpoints.");
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var sucursal = new Sucursal { Nombre = "DEMO Avance 3", Descripcion = "Datos sintéticos de pruebas", Telefono = "00000000",
            Correo = "sucursal@example.invalid", Direccion = "Datos sintéticos de pruebas", Activa = true };
        var servicio = new Servicio { Nombre = "DEMO lavado", Descripcion = "Datos sintéticos de pruebas", DuracionMinutos = 60,
            TarifaBase = 1000m, TipoLavado = "EXTERIOR", NivelDetalle = "BASICO", Activo = true };
        var rolCliente = new Rol { RolId = 3, Nombre = "Cliente" };
        var rolEncargado = new Rol { RolId = 2, Nombre = "Encargado" };
        var rolAdmin = new Rol { RolId = 1, Nombre = "Administrador" };
        Usuario Usuario(string nombre, Rol rol, Sucursal? sede = null) => new() { NombreCompleto = "DEMO " + nombre, Rol = rol,
            Sucursal = sede, Telefono = "00000000", Correo = nombre.ToLowerInvariant() + "@example.invalid", Direccion = "Datos sintéticos de pruebas",
            PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>().HashPassword(new Usuario(),
                Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))), Activo = true };
        db.Usuarios.AddRange(Usuario("Cliente", rolCliente), Usuario("Encargado", rolEncargado, sucursal), Usuario("Administrador", rolAdmin));
        db.SucursalServicios.Add(new SucursalServicio { Sucursal = sucursal, Servicio = servicio, Activo = true });
        db.ConfiguracionWashPoints.Add(new ConfiguracionWashPoints { ConfiguracionWashPointsId = 1, MetaLavados = 10, DescripcionRecompensa = "DEMO meta de 10 lavados", Activa = true });
        byte estadoId = 1;
        foreach (var nombre in new[] { "Pendiente", "Confirmada", "Reprogramada", "Completada", "Cancelada", "No asistió" })
            db.EstadosCita.Add(new EstadoCita { EstadoCitaId = estadoId++, Nombre = nombre, ColorHex = "#73787D" });
        byte estadoFacturaId = 1;
        foreach (var nombre in new[] { "Proforma", "Pendiente", "Completada", "Anulada" })
            db.EstadosFactura.Add(new EstadoFactura { EstadoFacturaId = estadoFacturaId++, Nombre = nombre });
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        Console.WriteLine("Base aislada creada con el modelo EF: 3 usuarios DEMO, 3 roles, 1 sucursal, 1 servicio, 1 relación sucursal-servicio, 6 estados y 1 configuración. Ninguna contraseña se muestra.");
    }

    public static async Task EjecutarAsync()
    {
        var cadena = Environment.GetEnvironmentVariable("WASHLABS_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Configura WASHLABS_TEST_CONNECTION para una copia aislada de pruebas.");
        var conexion = new SqlConnectionStringBuilder(cadena);
        if (!conexion.InitialCatalog.StartsWith("WashLabs_Pruebas_Avance3", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Estas pruebas solo aceptan bases llamadas WashLabs_Pruebas_Avance3*. No crean ni borran bases.");
        DbContextOptionsBuilder<WashLabsDbContext> Opciones() => new DbContextOptionsBuilder<WashLabsDbContext>()
            .UseSqlServer(cadena, s => s.UseCompatibilityLevel(150));
        WashLabsDbContext Contexto() => new(Opciones().Options);
        static void Verificar(bool condicion, string nombre)
        {
            if (!condicion) throw new InvalidOperationException("FALLÓ: " + nombre);
            Console.WriteLine("APROBADA: " + nombre);
        }
        await using var db = Contexto();
        var encargado = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Activo && u.Rol.Nombre == "Encargado" && u.Sucursal != null && u.Sucursal.Activa);
        var cliente = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Activo && u.Rol.Nombre == "Cliente");
        var servicio = await db.SucursalServicios.AsNoTracking().FirstAsync(s => s.SucursalId == encargado.SucursalId && s.Activo && s.Servicio.Activo);
        var estados = await db.EstadosCita.AsNoTracking().ToDictionaryAsync(e => e.Nombre, e => e.EstadoCitaId);
        var marca = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Vehiculo Vehiculo(string sufijo) => new() { ClienteId = cliente.UsuarioId, Matricula = "TST" + marca + sufijo,
            Marca = "Pruebas", Modelo = "Avance 3", CategoriaTamano = "MEDIANO", Activo = true };
        var primero = Vehiculo("A");
        var segundo = Vehiculo("B");
        db.Vehiculos.AddRange(primero, segundo);
        await db.SaveChangesAsync();
        Cita Cita(Vehiculo vehiculo, string estado, int dia) => new() {
            ClienteId = cliente.UsuarioId, VehiculoId = vehiculo.VehiculoId, SucursalId = encargado.SucursalId!.Value,
            ServicioId = servicio.ServicioId, EstadoCitaId = estados[estado],
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(30 + dia)), HoraInicio = new TimeOnly(10, 0), HoraFin = new TimeOnly(11, 0),
            CreadoPorUsuarioId = encargado.UsuarioId, Observaciones = "PRUEBA AVANCE 3 " + marca };
        var normal = Cita(primero, "Confirmada", 0);
        var otra = Cita(segundo, "Reprogramada", 1);
        var pendiente = Cita(primero, "Pendiente", 2);
        var cancelada = Cita(primero, "Cancelada", 3);
        var ausente = Cita(primero, "No asistió", 4);
        var concurrente = Cita(primero, "Confirmada", 5);
        var fallida = Cita(primero, "Confirmada", 6);
        var simultaneaA = Cita(primero, "Confirmada", 7);
        var simultaneaB = Cita(primero, "Confirmada", 8);
        db.Citas.AddRange(normal, otra, pendiente, cancelada, ausente, concurrente, fallida, simultaneaA, simultaneaB);
        await db.SaveChangesAsync();
        var estadoProforma = await db.EstadosFactura.Where(e => e.Nombre == "Proforma").Select(e => e.EstadoFacturaId).SingleAsync();
        foreach (var cita in new[] { normal, otra, pendiente, cancelada, ausente, concurrente, fallida, simultaneaA, simultaneaB })
            db.Facturas.Add(new Factura { CitaId = cita.CitaId, ClienteId = cita.ClienteId, SucursalId = cita.SucursalId,
                TipoDocumento = "PROFORMA", Origen = "CITA", EstadoFacturaId = estadoProforma,
                DetallesFactura = [new DetalleFactura { TipoLinea = "S", ServicioId = cita.ServicioId,
                    Descripcion = "Servicio de prueba", Cantidad = 1, PrecioUnitario = 1000m, PorcentajeImpuesto = 13m }] });
        await db.SaveChangesAsync();
        async Task<bool> Completar(long id)
        {
            await using var contexto = Contexto();
            return await new OperacionRepository(contexto).CompletarCitaAsync(id, encargado.UsuarioId, default);
        }
        async Task<int> Contador(int vehiculoId) => await db.TarjetasWashPoints.AsNoTracking()
            .Where(t => t.VehiculoId == vehiculoId).Select(t => t.LavadosAcumulados).SingleOrDefaultAsync();
        async Task<int> Movimientos(long citaId) => await db.MovimientosWashPoints.CountAsync(m => m.CitaId == citaId && m.TipoMovimiento == "ACUMULA");
        Verificar(await Completar(normal.CitaId) && await Contador(primero.VehiculoId) == 1 && await Movimientos(normal.CitaId) == 1, "A: completar acredita exactamente un lavado");
        Verificar(await Completar(normal.CitaId) && await Contador(primero.VehiculoId) == 1 && await Movimientos(normal.CitaId) == 1, "B: repetir no acredita otro lavado");
        Verificar(await Completar(otra.CitaId) && await Contador(segundo.VehiculoId) == 1 && await Contador(primero.VehiculoId) == 1, "C: contadores independientes");
        Verificar(await Movimientos(pendiente.CitaId) == 0 && await Contador(primero.VehiculoId) == 1, "D: pendiente sin completar no acumula");
        Verificar(!await Completar(cancelada.CitaId) && await Movimientos(cancelada.CitaId) == 0, "E: cancelada no acumula");
        Verificar(!await Completar(ausente.CitaId) && await Movimientos(ausente.CitaId) == 0, "F: No asistió no acumula");
        var consulta = await new OperacionService(new OperacionRepository(db)).WashPointsAsync(default, cliente.UsuarioId);
        Verificar(consulta.Single(v => v.Vehiculo.VehiculoId == primero.VehiculoId).Lavados == 1, "G: tarjeta coincide con SQL");
        await using (var falla = new WashLabsDbContext(Opciones().AddInterceptors(new FallarMovimiento()).Options))
        {
            try
            {
                await new OperacionRepository(falla).CompletarCitaAsync(fallida.CitaId, encargado.UsuarioId, default);
                throw new InvalidOperationException("No se provocó el fallo transaccional esperado.");
            }
            catch (IOException) { }
        }
        Verificar(await db.Citas.AsNoTracking().Where(c => c.CitaId == fallida.CitaId).Select(c => c.EstadoCitaId).SingleAsync() == estados["Confirmada"]
            && await Movimientos(fallida.CitaId) == 0 && await Contador(primero.VehiculoId) == 1, "H: fallo revierte estado, tarjeta y movimiento");
        var resultados = await Task.WhenAll(Completar(concurrente.CitaId), Completar(concurrente.CitaId));
        Verificar(resultados.Any(r => r) && await Movimientos(concurrente.CitaId) == 1 && await Contador(primero.VehiculoId) == 2, "I: concurrencia de la misma cita sin duplicados");
        var diferentes = await Task.WhenAll(Completar(simultaneaA.CitaId), Completar(simultaneaB.CitaId));
        if (!diferentes[0]) Verificar(await Completar(simultaneaA.CitaId), "Reintento tras conflicto de concurrencia A");
        if (!diferentes[1]) Verificar(await Completar(simultaneaB.CitaId), "Reintento tras conflicto de concurrencia B");
        Verificar(await Contador(primero.VehiculoId) == 4 && await Movimientos(simultaneaA.CitaId) == 1 && await Movimientos(simultaneaB.CitaId) == 1,
            "Dos citas del mismo vehículo no pierden incrementos");
        Console.WriteLine("Los vehículos TST y las citas PRUEBA AVANCE 3 quedan en la base aislada para inspección. No se eliminaron datos.");
    }

    private sealed class FallarMovimiento : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<MovimientoWashPoints>().Any(e => e.State == EntityState.Added))
                throw new IOException("Fallo simulado antes de guardar el movimiento.");
            return ValueTask.FromResult(result);
        }
    }
}
