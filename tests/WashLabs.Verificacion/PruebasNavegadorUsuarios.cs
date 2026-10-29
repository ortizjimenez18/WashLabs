using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

internal static class PruebasNavegadorUsuarios
{
    public static async Task EjecutarAsync(Uri servidor, string correo, string clave, long usuarioId, Action<bool, string> verificar)
    {
        var ejecutable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe");
        if (!File.Exists(ejecutable)) throw new InvalidOperationException("Las pruebas visuales requieren Microsoft Edge.");
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var puerto = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var inicio = new ProcessStartInfo(ejecutable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argumento in new[] { "--headless=new", "--disable-gpu", "--no-first-run", "--disable-background-networking",
            "--remote-debugging-port=" + puerto, "--user-data-dir=" + Path.Combine(Path.GetTempPath(), "WashLabsBrowser-" + Guid.NewGuid().ToString("N")), "about:blank" })
            inicio.ArgumentList.Add(argumento);
        using var proceso = Process.Start(inicio) ?? throw new InvalidOperationException("No se pudo iniciar Edge.");
        var salida = proceso.StandardOutput.ReadToEndAsync();
        var errores = proceso.StandardError.ReadToEndAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{puerto}"), Timeout = TimeSpan.FromSeconds(2) };
            string? paginas = null;
            for (var intento = 0; intento < 60 && paginas is null; intento++)
            {
                try { paginas = await http.GetStringAsync("/json/list"); }
                catch (HttpRequestException) { await Task.Delay(200); }
                catch (TaskCanceledException) { await Task.Delay(200); }
            }
            if (paginas is null) throw new InvalidOperationException("Edge no inició su puerto de verificación.");
            using var targets = JsonDocument.Parse(paginas);
            var target = targets.RootElement.EnumerateArray().First(t => t.GetProperty("type").GetString() == "page");
            using var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(target.GetProperty("webSocketDebuggerUrl").GetString()!), CancellationToken.None);
            var secuencia = 0;
            async Task<JsonElement> Comando(string metodo, object parametros)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var id = ++secuencia;
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { id, method = metodo, @params = parametros }));
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, timeout.Token);
                var buffer = new byte[32768];
                while (true)
                {
                    using var mensaje = new MemoryStream();
                    WebSocketReceiveResult recibido;
                    do
                    {
                        recibido = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                        if (recibido.MessageType == WebSocketMessageType.Close) throw new IOException("Edge cerró la sesión de verificación.");
                        mensaje.Write(buffer, 0, recibido.Count);
                    } while (!recibido.EndOfMessage);
                    using var json = JsonDocument.Parse(mensaje.ToArray());
                    if (!json.RootElement.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id) continue;
                    if (json.RootElement.TryGetProperty("error", out var error)) throw new InvalidOperationException(error.ToString());
                    return json.RootElement.GetProperty("result").Clone();
                }
            }
            async Task<JsonElement> Evaluar(string expression)
            {
                var resultado = await Comando("Runtime.evaluate", new { expression, awaitPromise = true, returnByValue = true });
                if (resultado.TryGetProperty("exceptionDetails", out _)) throw new InvalidOperationException("Falló la evaluación de la página de prueba.");
                return resultado.GetProperty("result").GetProperty("value").Clone();
            }
            async Task Navegar(string ruta, string selector)
            {
                await Comando("Page.navigate", new { url = new Uri(servidor, ruta).ToString() });
                for (var intento = 0; intento < 80; intento++)
                {
                    var listo = await Evaluar($"document.readyState === 'complete' && !!document.querySelector({JsonSerializer.Serialize(selector)})");
                    if (listo.GetBoolean()) return;
                    await Task.Delay(100);
                }
                throw new InvalidOperationException("La página no terminó de cargar: " + ruta);
            }
            await Comando("Page.enable", new { });
            await Navegar("/Cuenta/Entrar", "input[name=Correo]");
            var acceso = await Evaluar("(async () => { const input = document.querySelector('input[name=Correo]'); const form = input.form; input.value = "
                + JsonSerializer.Serialize(correo) + "; form.querySelector('input[name=Clave]').value = " + JsonSerializer.Serialize(clave)
                + "; const response = await fetch(form.action, {method:'POST', body:new FormData(form)}); return response.ok && !response.url.includes('/Cuenta/Entrar'); })()");
            verificar(acceso.GetBoolean(), "Navegador: sesión real del administrador");
            var carpeta = Path.GetFullPath("Evidencias/ActividadComprobacion");
            Directory.CreateDirectory(carpeta);
            foreach (var ancho in new[] { 1366, 390 })
            {
                await Comando("Emulation.setDeviceMetricsOverride", new { width = ancho, height = 1000, deviceScaleFactor = 1, mobile = ancho == 390 });
                foreach (var pagina in new[] { "ListaUsuarios", "DetalleUsuario" })
                {
                    await Navegar(pagina == "ListaUsuarios" ? "/Usuarios" : "/Usuarios/Detalle/" + usuarioId, pagina == "ListaUsuarios" ? ".users-table" : ".users-profile");
                    await Task.Delay(200);
                    verificar((await Evaluar("document.documentElement.scrollWidth <= window.innerWidth + 1")).GetBoolean(), $"Usuarios: {pagina} sin desbordamiento a {ancho}px");
                    if (pagina == "ListaUsuarios")
                        verificar((await Evaluar("document.querySelector('h1').textContent === 'Lista de Usuarios (María Paula Murillo Rosales)' && document.querySelectorAll('tr[data-user-id]').length >= 3")).GetBoolean(), "Navegador: título exacto y mínimo tres registros visibles");
                    var captura = await Comando("Page.captureScreenshot", new { format = "png", captureBeyondViewport = true });
                    await File.WriteAllBytesAsync(Path.Combine(carpeta, pagina + (ancho == 1366 ? "" : "-movil") + ".png"), Convert.FromBase64String(captura.GetProperty("data").GetString()!));
                }
            }
        }
        finally
        {
            if (!proceso.HasExited) proceso.Kill(entireProcessTree: true);
            await proceso.WaitForExitAsync();
            await Task.WhenAll(salida, errores);
        }
    }
}
