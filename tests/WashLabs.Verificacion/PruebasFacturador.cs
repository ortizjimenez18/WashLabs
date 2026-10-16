using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;
using WashLabs.Data.Repositories;
using WashLabs.Models.DTOs;
using WashLabs.Services.Implementations;

internal static class PruebasFacturador
{
    public static async Task EjecutarAsync()
    {
        var cadena = Environment.GetEnvironmentVariable("WASHLABS_TEST_CONNECTION") ?? throw new InvalidOperationException("Configura WASHLABS_TEST_CONNECTION.");
        if (new SqlConnectionStringBuilder(cadena).InitialCatalog != "WashLabs_Pruebas_Avance3")
            throw new InvalidOperationException("El facturador solo se prueba con escrituras en WashLabs_Pruebas_Avance3.");
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
        var estados = await db.EstadosFactura.ToDictionaryAsync(e => e.Nombre, e => e.EstadoFacturaId);
        var estadosCita = await db.EstadosCita.ToDictionaryAsync(e => e.Nombre, e => e.EstadoCitaId);
        var servicio = await db.SucursalServicios.AsNoTracking().FirstAsync(s => s.SucursalId == encargado.SucursalId && s.Activo && s.Servicio.Activo);
        var marca = Guid.NewGuid().ToString("N")[..8];
        var otraSucursal = new Sucursal { Nombre = "TST facturador " + marca, Descripcion = "Prueba", Direccion = "Prueba",
            Telefono = "00000000", Correo = marca + "@example.invalid", Activa = true };
        var ajeno = new Usuario { RolId = cliente.RolId, NombreCompleto = "TST ajeno " + marca, Identificacion = "TST-" + marca,
            Correo = "ajeno" + marca + "@example.invalid", Telefono = "00000000", Direccion = "Prueba", PasswordHash = Guid.NewGuid().ToString("N"), Activo = false };
        var vehiculo = new Vehiculo { ClienteId = cliente.UsuarioId, Matricula = "TSTF" + marca.ToUpperInvariant(), Marca = "Prueba",
            Modelo = "Facturador", CategoriaTamano = "MEDIANO", Activo = true };
        var categoria = new CategoriaProducto { Nombre = "TST facturador " + marca, Activa = true };
        var producto = new Producto { CategoriaProducto = categoria, Nombre = "TST edición " + marca, Descripcion = "Prueba", Precio = 50.25m, Activo = true };
        var adicional = new Producto { CategoriaProducto = categoria, Nombre = "TST adicional " + marca, Descripcion = "Prueba", Precio = 20m, Activo = true };
        db.Sucursales.Add(otraSucursal); db.Usuarios.Add(ajeno); db.Vehiculos.Add(vehiculo); db.Productos.AddRange(producto, adicional);
        await db.SaveChangesAsync();
        var fecha = DateTime.Today.AddYears(30).AddDays(Random.Shared.Next(1000)).AddHours(12);
        Factura Documento(string tipo, string estado, decimal precio, int dias, int? clienteId = null, int? sucursalId = null) => new() {
            ClienteId = clienteId ?? cliente.UsuarioId, SucursalId = sucursalId ?? encargado.SucursalId!.Value,
            TipoDocumento = tipo, EstadoFacturaId = estados[estado], Origen = "CITA", Fecha = fecha.AddDays(dias),
            Cita = new Cita { ClienteId = clienteId ?? cliente.UsuarioId, SucursalId = sucursalId ?? encargado.SucursalId!.Value,
                ServicioId = servicio.ServicioId, VehiculoId = vehiculo.VehiculoId, EstadoCitaId = estadosCita[tipo == "FACTURA" ? "Completada" : estado == "Anulada" ? "Cancelada" : "Pendiente"],
                Fecha = DateOnly.FromDateTime(fecha.AddDays(dias)), HoraInicio = new TimeOnly(9, 0), HoraFin = new TimeOnly(10, 0), CreadoPorUsuarioId = admin.UsuarioId },
            DetallesFactura = [new DetalleFactura { TipoLinea = "S", ServicioId = servicio.ServicioId, Descripcion = "Servicio facturador " + marca,
                Cantidad = 1, PrecioUnitario = precio, PorcentajeImpuesto = 13m }] };
        var reciente = Documento("FACTURA", "Pendiente", 100m, 0);
        var empate = Documento("FACTURA", "Pendiente", 200m, 0);
        var antigua = Documento("FACTURA", "Completada", 50m, -1);
        var anulada = Documento("FACTURA", "Anulada", 1000m, -2);
        var proforma = Documento("PROFORMA", "Proforma", 1000m, 1);
        proforma.Origen = "MIXTA";
        proforma.DetallesFactura.Add(new DetalleFactura { TipoLinea = "P", ProductoId = producto.ProductoId, Descripcion = producto.Nombre,
            Cantidad = 2, PrecioUnitario = producto.Precio, Descuento = 5m, PorcentajeImpuesto = 13m });
        var cancelada = Documento("PROFORMA", "Anulada", 100m, -2);
        var otraSede = Documento("PROFORMA", "Proforma", 100m, 0, sucursalId: otraSucursal.SucursalId);
        otraSede.Origen = "MIXTA";
        otraSede.DetallesFactura.Add(new DetalleFactura { TipoLinea = "P", ProductoId = producto.ProductoId, Descripcion = producto.Nombre,
            Cantidad = 2, PrecioUnitario = producto.Precio, PorcentajeImpuesto = 13m });
        var otroCliente = Documento("FACTURA", "Pendiente", 100m, 0, ajeno.UsuarioId);
        var documentos = new[] { reciente, empate, antigua, anulada, proforma, cancelada, otraSede, otroCliente };
        db.Facturas.AddRange(documentos);
        await db.SaveChangesAsync();
        foreach (var documento in documentos)
        {
            var lineas = await db.DetallesFactura.AsNoTracking().Where(d => d.FacturaId == documento.FacturaId).ToListAsync();
            documento.Subtotal = lineas.Sum(d => d.Subtotal ?? 0m);
            documento.Impuesto = lineas.Sum(d => d.Impuesto ?? 0m);
            documento.Total = documento.Subtotal + documento.Impuesto;
        }
        await db.SaveChangesAsync();
        var desde = DateOnly.FromDateTime(fecha.AddDays(-2));
        var hasta = DateOnly.FromDateTime(fecha.AddDays(1));
        FiltroFacturacionDTO Filtro(string orden = "recientes") => new() { FechaInicial = desde, FechaFinal = hasta, Orden = orden };
        async Task<HistorialFacturacionDTO> Consultar(FiltroFacturacionDTO filtro, int? actor = null)
        {
            await using var ctx = Contexto();
            return await new FacturaService(new FacturaRepository(ctx)).ConsultarAsync(filtro, actor ?? admin.UsuarioId, default);
        }
        var historial = await Consultar(Filtro());
        Verificar(historial.Documentos.Select(f => f.FacturaId).SequenceEqual(documentos.OrderByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId).Select(f => f.FacturaId)), "Orden predeterminado por fecha e ID numérico descendentes");
        Verificar((await Consultar(Filtro("antiguas"))).Documentos.Select(f => f.FacturaId).SequenceEqual(documentos.OrderBy(f => f.Fecha).ThenBy(f => f.FacturaId).Select(f => f.FacturaId)), "Orden de antiguas a recientes con desempate");
        foreach (var orden in new[] { "mayor", "menor" })
        {
            var esperadas = orden == "mayor" ? documentos.OrderByDescending(f => f.Total).ThenByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId)
                : documentos.OrderBy(f => f.Total).ThenByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId);
            Verificar((await Consultar(Filtro(orden))).Documentos.Select(f => f.FacturaId).SequenceEqual(esperadas.Select(f => f.FacturaId)), "Orden por importe " + orden);
        }
        Verificar(historial.Resumen.Facturas == 5 && historial.Resumen.ProformasPendientes == 2 && historial.Resumen.TotalFacturado == 508.50m,
            "Resumen SQL excluye proformas y facturas anuladas del total emitido");
        Verificar(historial.Estados.Count == estados.Count && historial.Sucursales.Any(s => s.SucursalId == otraSucursal.SucursalId), "Estados reales y sucursales del administrador");
        var filtro = Filtro(); filtro.FechaInicial = DateOnly.FromDateTime(fecha); filtro.FechaFinal = filtro.FechaInicial;
        Verificar((await Consultar(filtro)).Documentos.Count == 4, "Fechas inicial y final incluyen el día completo");
        filtro = Filtro(); filtro.Cliente = "TST ajeno " + marca;
        Verificar((await Consultar(filtro)).Documentos.Single().FacturaId == otroCliente.FacturaId, "Búsqueda por nombre de cliente");
        filtro.Cliente = ajeno.Identificacion;
        Verificar((await Consultar(filtro)).Documentos.Single().FacturaId == otroCliente.FacturaId, "Búsqueda por identificación");
        Verificar((await Consultar(filtro, cliente.UsuarioId)).Documentos.Count == 0, "Cliente no accede a documentos ajenos mediante filtros");
        filtro = Filtro(); filtro.TipoDocumento = "PROFORMA";
        Verificar((await Consultar(filtro)).Documentos.Count == 3 && (await Consultar(filtro)).Resumen.TotalFacturado == 0, "Filtro por tipo sin contar proformas como facturas");
        filtro.EstadoFacturaId = estados["Proforma"];
        filtro.SucursalId = encargado.SucursalId;
        Verificar((await Consultar(filtro)).Documentos.Single().FacturaId == proforma.FacturaId, "Filtros combinados por tipo y estado");
        filtro.SucursalId = encargado.SucursalId; filtro.NumeroDocumento = proforma.FacturaId; filtro.Cliente = cliente.NombreCompleto;
        Verificar((await Consultar(filtro)).Documentos.Single().FacturaId == proforma.FacturaId, "Todos los filtros y orden se combinan en SQL");
        filtro = Filtro(); filtro.NumeroDocumento = empate.FacturaId;
        Verificar((await Consultar(filtro)).Documentos.Single().FacturaId == empate.FacturaId, "Búsqueda por número exacto");
        filtro.NumeroDocumento = long.MaxValue;
        Verificar((await Consultar(filtro)).Documentos.Count == 0 && (await Consultar(filtro)).Resumen.TotalFacturado == 0, "Sin resultados conserva resumen en cero");
        filtro = Filtro(); filtro.SucursalId = otraSucursal.SucursalId;
        Verificar((await Consultar(filtro)).Documentos.Single().FacturaId == otraSede.FacturaId, "Administrador filtra por sucursal");
        Verificar((await Consultar(filtro, encargado.UsuarioId)).Documentos.Count == 0, "Filtro de sucursal no amplía el alcance del encargado");
        Verificar((await Consultar(Filtro(), cliente.UsuarioId)).Documentos.All(f => f.ClienteId == cliente.UsuarioId), "Listado del cliente conserva propiedad");
        Verificar((await Consultar(new FiltroFacturacionDTO())).Documentos.Count >= documentos.Length, "Limpiar filtros restablece documentos autorizados");
        try { await Consultar(new FiltroFacturacionDTO { FechaInicial = hasta, FechaFinal = desde }); throw new InvalidOperationException("Faltó validar fechas"); }
        catch (ValidationException) { Verificar(true, "Rango de fechas invertido rechazado por el servicio"); }
        try { await Consultar(new FiltroFacturacionDTO { Orden = "arbitrario" }); throw new InvalidOperationException("Faltó validar orden"); }
        catch (ValidationException) { Verificar(true, "Orden desconocido rechazado"); }

        async Task<FacturaDTO> Detalle(long? id = null)
        {
            await using var ctx = Contexto();
            return await new FacturaService(new FacturaRepository(ctx)).ObtenerAsync(id ?? proforma.FacturaId, default, admin.UsuarioId)
                ?? throw new InvalidOperationException("Documento no encontrado");
        }
        async Task<bool> Cambiar(long linea, decimal cantidad, int? actor = null, long? facturaId = null)
        {
            await using var ctx = Contexto();
            return await new FacturaService(new FacturaRepository(ctx)).CambiarCantidadAsync(facturaId ?? proforma.FacturaId, linea, cantidad, actor ?? encargado.UsuarioId, default);
        }
        async Task<bool> Quitar(long linea, int? actor = null, long? facturaId = null)
        {
            await using var ctx = Contexto();
            return await new FacturaService(new FacturaRepository(ctx)).QuitarProductoAsync(facturaId ?? proforma.FacturaId, linea, actor ?? encargado.UsuarioId, default);
        }
        async Task<bool> Agregar(int productoId, decimal cantidad, int? actor = null)
        {
            await using var ctx = Contexto();
            return await new FacturaService(new FacturaRepository(ctx)).AgregarProductoAsync(proforma.FacturaId, productoId, cantidad, actor ?? encargado.UsuarioId, default);
        }
        var detalle = await Detalle();
        var lineaProducto = detalle.Lineas.Single(l => l.ProductoId == producto.ProductoId).DetalleFacturaId;
        var lineaServicio = detalle.Lineas.Single(l => l.Tipo == "Servicio").DetalleFacturaId;
        Verificar(detalle.Sucursal.SucursalId == encargado.SucursalId && detalle.Cliente == cliente.NombreCompleto
            && detalle.CitaId == proforma.CitaId && detalle.PermiteProductos, "Detalle conserva encabezado, líneas y edición de proforma");
        producto.Precio = 800m;
        await db.SaveChangesAsync();
        Verificar(await Cambiar(lineaProducto, 3), "Encargado cambia cantidad de producto");
        detalle = await Detalle();
        var linea = detalle.Lineas.Single(l => l.DetalleFacturaId == lineaProducto);
        Verificar(linea.Cantidad == 3 && linea.PrecioUnitario == 50.25m && linea.Descuento == 5m, "Cambio conserva precio pactado y descuento");
        Verificar(linea.Subtotal == 145.75m && linea.Impuesto == 18.95m && detalle.Subtotal == 1145.75m && detalle.Total == 1294.70m,
            "Cambio actualiza subtotal, IVA y total persistidos");
        Verificar(await Cambiar(lineaProducto, 3) && (await Detalle()).Lineas.Count == 2, "Reintentar cambio absoluto no duplica unidades");
        Verificar(!await Cambiar(lineaProducto, 0) && !await Cambiar(lineaProducto, 1.5m), "Cantidades inválidas rechazadas");
        Verificar(!await Cambiar(lineaServicio, 2) && !await Quitar(lineaServicio), "Servicio original no se modifica ni se elimina");
        Verificar(!await Cambiar(lineaProducto, 4, cliente.UsuarioId) && !await Quitar(lineaProducto, cliente.UsuarioId), "Cliente no edita ni quita productos");
        var ajenaLinea = (await Detalle(otraSede.FacturaId)).Lineas.Single(l => l.ProductoId == producto.ProductoId).DetalleFacturaId;
        Verificar(!await Cambiar(ajenaLinea, 2, facturaId: otraSede.FacturaId) && !await Quitar(ajenaLinea, facturaId: otraSede.FacturaId), "Encargado no cambia documentos de otra sucursal");
        Verificar(await Agregar(adicional.ProductoId, 2) && (await Detalle()).Lineas.Count == 3, "Agregar producto mantiene la misma orden");
        var lineaAdicional = (await Detalle()).Lineas.Single(l => l.ProductoId == adicional.ProductoId).DetalleFacturaId;
        Verificar(await Quitar(lineaAdicional) && await Quitar(lineaAdicional), "Quitar producto y repetir eliminación es seguro");
        Verificar((await Detalle()).Lineas.Count == 2 && (await Detalle()).Total == 1294.70m, "Eliminar actualiza totales sin perder servicio");
        var antes = await Detalle();
        await using (var falla = new WashLabsDbContext(Opciones().AddInterceptors(new FallarCabecera()).Options))
        {
            try { await new FacturaRepository(falla).CambiarCantidadAsync(proforma.FacturaId, lineaProducto, 5, encargado.UsuarioId, default); throw new InvalidOperationException("Fallo no provocado"); }
            catch (IOException) { }
        }
        Verificar((await Detalle()).Total == antes.Total && (await Detalle()).Lineas.Single(l => l.DetalleFacturaId == lineaProducto).Cantidad == 3,
            "Fallo al actualizar cabecera revierte cantidades y totales");
        var concurrentes = await Task.WhenAll(Cambiar(lineaProducto, 4), Cambiar(lineaProducto, 4));
        Verificar(concurrentes.Any(r => r) && (await Detalle()).Lineas.Single(l => l.DetalleFacturaId == lineaProducto).Cantidad == 4,
            "Ediciones concurrentes de cantidad no duplican unidades");
        Verificar(await Agregar(adicional.ProductoId, 1, admin.UsuarioId), "Administrador gestiona proforma autorizada");
        adicional.Activo = false; await db.SaveChangesAsync();
        Verificar(!await Agregar(adicional.ProductoId, 2), "No se agregan productos inactivos");
        await using (var ctx = Contexto())
            Verificar(await new OperacionRepository(ctx).CompletarCitaAsync(proforma.CitaId!.Value, encargado.UsuarioId, default), "Finalización conserva la edición y acredita WashPoints");
        var definitiva = await Detalle();
        Verificar(definitiva.TipoDocumento == "FACTURA" && definitiva.Estado == "Pendiente" && definitiva.Lineas.Count == 3
            && definitiva.Lineas.Single(l => l.DetalleFacturaId == lineaProducto).Cantidad == 4 && definitiva.MontoPagado == 0, "Factura definitiva conserva líneas, cantidades, descuento y ausencia de pago");
        Verificar(!await Cambiar(lineaProducto, 8) && !await Quitar(lineaProducto) && !await Agregar(producto.ProductoId, 1), "Factura definitiva es de solo lectura");
        await using (var ctx = Contexto())
            Verificar(await new OperacionRepository(ctx).CompletarCitaAsync(proforma.CitaId!.Value, encargado.UsuarioId, default)
                && await ctx.MovimientosWashPoints.CountAsync(m => m.CitaId == proforma.CitaId && m.TipoMovimiento == "ACUMULA") == 1,
                "Segunda finalización acredita exactamente un lavado");
        filtro = new FiltroFacturacionDTO { NumeroDocumento = proforma.FacturaId, TipoDocumento = "FACTURA" };
        Verificar((await Consultar(filtro)).Documentos.Single().Total == definitiva.Total && (await Consultar(filtro)).Resumen.Facturas == 1,
            "Factura convertida aparece una sola vez en historial y resumen");
        var catalogo = new ProductoService(new ProductoRepository(db));
        Verificar((await catalogo.ListarAsync(categoria.CategoriaProductoId, "edición " + marca)).Single().ProductoId == producto.ProductoId,
            "Catálogo buscable por nombre y categoría en SQL");
        Verificar((await catalogo.ListarAsync(categoria.CategoriaProductoId, "ausente " + marca)).Count == 0, "Catálogo sin coincidencias");
        Console.WriteLine($"Total facturador: {total} pruebas. Fixtures TST conservados exclusivamente en la base aislada.");
    }

    private sealed class FallarCabecera : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Factura>().Any(e => e.State == EntityState.Modified))
                throw new IOException("Fallo simulado de cabecera después de guardar el detalle.");
            return ValueTask.FromResult(result);
        }
    }
}
