param([ValidateSet('Respaldo','Esquema','LimpiarDatos','Verificar')][string]$Accion='Verificar')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$cadena=$env:ConnectionStrings__WashLabsDB
if (-not $cadena) {
    $archivo=Join-Path $env:APPDATA 'Microsoft\UserSecrets\84cd4722-f2f7-45ff-80bb-b95a43666266\secrets.json'
    $config=Get-Content -LiteralPath $archivo -Raw -Encoding UTF8 | ConvertFrom-Json
    $cadena=$config.'ConnectionStrings:WashLabsDB'
    if (-not $cadena) { $cadena=$config.ConnectionStrings.WashLabsDB }
}
if (-not $cadena) { throw 'Configure ConnectionStrings:WashLabsDB en User Secrets o ConnectionStrings__WashLabsDB.' }
$conexion=New-Object System.Data.SqlClient.SqlConnection($cadena)
try {
    $conexion.Open()
    if ($conexion.Database -ne 'WashLabsDB') { throw 'Este ejecutor admite únicamente WashLabsDB.' }
    $cmd=$conexion.CreateCommand()
    $cmd.CommandTimeout=180
    if ($Accion -eq 'Respaldo') {
        $cmd.CommandText=@'
DECLARE @directorio nvarchar(4000)=CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'));
IF @directorio IS NULL THROW 51000,'Configure una ruta de respaldo en SQL Server.',1;
DECLARE @archivo nvarchar(4000)=@directorio+CASE WHEN RIGHT(@directorio,1)=N'\' THEN N'' ELSE N'\' END+N'WashLabs_Avance4_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE WashLabsDB TO DISK=@archivo WITH COPY_ONLY,CHECKSUM;
RESTORE VERIFYONLY FROM DISK=@archivo WITH CHECKSUM;
SELECT @archivo AS RespaldoVerificado;
'@
    } else {
        $nombre=@{Esquema='01_Esquema.sql';LimpiarDatos='02_LimpiezaDatosPrueba.sql';Verificar='03_Verificacion.sql'}[$Accion]
        $cmd.CommandText=Get-Content -LiteralPath (Join-Path $repo "Database/Avance4/Sebastian/$nombre") -Raw -Encoding UTF8
    }
    $adaptador=New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $datos=New-Object System.Data.DataSet
    [void]$adaptador.Fill($datos)
    foreach ($tabla in $datos.Tables) { $tabla | Format-Table -AutoSize | Out-String -Width 220 | Write-Output }
    Write-Output "Completado: $Accion"
} finally { $conexion.Dispose() }
