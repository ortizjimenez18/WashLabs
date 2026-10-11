# Persistencia de WashLabs

## Estado de preparación

- Rama: `feature/conexion-base-datos`.
- .NET 8; Microsoft.EntityFrameworkCore.SqlServer, Design y Tools 8.0.31 en Data.
- Design 8.0.31 también está en el proyecto web, utilizado como proyecto de inicio por las herramientas.
- SQL Server local: `localhost`, Windows Authentication, base `WashLabsDB` en línea.
- La inspección se limitó a consultas SELECT de metadatos: 35 tablas en dbo, incluida sysdiagrams.
- Scaffold ejecutado con EF Core 8.0.31: 34 entidades en `WashLabs.Data/Entities`
  y contexto en `WashLabs.Data/Context`. No se ejecutaron migraciones ni cambios SQL.
- Respaldo previo verificado: `artifacts/database-first-20261010-190643` (ignorado por Git).
  Incluye el contexto provisional, la configuración anterior y los archivos generados
  antes de normalizar los nombres de clases. Los respaldos de código usan extensión `.bak`.

## Configuración y dependencias

La aplicación registra `AddWashLabsServices`, que delega en `AddWashLabsData`.
Data registra `WashLabsDbContext` con alcance scoped y proveedor SQL Server, usando
compatibilidad SQL 150 (SQL Server 2019). Este ajuste configura las consultas de EF;
no altera la compatibilidad de la base.

El contexto ya contiene el modelo de la base existente. Los futuros
controllers consumirán interfaces de Services; sus implementaciones consumirán
interfaces de repositorios de Data; solo las implementaciones de repositorios
accederán al DbContext. Por indicación del usuario, las entidades de persistencia
están en Data/Entities; no se añadieron entidades a Models ni referencias nuevas
entre proyectos. No se generaron controllers, servicios de negocio o repositorios.
No hay llamadas a EnsureCreated, EnsureDeleted o Migrate al iniciar la aplicación.

La clave `ConnectionStrings:WashLabsDB` está configurada en User Secrets del
proyecto web para el usuario de Windows que realizó la preparación. ASP.NET Core
la carga en Development. No se guarda la cadena en appsettings ni en código.
Windows Authentication usa la identidad del proceso que ejecuta la aplicación.

Para configurar otro entorno local desde la raíz:

```powershell
dotnet user-secrets set 'ConnectionStrings:WashLabsDB' 'Server=localhost;Database=WashLabsDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15' --project WashLabs.csproj
```

Alternativamente, establecer `ConnectionStrings__WashLabsDB` en el entorno del
proceso. Esta variable tiene prioridad sobre User Secrets. TrustServerCertificate
se usa aquí para el certificado de la instancia local de desarrollo; en despliegues
con certificado válido se debe usar TrustServerCertificate=False.

## Database First: generación realizada y reproducción

Se ejecutó `dotnet ef dbcontext scaffold`, equivalente de `Scaffold-DbContext`,
con la herramienta temporal 8.0.31 y WashLabs como proyecto de inicio. Se seleccionaron
las 34 tablas de negocio y se excluyó dbo.sysdiagrams. El comando equivalente para
Package Manager Console se conserva abajo como referencia para una regeneración.

Antes de cualquier regeneración, respaldar contexto y entidades y revisar los cambios
pendientes. `-Force` reemplaza archivos existentes. La generación inicial se hizo
con nombres SQL exactos; después se singularizaron los tipos C# en español, sin cambiar
los nombres de propiedades escalares ni navegaciones. Por ejemplo, `Productos` se
mapea a `Producto` y `DetallesFactura` a `DetalleFactura`. Los DbSet conservan los
nombres SQL. Se fijaron explícitamente las tablas, esquema dbo y nombres de PK;
las FK, índices, tipos y restricciones conservan el mapeo obtenido de SQL Server.
Una regeneración sin repetir esta normalización produciría tipos plurales adicionales;
generar en una carpeta temporal y reconciliar los archivos antes de reemplazarlos.

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$tables = @(
    'dbo.BloqueosAgenda', 'dbo.CategoriasAsistente', 'dbo.CategoriasProducto',
    'dbo.Citas', 'dbo.ConfiguracionSistema', 'dbo.ConfiguracionWashPoints',
    'dbo.CriteriosProducto', 'dbo.DetalleFacturaPersonalizacion', 'dbo.DetallesFactura',
    'dbo.EstadosCita', 'dbo.EstadosFactura', 'dbo.Facturas',
    'dbo.HistorialEstadoCita', 'dbo.HorariosSucursal', 'dbo.InteraccionesAsistente',
    'dbo.MetodosPago', 'dbo.MovimientosWashPoints', 'dbo.NotificacionesCita',
    'dbo.OpcionesAsistente', 'dbo.OpcionesCriterioProducto', 'dbo.PagosFactura',
    'dbo.PreguntasAsistente', 'dbo.PreguntasReserva', 'dbo.Productos',
    'dbo.RecompensasWashPoints', 'dbo.RespuestasReserva', 'dbo.Roles',
    'dbo.Servicios', 'dbo.SesionesAsistente', 'dbo.Sucursales',
    'dbo.SucursalServicios', 'dbo.TarjetasWashPoints', 'dbo.Usuarios', 'dbo.Vehiculos'
)
Scaffold-DbContext 'Name=ConnectionStrings:WashLabsDB' Microsoft.EntityFrameworkCore.SqlServer -Project WashLabs.Data -StartupProject WashLabs -Context WashLabsDbContext -ContextDir Context -OutputDir Entities -Namespace WashLabs.Data.Entities -ContextNamespace WashLabs.Data.Context -Tables $tables -UseDatabaseNames -NoPluralize -NoOnConfiguring -Force
```

Las rutas son relativas al proyecto Data. El contexto queda en Data/Context y las
entidades en Data/Entities. `Name=` lee la configuración y `-NoOnConfiguring` evita
generar la cadena en el contexto. Se conservó el constructor que recibe
`DbContextOptions<WashLabsDbContext>` y el registro de dependencias existente.
Las relaciones se detallan en [relaciones-database-first.md](relaciones-database-first.md).
La verificación compara columnas, nulabilidad, PK, FK y navegaciones contra los
metadatos SQL y realiza una consulta LINQ `AsNoTracking` de solo lectura.
Compilar con `dotnet build WashLabs.sln`; no ejecutar Update-Database ni migraciones.

Referencia: [herramientas EF Core para Package Manager Console](https://learn.microsoft.com/en-us/ef/core/cli/powershell).
