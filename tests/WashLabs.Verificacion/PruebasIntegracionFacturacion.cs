using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Repositories;
using WashLabs.Models.DTOs;
using WashLabs.Services.Implementations;

internal static class PruebasIntegracionFacturacion
{
    public static async Task EjecutarAsync()
    {
        var cadena = Environment.GetEnvironmentVariable("WASHLABS_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Configura WASHLABS_TEST_CONNECTION.");
        if (new SqlConnectionStringBuilder(cadena).InitialCatalog != "WashLabs_Pruebas_Avance3")
            throw new InvalidOperationException("Solo se permite la base aislada WashLabs_Pruebas_Avance3.");
        DbContextOptionsBuilder<WashLabsDbContext> Opciones() => new DbContextOptionsBuilder<WashLabsDbContext>()
            .UseSqlServer(cadena, s => s.UseCompatibilityLevel(150));
        WashLabsDbContext Contexto() => new(Opciones().Options);
        var total = 0;
        void Verificar(bool condicion, string nombre)
        {
            if (!condicion) throw new InvalidOperationException("FALLÓ: " + nombre);
            total++;
            Console.WriteLine("APROBADA: " + nombre);
        }
        await using var db = Contexto();
        var cliente = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Activo && u.Rol.Nombre == "Cliente");
        var encargado = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Activo && u.Rol.Nombre == "Encargado" && u.Sucursal!.Activa);
        var admin = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Activo && u.Rol.Nombre == "Administrador");
        var estados = await db.EstadosFactura.ToListAsync();
        foreach (var nombre in new[] { "Proforma", "Pendiente", "Completada", "Anulada" })
            if (estados.All(e => e.Nombre != nombre))
            {
                var estado = new EstadoFactura { EstadoFacturaId = (byte)(estados.Select(e => (int)e.EstadoFacturaId).DefaultIfEmpty().Max() + 1), Nombre = nombre };
                estados.Add(estado);
                db.EstadosFactura.Add(estado);
            }
        var marca = Guid.NewGuid().ToString("N")[..8];
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(2000 + Random.Shared.Next(2000)));
        var servicio = new Servicio { Nombre = "TST integración " + marca, Descripcion = "Servicio de prueba", TarifaBase = 1000m,
            DuracionMinutos = 60, TipoLavado = "EXTERIOR", NivelDetalle = "BASICO", Activo = true };
        var tarifa = new SucursalServicio { SucursalId = encargado.SucursalId!.Value, Servicio = servicio, PrecioSucursal = 1234.56m, Activo = true };
        var vehiculo = new Vehiculo { ClienteId = cliente.UsuarioId, Matricula = "TST" + marca.ToUpperInvariant(),
            Marca = "Pruebas", Modelo = "Integración", CategoriaTamano = "MEDIANO", Activo = true };
        var categoria = new CategoriaProducto { Nombre = "TST " + marca, Activa = true };
        var productoA = new Producto { CategoriaProducto = categoria, Nombre = "TST producto A " + marca,
            Descripcion = "Prueba", Precio = 50.25m, Activo = true };
        var productoB = new Producto { CategoriaProducto = categoria, Nombre = "TST producto B " + marca,
            Descripcion = "Prueba", Precio = 79.99m, Activo = true };
        db.SucursalServicios.Add(tarifa);
        db.Vehiculos.Add(vehiculo);
        db.Productos.AddRange(productoA, productoB);
        for (byte dia = 1; dia <= 7; dia++)
            db.HorariosSucursal.Add(new HorarioSucursal { SucursalId = tarifa.SucursalId, DiaSemana = dia,
                HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(18, 0), Activo = true,
                FechaVigenciaDesde = fecha, FechaVigenciaHasta = fecha.AddDays(20) });
        await db.SaveChangesAsync();
        var preguntas = await db.PreguntasReserva.Where(p => p.Activa).Select(p => p.PreguntaReservaId).ToListAsync();
        CrearReservaDTO Solicitud(int dia) => new() { ClienteId = cliente.UsuarioId, VehiculoId = vehiculo.VehiculoId,
            SucursalId = tarifa.SucursalId, ServicioId = servicio.ServicioId, Fecha = fecha.AddDays(dia), HoraInicio = new TimeOnly(9, 0),
            Observaciones = "TST integración " + marca, Respuestas = preguntas.Select(p => new CrearRespuestaReservaDTO { PreguntaId = p, Respuesta = "Prueba" }).ToList() };
        async Task<ResultadoCrearReservaDTO> Crear(CrearReservaDTO solicitud, int? actor = null)
        {
            await using var ctx = Contexto();
            return await new ReservaService(new ReservaRepository(ctx)).CrearAsync(solicitud, actor ?? cliente.UsuarioId);
        }
        async Task<Factura> Documento(long citaId) => await db.Facturas.AsNoTracking().Include(f => f.DetallesFactura)
            .Include(f => f.EstadoFactura).SingleAsync(f => f.CitaId == citaId);
        async Task<bool> Completar(long id, int? actor = null)
        {
            await using var ctx = Contexto();
            return await new OperacionRepository(ctx).CompletarCitaAsync(id, actor ?? encargado.UsuarioId, default);
        }
        async Task<bool> Producto(long id, int productoId, decimal cantidad, int? actor = null)
        {
            await using var ctx = Contexto();
            return await new FacturaRepository(ctx).AgregarProductoAsync(id, productoId, cantidad, actor ?? encargado.UsuarioId, default);
        }
        async Task<int> Movimientos(long id) => await db.MovimientosWashPoints.CountAsync(m => m.CitaId == id && m.TipoMovimiento == "ACUMULA");
        var solicitud = Solicitud(0);
        var creada = await Crear(solicitud);
        Verificar(creada.Exito && creada.CitaId.HasValue, "Crear reserva mediante servicio real");
        var citaId = creada.CitaId!.Value;
        var proforma = await Documento(citaId);
        Verificar(proforma.TipoDocumento == "PROFORMA" && proforma.EstadoFactura.Nombre == "Proforma", "Reserva genera proforma");
        Verificar(proforma.ClienteId == cliente.UsuarioId && proforma.SucursalId == tarifa.SucursalId && proforma.CitaId == citaId, "Relaciones cliente, cita y sucursal");
        Verificar(proforma.DetallesFactura.Single().ServicioId == servicio.ServicioId && proforma.DetallesFactura.Single().PrecioUnitario == 1234.56m,
            "Servicio correcto y prioridad del precio de sucursal");
        Verificar(proforma.Subtotal == 1234.56m && proforma.Impuesto == 160.49m && proforma.Total == 1395.05m, "Totales de proforma e IVA 13%");
        var repetida = await Crear(solicitud);
        Verificar(repetida.CitaId == citaId && await db.Facturas.CountAsync(f => f.CitaId == citaId) == 1, "Repetir reserva conserva un documento");
        var invalidada = Solicitud(1);
        invalidada.VehiculoId = int.MaxValue;
        Verificar(!(await Crear(invalidada)).Exito, "Vehículo ajeno o inexistente rechazado");
        invalidada = Solicitud(1); invalidada.SucursalId = int.MaxValue;
        Verificar(!(await Crear(invalidada)).Exito, "Sucursal inexistente rechazada");
        invalidada = Solicitud(1); invalidada.ServicioId = int.MaxValue;
        Verificar(!(await Crear(invalidada)).Exito, "Servicio no disponible rechazado");
        invalidada = Solicitud(1); invalidada.Fecha = fecha.AddYears(-30);
        Verificar(!(await Crear(invalidada)).Exito, "Fecha pasada rechazada");
        invalidada = Solicitud(1); invalidada.HoraInicio = new TimeOnly(22, 0);
        Verificar(!(await Crear(invalidada)).Exito, "Horario fuera de apertura rechazado");
        var colision = Solicitud(0); colision.HoraInicio = new TimeOnly(9, 15);
        Verificar(!(await Crear(colision)).Exito, "Reservas incompatibles rechazadas");
        await using (var falla = new WashLabsDbContext(Opciones().AddInterceptors(new FallarDocumento(true)).Options))
        {
            try { await new ReservaService(new ReservaRepository(falla)).CrearAsync(Solicitud(2), cliente.UsuarioId); throw new InvalidOperationException("Fallo no provocado"); }
            catch (IOException) { }
        }
        Verificar(!await db.Citas.AnyAsync(c => c.VehiculoId == vehiculo.VehiculoId && c.Fecha == fecha.AddDays(2)), "Fallo al crear proforma revierte reserva");
        Verificar(await Producto(proforma.FacturaId, productoA.ProductoId, 2) && await Producto(proforma.FacturaId, productoB.ProductoId, 1), "Agregar dos productos de catálogo");
        Verificar(await Producto(proforma.FacturaId, productoA.ProductoId, 2) && (await Documento(citaId)).DetallesFactura.Count == 3, "Repetir inclusión idéntica no duplica línea");
        Verificar(!await Producto(proforma.FacturaId, productoA.ProductoId, 0) && !await Producto(proforma.FacturaId, productoA.ProductoId, -1), "Cantidades no positivas rechazadas");
        Verificar(!await Producto(proforma.FacturaId, int.MaxValue, 1) && !await Producto(proforma.FacturaId, productoA.ProductoId, 1.5m),
            "Producto inexistente y unidades fraccionarias rechazados");
        Verificar(!await Producto(proforma.FacturaId, productoA.ProductoId, 1, cliente.UsuarioId), "Cliente no agrega productos");
        Verificar(!await Completar(citaId, cliente.UsuarioId) && await Movimientos(citaId) == 0, "Cliente no finaliza citas");
        productoA.Precio = 900m;
        tarifa.PrecioSucursal = 9000m;
        await db.SaveChangesAsync();
        Verificar(await Completar(citaId), "Encargado completa cita con factura y puntos");
        var factura = await Documento(citaId);
        Verificar(factura.FacturaId == proforma.FacturaId && factura.TipoDocumento == "FACTURA" && factura.EstadoFactura.Nombre == "Pendiente"
            && !await db.PagosFactura.AnyAsync(p => p.FacturaId == factura.FacturaId), "Mismo documento definitivo pendiente sin inventar pago");
        Verificar(factura.DetallesFactura.Count == 3 && factura.DetallesFactura.Single(d => d.ProductoId == productoA.ProductoId).PrecioUnitario == 50.25m
            && factura.DetallesFactura.Single(d => d.ServicioId == servicio.ServicioId).PrecioUnitario == 1234.56m, "Conversión conserva tres líneas y precios pactados");
        Verificar(factura.Subtotal == 1415.05m && factura.Impuesto == 183.96m && factura.Total == 1599.01m
            && factura.Total == factura.DetallesFactura.Sum(d => d.Total ?? 0m), "Factura mixta calcula subtotal, IVA y total desde SQL");
        Verificar(await Movimientos(citaId) == 1 && await Completar(citaId) && await Movimientos(citaId) == 1
            && await db.Facturas.CountAsync(f => f.CitaId == citaId) == 1, "Segunda finalización no duplica factura ni lavado");
        Verificar(!await Producto(factura.FacturaId, productoB.ProductoId, 3), "Factura definitiva no admite edición de productos");
        var cancelada = await Crear(Solicitud(3));
        await using (var ctx = Contexto())
            Verificar(await new OperacionRepository(ctx).CancelarCitaAsync(cancelada.CitaId!.Value, cliente.UsuarioId, default), "Cliente cancela reserva propia");
        Verificar((await Documento(cancelada.CitaId!.Value)).EstadoFactura.Nombre == "Anulada"
            && !await Completar(cancelada.CitaId.Value) && await Movimientos(cancelada.CitaId.Value) == 0, "Cancelación conserva proforma anulada sin facturar ni acumular");
        var fallida = await Crear(Solicitud(4));
        await using (var falla = new WashLabsDbContext(Opciones().AddInterceptors(new FallarDocumento(false)).Options))
        {
            try { await new OperacionRepository(falla).CompletarCitaAsync(fallida.CitaId!.Value, encargado.UsuarioId, default); throw new InvalidOperationException("Fallo no provocado"); }
            catch (IOException) { }
        }
        Verificar((await Documento(fallida.CitaId!.Value)).TipoDocumento == "PROFORMA" && await Movimientos(fallida.CitaId.Value) == 0
            && await db.Citas.AnyAsync(c => c.CitaId == fallida.CitaId && c.EstadoCita.Nombre == "Pendiente"), "Fallo de factura revierte cita y WashPoints");
        var fallaPuntos = await Crear(Solicitud(5));
        await using (var falla = new WashLabsDbContext(Opciones().AddInterceptors(new FallarPuntos()).Options))
        {
            try { await new OperacionRepository(falla).CompletarCitaAsync(fallaPuntos.CitaId!.Value, encargado.UsuarioId, default); throw new InvalidOperationException("Fallo no provocado"); }
            catch (IOException) { }
        }
        Verificar((await Documento(fallaPuntos.CitaId!.Value)).TipoDocumento == "PROFORMA" && await Movimientos(fallaPuntos.CitaId.Value) == 0
            && await db.Citas.AnyAsync(c => c.CitaId == fallaPuntos.CitaId && c.EstadoCita.Nombre == "Pendiente"), "Fallo WashPoints revierte factura y cita");
        var concurrente = await Crear(Solicitud(6));
        var resultados = await Task.WhenAll(Completar(concurrente.CitaId!.Value), Completar(concurrente.CitaId.Value));
        Verificar(resultados.Any(r => r) && await Movimientos(concurrente.CitaId.Value) == 1
            && await db.Facturas.CountAsync(f => f.CitaId == concurrente.CitaId && f.TipoDocumento == "FACTURA") == 1, "Dos finalizaciones concurrentes: una factura y un lavado");
        var solicitudConcurrente = Solicitud(7);
        var reservasConcurrentes = await Task.WhenAll(Crear(solicitudConcurrente), Crear(solicitudConcurrente));
        Verificar(reservasConcurrentes.Any(r => r.Exito) && await db.Citas.CountAsync(c => c.VehiculoId == vehiculo.VehiculoId && c.Fecha == fecha.AddDays(7)) == 1,
            "Dos creaciones concurrentes no duplican reserva");
        tarifa.PrecioSucursal = null;
        await db.SaveChangesAsync();
        var baseCreada = await Crear(Solicitud(8));
        Verificar((await Documento(baseCreada.CitaId!.Value)).DetallesFactura.Single().PrecioUnitario == servicio.TarifaBase, "Tarifa base cuando no hay precio de sucursal");
        var mixtaConcurrente = await Crear(Solicitud(10));
        var mixtaDocumento = await Documento(mixtaConcurrente.CitaId!.Value);
        var cambiosConcurrentes = await Task.WhenAll(Producto(mixtaDocumento.FacturaId, productoB.ProductoId, 1), Completar(mixtaConcurrente.CitaId.Value));
        if (!cambiosConcurrentes[1]) Verificar(await Completar(mixtaConcurrente.CitaId.Value), "Reintento de finalización tras conflicto con productos");
        var mixtaFinal = await Documento(mixtaConcurrente.CitaId.Value);
        Verificar(mixtaFinal.TipoDocumento == "FACTURA" && mixtaFinal.DetallesFactura.Count == (cambiosConcurrentes[0] ? 2 : 1)
            && mixtaFinal.Subtotal == mixtaFinal.DetallesFactura.Sum(d => d.Subtotal ?? 0m)
            && mixtaFinal.Total == mixtaFinal.DetallesFactura.Sum(d => d.Total ?? 0m) && await Movimientos(mixtaConcurrente.CitaId.Value) == 1,
            "Incluir producto y finalizar simultáneamente conserva líneas y totales");
        var otraSucursal = new Sucursal { Nombre = "TST otra sucursal " + marca, Descripcion = "Prueba", Telefono = "00000000",
            Correo = "tst" + marca + "@example.invalid", Direccion = "Prueba", Activa = true };
        db.Sucursales.Add(otraSucursal);
        await db.SaveChangesAsync();
        db.SucursalServicios.Add(new SucursalServicio { SucursalId = otraSucursal.SucursalId, ServicioId = servicio.ServicioId, Activo = true });
        var otraCita = new Cita { ClienteId = cliente.UsuarioId, VehiculoId = vehiculo.VehiculoId, ServicioId = servicio.ServicioId,
            SucursalId = otraSucursal.SucursalId, EstadoCitaId = await db.EstadosCita.Where(e => e.Nombre == "Confirmada").Select(e => e.EstadoCitaId).SingleAsync(),
            Fecha = fecha.AddDays(9), HoraInicio = new TimeOnly(9, 0), HoraFin = new TimeOnly(10, 0), CreadoPorUsuarioId = admin.UsuarioId };
        db.Citas.Add(otraCita);
        await db.SaveChangesAsync();
        db.Facturas.Add(new Factura { CitaId = otraCita.CitaId, ClienteId = cliente.UsuarioId, SucursalId = otraSucursal.SucursalId,
            TipoDocumento = "PROFORMA", Origen = "CITA", EstadoFacturaId = estados.Single(e => e.Nombre == "Proforma").EstadoFacturaId,
            DetallesFactura = [new DetalleFactura { TipoLinea = "S", ServicioId = servicio.ServicioId, Descripcion = servicio.Nombre,
                Cantidad = 1, PrecioUnitario = 1000m, PorcentajeImpuesto = 13m }] });
        await db.SaveChangesAsync();
        Verificar(!await Completar(otraCita.CitaId) && await Movimientos(otraCita.CitaId) == 0, "Encargado no completa otra sucursal");
        Verificar(await Completar(otraCita.CitaId, admin.UsuarioId), "Administrador finaliza dentro de su alcance global");
        var repo = new FacturaRepository(db);
        Verificar(await repo.ObtenerAsync((await Documento(otraCita.CitaId)).FacturaId, default, encargado.UsuarioId) is null,
            "Detalle de factura restringido por sucursal");
        Verificar((await repo.ListarAsync(default, cliente.UsuarioId)).Any(f => f.FacturaId == proforma.FacturaId)
            && (await new FacturaService(repo).ObtenerAsync(proforma.FacturaId, default, cliente.UsuarioId))?.Lineas.Count == 3,
            "Historial y detalle de factura conservan servicio y productos");
        Verificar((await new ReservaRepository(db).ListarClienteAsync(cliente.UsuarioId, default)).Any(c => c.CitaId == citaId)
            && await new ReservaRepository(db).ObtenerClienteAsync(citaId, cliente.UsuarioId, default) is not null, "Listado y detalle de reservas siguen funcionando");
        var catalogo = new ProductoService(new ProductoRepository(db));
        Verificar((await catalogo.ListarAsync(null, null)).Any(p => p.ProductoId == productoA.ProductoId)
            && (await catalogo.ObtenerAsync(productoB.ProductoId))?.Precio == productoB.Precio, "Catálogo y detalle de productos sin regresiones");
        Console.WriteLine($"Total integración: {total} pruebas. Datos TST conservados exclusivamente en WashLabs_Pruebas_Avance3.");
    }

    private sealed class FallarDocumento(bool creacion) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Factura>().Any(e => e.State == (creacion ? EntityState.Added : EntityState.Modified)))
                throw new IOException("Fallo simulado al guardar factura.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FallarPuntos : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<MovimientoWashPoints>().Any())
                throw new IOException("Fallo simulado después de guardar WashPoints y antes del commit.");
            return ValueTask.FromResult(result);
        }
    }
}
