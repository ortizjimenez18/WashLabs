using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

internal sealed class NavegadorCatalogo : IAsyncDisposable
{
    private readonly ClientWebSocket socket = new();
    private Process? proceso;
    private int secuencia;

    public async Task IniciarAsync(string carpeta)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var puerto = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var inicio = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft/Edge/Application/msedge.exe"))
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "--headless=new", "--disable-gpu", "--no-first-run", "--disable-background-networking",
            "--remote-debugging-port=" + puerto, "--user-data-dir=" + Path.Combine(carpeta, "edge-cdp-profile"), "about:blank" }) inicio.ArgumentList.Add(arg);
        proceso = Process.Start(inicio)!;
        _ = proceso.StandardOutput.ReadToEndAsync();
        _ = proceso.StandardError.ReadToEndAsync();
        using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{puerto}"), Timeout = TimeSpan.FromSeconds(2) };
        string? paginas = null;
        for (var i = 0; i < 60 && paginas is null; i++)
        {
            try { paginas = await http.GetStringAsync("/json/list"); }
            catch (HttpRequestException) { await Task.Delay(200); }
            catch (TaskCanceledException) { await Task.Delay(200); }
        }
        using var targets = JsonDocument.Parse(paginas ?? throw new InvalidOperationException("Edge no inició."));
        var pagina = targets.RootElement.EnumerateArray().First(t => t.GetProperty("type").GetString() == "page");
        await socket.ConnectAsync(new Uri(pagina.GetProperty("webSocketDebuggerUrl").GetString()!), CancellationToken.None);
        await ComandoAsync("Page.enable", new { });
    }

    public async Task<JsonElement> ComandoAsync(string metodo, object parametros)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
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
                if (recibido.MessageType == WebSocketMessageType.Close) throw new IOException("Edge cerró la sesión.");
                mensaje.Write(buffer, 0, recibido.Count);
            } while (!recibido.EndOfMessage);
            using var json = JsonDocument.Parse(mensaje.ToArray());
            if (!json.RootElement.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id) continue;
            if (json.RootElement.TryGetProperty("error", out var error)) throw new InvalidOperationException(error.ToString());
            return json.RootElement.GetProperty("result").Clone();
        }
    }

    public async Task<JsonElement> EvaluarAsync(string expression)
    {
        var resultado = await ComandoAsync("Runtime.evaluate", new { expression, awaitPromise = true, returnByValue = true });
        if (resultado.TryGetProperty("exceptionDetails", out var error)) throw new InvalidOperationException(error.ToString());
        return resultado.GetProperty("result").GetProperty("value").Clone();
    }

    public async ValueTask DisposeAsync()
    {
        socket.Dispose();
        if (proceso is { HasExited: false })
        {
            proceso.Kill(true);
            await proceso.WaitForExitAsync();
        }
        proceso?.Dispose();
    }
}
