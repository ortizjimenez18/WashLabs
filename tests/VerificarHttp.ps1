param([string]$BaseUrl = 'http://127.0.0.1:5189')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $false
$client = New-Object System.Net.Http.HttpClient($handler)
try {
    $cases = @(
        @('/', 200), @('/Productos', 200), @('/Productos/Detalle/2147483647', 404),
        @('/Asistente', 200), @('/Asistente/Categorias', 200), @('/Asistente/Consulta/2147483647', 404),
        @('/Cuenta/Entrar', 200), @('/Cuenta/Registrarse', 200),
        @('/Productos/Mantenimiento', 302), @('/Productos/DetalleMantenimiento/1', 302),
        @('/Facturacion', 302), @('/Facturacion/Detalle/1', 302),
        @('/Reservas', 302), @('/Reservas/Detalle/1', 302), @('/Reservas/Crear', 302),
        @('/Vehiculos', 302), @('/Vehiculos/Crear', 302), @('/WashPoints', 302)
    )
    foreach ($case in $cases) {
        $response = $client.GetAsync($BaseUrl.TrimEnd('/') + $case[0]).GetAwaiter().GetResult()
        try {
            $status = [int]$response.StatusCode
            if ($status -ne $case[1]) { throw "FALLÓ: $($case[0]), HTTP $status, esperado $($case[1])" }
            if ($status -eq 302 -and $response.Headers.Location.ToString() -notmatch '/Cuenta/Entrar') {
                throw "FALLÓ: $($case[0]) no redirige al inicio de sesión."
            }
            Write-Output "APROBADA: $($case[0]) HTTP $status"
        } finally { $response.Dispose() }
    }
    Write-Output "Total: $($cases.Count) rutas HTTP. Sin iniciar sesión ni modificar SQL."
} finally { $client.Dispose(); $handler.Dispose() }
