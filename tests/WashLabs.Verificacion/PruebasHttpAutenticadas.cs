using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Entities;

internal static class PruebasHttpAutenticadas
{
    public static async Task EjecutarAsync()
    {
        var cadena = Environment.GetEnvironmentVariable("WASHLABS_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Configura la conexión aislada WASHLABS_TEST_CONNECTION.");
        if (new SqlConnectionStringBuilder(cadena).InitialCatalog != "WashLabs_Pruebas_Avance3")
            throw new InvalidOperationException("Las sesiones de prueba solo usan WashLabs_Pruebas_Avance3.");
        await using var db = new WashLabsDbContext(new DbContextOptionsBuilder<WashLabsDbContext>().UseSqlServer(cadena).Options);
        var usuarios = await db.Usuarios.Include(u => u.Rol).Where(u => u.Activo).ToListAsync();
        if (usuarios.Count != 3 || usuarios.Any(u => u.Correo != u.Rol.Nombre.ToLowerInvariant() + "@example.invalid"))
            throw new InvalidOperationException("Solo se admiten las tres cuentas sintéticas creadas por el preparador.");
        var clave = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
        foreach (var usuario in usuarios) usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, clave);
        await db.SaveChangesAsync();
        // La clave aleatoria permanece en memoria; no se imprime ni se guarda en archivos.
        var inicio = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Directory.GetCurrentDirectory() };
        inicio.ArgumentList.Add(Path.GetFullPath(Environment.GetEnvironmentVariable("WASHLABS_TEST_APP_PATH") ?? "bin/Debug/net8.0/WashLabs.dll"));
        inicio.ArgumentList.Add("--urls"); inicio.ArgumentList.Add("http://127.0.0.1:5291");
        inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        inicio.Environment["ConnectionStrings__WashLabsDB"] = cadena;
        using var proceso = Process.Start(inicio) ?? throw new InvalidOperationException("No se pudo iniciar WashLabs.");
        var salida = proceso.StandardOutput.ReadToEndAsync();
        var errores = proceso.StandardError.ReadToEndAsync();
        var total = 0;
        long? reservaHttp = null;
        long? proformaHttp = null;
        void Verificar(bool condicion, string nombre)
        {
            if (!condicion) throw new InvalidOperationException("FALLÓ: " + nombre);
            total++;
            Console.WriteLine("APROBADA: " + nombre);
        }
        try
        {
            using var comprobacion = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5291"), Timeout = TimeSpan.FromSeconds(2) };
            var disponible = false;
            for (var intento = 0; intento < 40 && !disponible; intento++)
            {
                if (proceso.HasExited) throw new InvalidOperationException("El servidor de pruebas terminó antes de iniciar.");
                try { using var respuesta = await comprobacion.GetAsync("/Cuenta/Entrar"); disponible = respuesta.StatusCode == HttpStatusCode.OK; }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                if (!disponible) await Task.Delay(500);
            }
            Verificar(disponible, "Servidor HTTP sobre base aislada");
            static string Token(string html)
            {
                var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
                if (!token.Success) throw new InvalidOperationException("No se encontró el token antifalsificación.");
                return WebUtility.HtmlDecode(token.Groups[1].Value);
            }
            foreach (var usuario in usuarios.OrderBy(u => u.Rol.Nombre == "Cliente" ? 0 : u.Rol.Nombre == "Encargado" ? 1 : 2))
            {
                using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
                using var cliente = new HttpClient(handler) { BaseAddress = comprobacion.BaseAddress };
                var entrada = await cliente.GetStringAsync("/Cuenta/Entrar");
                using var acceso = await cliente.PostAsync("/Cuenta/Entrar", new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["Correo"] = usuario.Correo, ["Clave"] = clave, ["__RequestVerificationToken"] = Token(entrada) }));
                Verificar(acceso.StatusCode == HttpStatusCode.Redirect && handler.CookieContainer.GetCookies(cliente.BaseAddress!).Cast<Cookie>()
                    .Any(c => c.Name == "WashLabs.Session"), $"Login y cookie del rol {usuario.Rol.Nombre}");
                foreach (var ruta in new[] { "/Reservas", "/Facturacion", "/Vehiculos", "/WashPoints" })
                {
                    using var respuesta = await cliente.GetAsync(ruta);
                    Verificar(respuesta.StatusCode == HttpStatusCode.OK, $"{usuario.Rol.Nombre}: {ruta} accesible");
                }
                using var mantenimiento = await cliente.GetAsync("/Productos/Mantenimiento");
                Verificar(usuario.Rol.Nombre == "Cliente" ? mantenimiento.StatusCode == HttpStatusCode.Redirect
                    && mantenimiento.Headers.Location!.ToString().Contains("/Cuenta/SinPermiso") : mantenimiento.StatusCode == HttpStatusCode.OK,
                    $"{usuario.Rol.Nombre}: autorización de mantenimiento");
                var html = await cliente.GetStringAsync("/Reservas");
                var documento = await db.Facturas.AsNoTracking().Where(f => usuario.Rol.Nombre == "Administrador"
                    || usuario.Rol.Nombre == "Cliente" && f.ClienteId == usuario.UsuarioId
                    || usuario.Rol.Nombre == "Encargado" && f.SucursalId == usuario.SucursalId)
                    .OrderByDescending(f => f.FacturaId).FirstAsync();
                var detalleDocumento = await cliente.GetStringAsync("/Facturacion/Detalle/" + documento.FacturaId);
                Verificar(detalleDocumento.Contains(documento.TipoDocumento) && detalleDocumento.Contains("IVA"),
                    $"{usuario.Rol.Nombre}: detalle Razor con tipo e IVA");
                Verificar(detalleDocumento.Contains("billing-logo") && detalleDocumento.Contains("Imprimir") && detalleDocumento.Contains("Identificaci"),
                    $"{usuario.Rol.Nombre}: encabezado comercial e impresión disponibles");
                var filtrado = await cliente.GetStringAsync($"/Facturacion?NumeroDocumento={documento.FacturaId}&TipoDocumento={documento.TipoDocumento}&EstadoFacturaId={documento.EstadoFacturaId}&Orden=mayor");
                Verificar(Regex.Matches(filtrado, "data-document-id=").Count == 1 && filtrado.Contains($"data-document-id=\"{documento.FacturaId}\""),
                    $"{usuario.Rol.Nombre}: filtros GET combinados localizan el documento");
                var cabecera = Regex.Match(filtrado, "<thead>(.*?)</thead>", RegexOptions.Singleline).Value;
                Verificar(Regex.Matches(cabecera, "<th ").Count == 4, "Historial conserva cuatro campos principales");
                var sinDocumentos = await cliente.GetStringAsync("/Facturacion?NumeroDocumento=" + long.MaxValue);
                Verificar(sinDocumentos.Contains("No encontramos documentos") && sinDocumentos.Contains("Limpiar filtros"), "Sin resultados ofrece limpiar filtros");
                var fechasInvalidas = await cliente.GetStringAsync("/Facturacion?FechaInicial=2030-01-01&FechaFinal=2020-01-01");
                Verificar(WebUtility.HtmlDecode(fechasInvalidas).Contains("La fecha inicial no puede ser posterior"), "HTTP valida rango de fechas");
                var detalleReserva = await cliente.GetStringAsync("/Reservas/Detalle/" + documento.CitaId);
                Verificar(detalleReserva.Contains("/Facturacion/Detalle/" + documento.FacturaId),
                    $"{usuario.Rol.Nombre}: reserva enlaza su documento");
                if (usuario.Rol.Nombre == "Cliente")
                {
                    using var completar = await cliente.PostAsync("/Reservas/Completar/" + documento.CitaId,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalleReserva) }));
                    Verificar(completar.StatusCode == HttpStatusCode.Redirect && completar.Headers.Location!.ToString().Contains("/Cuenta/SinPermiso"),
                        "HTTP: cliente no puede completar citas");
                    using var agregar = await cliente.PostAsync("/Facturacion/AgregarProducto/" + documento.FacturaId,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalleReserva), ["productoId"] = "1", ["cantidad"] = "1" }));
                    Verificar(agregar.StatusCode == HttpStatusCode.Redirect && agregar.Headers.Location!.ToString().Contains("/Cuenta/SinPermiso"),
                        "HTTP: cliente no puede agregar productos");
                    using var editar = await cliente.PostAsync("/Facturacion/CambiarCantidad/" + documento.FacturaId,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalleReserva), ["detalleId"] = "1", ["cantidad"] = "2" }));
                    Verificar(editar.StatusCode == HttpStatusCode.Redirect && editar.Headers.Location!.ToString().Contains("/Cuenta/SinPermiso"), "HTTP: cliente no cambia cantidades");
                    using var quitar = await cliente.PostAsync("/Facturacion/QuitarProducto/" + documento.FacturaId,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalleReserva), ["detalleId"] = "1" }));
                    Verificar(quitar.StatusCode == HttpStatusCode.Redirect && quitar.Headers.Location!.ToString().Contains("/Cuenta/SinPermiso"), "HTTP: cliente no quita productos");
                    var servicio = await db.SucursalServicios.AsNoTracking().Where(s => s.Servicio.Nombre.StartsWith("TST integración "))
                        .OrderByDescending(s => s.ServicioId).FirstAsync();
                    var referencia = await db.Citas.AsNoTracking().Where(c => c.ServicioId == servicio.ServicioId && c.SucursalId == servicio.SucursalId)
                        .OrderBy(c => c.Fecha).FirstAsync();
                    var reservas = new WashLabs.Services.Implementations.ReservaService(new WashLabs.Data.Repositories.ReservaRepository(db));
                    var fecha = referencia.Fecha.AddDays(10);
                    while (fecha <= referencia.Fecha.AddDays(20)
                        && !(await reservas.HorariosAsync(usuario.UsuarioId, servicio.ServicioId, fecha, default, servicio.SucursalId)).Contains(new TimeOnly(9, 0)))
                        fecha = fecha.AddDays(1);
                    var formulario = await cliente.GetStringAsync($"/Reservas/Crear?sucursalId={servicio.SucursalId}&servicioId={servicio.ServicioId}");
                    var campos = new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(formulario),
                        ["Reserva.SucursalId"] = servicio.SucursalId.ToString(), ["Reserva.ClienteId"] = usuario.UsuarioId.ToString(),
                        ["Reserva.VehiculoId"] = referencia.VehiculoId.ToString(), ["Reserva.ServicioId"] = servicio.ServicioId.ToString(),
                        ["Reserva.Fecha"] = fecha.ToString("yyyy-MM-dd"), ["Reserva.HoraInicio"] = "09:00", ["Reserva.Observaciones"] = "TST HTTP integración" };
                    var preguntas = await db.PreguntasReserva.Where(p => p.Activa).OrderBy(p => p.Orden).ToListAsync();
                    for (var i = 0; i < preguntas.Count; i++)
                    {
                        campos[$"Reserva.Respuestas[{i}].PreguntaId"] = preguntas[i].PreguntaReservaId.ToString();
                        campos[$"Reserva.Respuestas[{i}].Respuesta"] = "Prueba HTTP";
                    }
                    using var crear = await cliente.PostAsync("/Reservas/Crear", new FormUrlEncodedContent(campos));
                    Verificar(crear.StatusCode == HttpStatusCode.Redirect && crear.Headers.Location!.ToString().Contains("/Reservas/Detalle/"),
                        "HTTP: cliente crea reserva con formulario MVC");
                    reservaHttp = await db.Citas.Where(c => c.VehiculoId == referencia.VehiculoId && c.Fecha == fecha && c.HoraInicio == new TimeOnly(9, 0))
                        .Select(c => c.CitaId).SingleAsync();
                    proformaHttp = await db.Facturas.Where(f => f.CitaId == reservaHttp && f.TipoDocumento == "PROFORMA").Select(f => f.FacturaId).SingleAsync();
                    Verificar(proformaHttp.HasValue && await db.DetallesFactura.CountAsync(d => d.FacturaId == proformaHttp && d.ServicioId == servicio.ServicioId) == 1,
                        "HTTP: reserva persiste proforma con su servicio");
                    using var repetir = await cliente.PostAsync("/Reservas/Crear", new FormUrlEncodedContent(campos));
                    Verificar(repetir.StatusCode == HttpStatusCode.Redirect && await db.Facturas.CountAsync(f => f.CitaId == reservaHttp) == 1,
                        "HTTP: doble envío no duplica proforma");
                }
                if (usuario.Rol.Nombre == "Encargado")
                {
                    var proformaHtml = await cliente.GetStringAsync("/Facturacion/Detalle/" + proformaHttp);
                    Verificar(proformaHtml.Contains("Agregar producto"), "HTTP: encargado dispone del formulario de productos");
                    var productos = await db.Productos.AsNoTracking().Where(p => p.Nombre.StartsWith("TST producto ")).OrderByDescending(p => p.ProductoId).Take(2).ToListAsync();
                    var catalogoHtml = await cliente.GetStringAsync($"/Facturacion/Detalle/{proformaHttp}?mostrarProductos=true&busquedaProducto={Uri.EscapeDataString(productos[0].Nombre)}&categoriaProductoId={productos[0].CategoriaProductoId}");
                    Verificar(catalogoHtml.Contains($"value=\"{productos[0].ProductoId}\"") && !catalogoHtml.Contains($"value=\"{productos[1].ProductoId}\">"),
                        "HTTP: catálogo filtra productos reales por nombre y categoría");
                    foreach (var producto in productos)
                    {
                        using var incluir = await cliente.PostAsync("/Facturacion/AgregarProducto/" + proformaHttp,
                            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(proformaHtml),
                                ["productoId"] = producto.ProductoId.ToString(), ["cantidad"] = "1" }));
                        Verificar(incluir.StatusCode == HttpStatusCode.Redirect, "HTTP: producto agregado por encargado");
                    }
                    var lineaEditable = await db.DetallesFactura.AsNoTracking().SingleAsync(d => d.FacturaId == proformaHttp && d.ProductoId == productos[0].ProductoId);
                    var lineaQuitable = await db.DetallesFactura.AsNoTracking().SingleAsync(d => d.FacturaId == proformaHttp && d.ProductoId == productos[1].ProductoId);
                    var volverUrl = "/Facturacion?NumeroDocumento=" + proformaHttp;
                    using var cambiar = await cliente.PostAsync("/Facturacion/CambiarCantidad/" + proformaHttp,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(proformaHtml),
                            ["detalleId"] = lineaEditable.DetalleFacturaId.ToString(), ["cantidad"] = "3", ["volverUrl"] = volverUrl }));
                    Verificar(cambiar.StatusCode == HttpStatusCode.Redirect
                        && await db.DetallesFactura.AnyAsync(d => d.DetalleFacturaId == lineaEditable.DetalleFacturaId && d.Cantidad == 3), "HTTP: cambiar cantidad persiste en SQL");
                    var actualizadaHtml = await cliente.GetStringAsync(cambiar.Headers.Location!.ToString());
                    Verificar(WebUtility.HtmlDecode(actualizadaHtml).Contains("Cantidad actualizada") && actualizadaHtml.Contains("href=\"/Facturacion?NumeroDocumento="),
                        "HTTP: proforma se actualiza y conserva retorno al historial filtrado");
                    using var retirar = await cliente.PostAsync("/Facturacion/QuitarProducto/" + proformaHttp,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(proformaHtml), ["detalleId"] = lineaQuitable.DetalleFacturaId.ToString() }));
                    Verificar(retirar.StatusCode == HttpStatusCode.Redirect && !await db.DetallesFactura.AnyAsync(d => d.DetalleFacturaId == lineaQuitable.DetalleFacturaId)
                        && await db.DetallesFactura.CountAsync(d => d.FacturaId == proformaHttp) == 2, "HTTP: quitar producto conserva servicio y actualiza orden");
                    using var restaurar = await cliente.PostAsync("/Facturacion/AgregarProducto/" + proformaHttp,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(proformaHtml), ["productoId"] = productos[1].ProductoId.ToString(), ["cantidad"] = "1" }));
                    Verificar(restaurar.StatusCode == HttpStatusCode.Redirect && await db.DetallesFactura.CountAsync(d => d.FacturaId == proformaHttp) == 3, "HTTP: varios productos pertenecen al mismo documento");
                    if (Environment.GetEnvironmentVariable("WASHLABS_VISUAL_TESTS") == "1")
                        await PruebasNavegadorFacturacion.EjecutarAsync(cliente.BaseAddress!, usuario.Correo, clave, proformaHttp!.Value, Verificar);
                    var reservaHtml = await cliente.GetStringAsync("/Reservas/Detalle/" + reservaHttp);
                    using var finalizar = await cliente.PostAsync("/Facturacion/CompletarCita/" + proformaHttp,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(reservaHtml) }));
                    Verificar(finalizar.StatusCode == HttpStatusCode.Redirect
                        && await db.Facturas.AnyAsync(f => f.FacturaId == proformaHttp && f.TipoDocumento == "FACTURA" && f.EstadoFactura.Nombre == "Pendiente")
                        && await db.DetallesFactura.CountAsync(d => d.FacturaId == proformaHttp) == 3
                        && await db.MovimientosWashPoints.CountAsync(m => m.CitaId == reservaHttp && m.TipoMovimiento == "ACUMULA") == 1,
                        "HTTP: completar conserva tres líneas, emite factura y acredita lavado");
                    using var repetir = await cliente.PostAsync("/Reservas/Completar/" + reservaHttp,
                        new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(reservaHtml) }));
                    Verificar(repetir.StatusCode == HttpStatusCode.Redirect
                        && await db.MovimientosWashPoints.CountAsync(m => m.CitaId == reservaHttp && m.TipoMovimiento == "ACUMULA") == 1,
                        "HTTP: doble finalización mantiene un lavado");
                }
                using var sinToken = await cliente.PostAsync("/Cuenta/Salir", new FormUrlEncodedContent(new Dictionary<string, string>()));
                Verificar(sinToken.StatusCode == HttpStatusCode.BadRequest, "Logout sin token rechazado");
                using var salir = await cliente.PostAsync("/Cuenta/Salir", new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["__RequestVerificationToken"] = Token(html) }));
                Verificar(salir.StatusCode == HttpStatusCode.Redirect, $"{usuario.Rol.Nombre}: cierre de sesión");
                using var privado = await cliente.GetAsync("/Reservas");
                Verificar(privado.StatusCode == HttpStatusCode.Redirect && privado.Headers.Location!.ToString().Contains("/Cuenta/Entrar"),
                    "Ruta privada exige autenticación después de salir");
            }
            Console.WriteLine($"Total: {total} verificaciones HTTP autenticadas. Reserva, factura y productos de prueba conservados en la base aislada; hashes de tres cuentas sintéticas renovados.");
        }
        finally
        {
            if (!proceso.HasExited) proceso.Kill(entireProcessTree: true);
            await proceso.WaitForExitAsync();
            await Task.WhenAll(salida, errores);
        }
    }
}
