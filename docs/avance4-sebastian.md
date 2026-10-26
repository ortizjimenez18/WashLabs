# Avance 4 — Sebastián Ortiz Jiménez

WashLabs · EvoGroup · Universidad Técnica Nacional · ISW-711 Programación en Ambiente Web.

## Alcance y versión de partida

Rama `feature/avance4-sebastian`, creada desde `975823c` de
`cambios-realizados-avance3`. La compilación inicial Release pasó sin advertencias.
El directorio estaba limpio; el respaldo anterior `stash@{0}` se conservó intacto.

Incluye únicamente sucursales, asignaciones de encargados, horarios, bloqueos y
sus scripts/pruebas. Productos y Servicios conservan su implementación. El logo,
la página principal y los estilos compartidos no se sustituyen.

Rutas protegidas para Administrador:

| Ruta | Función |
| --- | --- |
| `/Sucursales` | Listado y confirmación de guardado |
| `/Sucursales/Detalle/{id}` | Datos y encargados asignados |
| `/Sucursales/Crear` | Crear con selección múltiple de encargados disponibles |
| `/Sucursales/Editar/{id}` | Precargar, modificar y cambiar asignaciones |
| `/Horarios?sucursalId={id}` | Selector y listados de horarios/bloqueos |
| `/Horarios/CrearHorario?sucursalId={id}` | Horario para una fecha específica |
| `/Horarios/CrearBloqueo?sucursalId={id}` | Bloqueo con motivo y usuario creador real |
| `/Horarios/DetalleHorario/{id}` | Detalle de horario puntual o recurrente |
| `/Horarios/DetalleBloqueo/{id}` | Detalle del bloqueo y confirmación de guardado |

## Arquitectura y concurrencia

Razor → Controller → interfaz/implementación de Service →
interfaz/implementación de Repository → `WashLabsDbContext` → SQL Server.
ViewModels validan formatos; DTOs y Mappers separan las entidades de las vistas.
Los controladores no reciben DbContext ni entidades de EF.

Los servicios aplican las reglas de elegibilidad y sobreposición. El repositorio
consulta el estado real dentro de una transacción Serializable y entrega ese
estado al validador del servicio antes de guardar. `sp_getapplock`, con propietario
Transaction, serializa las asignaciones de encargados globalmente y la agenda por
sucursal. Las dos clases de agenda usan el mismo recurso de bloqueo por sucursal.
No depende de bloqueos en memoria, por lo que funciona con varias instancias web.
Los bloqueos se liberan al confirmar o revertir. Una espera agotada o un deadlock
produce un mensaje para reintentar; no deja guardados parciales.

Un encargado tiene una sola FK nullable a Sucursales. Al crear se admiten solo
encargados activos sin asignación; al editar se agregan los ya asignados a esa
sucursal. Un encargado inactivo ya asignado se puede conservar o retirar, pero
no agregar a una sucursal diferente. Las asignaciones no cambian roles, hashes ni
otros datos de autenticación. La validación de sesión existente invalida el acceso
del encargado cuando cambia su sucursal.

Se aplica `inicioA < finB && inicioB < finA`: los intervalos contiguos se permiten.
Se consultan horarios activos cuya vigencia incluye la fecha y cuyo día coincide
(lunes=1, domingo=7), bloqueos parciales y bloqueos de día completo. Un bloqueo
tampoco puede perjudicar una cita existente no cancelada. Las fechas se validan
en hora de Costa Rica, incluyendo el día actual, y las horas se ingresan con
precisión de minutos para evitar redondeos en columnas `time(0)`.

Todos los POST llevan antifalsificación. Se validan los identificadores de la
ruta y la sesión, los registros inexistentes devuelven 404 y los formularios
conservan sus valores al mostrar errores en español.

## Esquema real y respaldo

Se verificaron columnas, CHECKs y FKs directamente en WashLabsDB. No se agregan
columnas ni tablas ni se regeneran entidades Database First.

`HorariosSucursal` representa recurrencia por día semanal con vigencia. Un horario
puntual se representa estableciendo `FechaVigenciaDesde = FechaVigenciaHasta = Fecha`.
El cálculo de disponibilidad existente del Avance 3 ya entiende esta representación.
Los registros recurrentes y las citas existentes conservan sus datos y relaciones.

Dos restricciones originales requerían ajustes no destructivos:

1. `CK_Usuarios_EncargadoSucursal` impedía que un encargado estuviera sin sucursal.
   Se retira este CHECK; se conserva la FK y la asignación única por columna.
2. `CK_HorariosSucursal_Horas` exigía ocho horas para cualquier horario.
   Se permite una duración menor únicamente en una fecha puntual. Los recurrentes
   mantienen el mínimo de ocho horas. `HoraInicio < HoraFin` continúa obligatorio.

El script usa transacción, `XACT_ABORT`, validación de estructura y `WITH CHECK`.
Exige un respaldo FULL COPY_ONLY con CHECKSUM reciente y ejecuta RESTORE VERIFYONLY
antes de modificar restricciones. No se destruyen ni se transforman datos.
La copia queda en el directorio de respaldos de SQL Server, fuera del repositorio.

Se verificaron los respaldos `WashLabs_Avance4_84D9E870A67F42769F384484DF277B6B.bak`
y `WashLabs_Avance4_537461E611A046259F16EFE4D11A01AB.bak` durante la implementación.
En otra instalación debe generarse su propio respaldo antes de ejecutar los scripts.

## Ejecución reproducible

Requiere el esquema y datos del Avance 3, SDK .NET 8 y una conexión configurada en
User Secrets (`ConnectionStrings:WashLabsDB`) o en la variable
`ConnectionStrings__WashLabsDB`. No guardar la conexión en archivos versionados.
Ejecutar desde la raíz del repositorio, con permisos de SQL para respaldo y DDL:

```powershell
./scripts/Avance4-Sebastian.ps1 -Accion Respaldo
./scripts/Avance4-Sebastian.ps1 -Accion Esquema
./scripts/Avance4-Sebastian.ps1 -Accion Datos
./scripts/Avance4-Sebastian.ps1 -Accion Verificar
dotnet build WashLabs.sln --configuration Release
dotnet run --project tests/WashLabs.Avance4 --configuration Release
dotnet run --project tests/WashLabs.Verificacion --configuration Release -- --solo-unitarias
dotnet run --project WashLabs.csproj --configuration Release --launch-profile https
```

Los tres scripts también se pueden abrir en SSMS seleccionando WashLabsDB, en el
orden indicado. Son compatibles con SQL Server 2019. No requieren GO ni SQLCMD.

| Script | Contenido |
| --- | --- |
| `Database/Avance4/Sebastian/01_Esquema.sql` | Respaldo verificado, ajustes de CHECK y validación |
| `Database/Avance4/Sebastian/02_DatosPrueba.sql` | Cuatro sucursales, ocho encargados, cuatro horarios y cuatro bloqueos DEMO |
| `Database/Avance4/Sebastian/03_Verificacion.sql` | Solo lectura: estructura, FKs, registros, asignaciones y conflictos |

Los registros DEMO se identifican por `A4 DEMO` y correos `@example.invalid`.
Dos sucursales tienen dos encargados; quedan dos encargados disponibles sin
asignar. Los horarios y bloqueos no se superponen. Ejecutar Datos dos veces no
duplica registros ni cambia asignaciones hechas después por el evaluador.
Las fechas se fijan catorce días después de la primera instalación. Repetir no
desplaza una agenda ya instalada; para probar fechas posteriores se usa Crear.
Las cuentas DEMO nuevas tienen un marcador de hash sin acceso utilizable; no se
publican contraseñas. Para evaluar los módulos basta una cuenta Administrador
existente. No se modifica su contraseña ni la de otras cuentas.

## Validación y evidencias

Las pruebas usan SQL Server real y registros temporales identificados con un GUID.
El finally elimina solo los registros de esa ejecución. Los datos DEMO permanecen.
Se comprueban creación/edición, precarga, múltiples encargados, elegibilidad,
asignación concurrente, duplicados, formatos y campos obligatorios. En agenda se
prueban fechas pasadas/actuales, horas invertidas, los cuatro cruces, intervalos
contiguos, separación por sucursal, recurrencias, día completo y concurrencia.

Las pruebas HTTP inician su propio servidor en un puerto libre, firman una cookie
efímera con las claves locales de desarrollo y el sello real de sesión, y pasan
por la autorización normal de la aplicación. No cambian credenciales ni imprimen
cookies. Se comprueban permisos, CSRF, formularios, mensajes visibles, listados,
detalles y regresión de Productos, Reservas, Facturación y Usuarios.

Resultados y registro de actividades para Trello:

- Inspección de rama, Avance 3, esquema real y compilación inicial: realizada.
- Respaldo FULL COPY_ONLY/CHECKSUM y RESTORE VERIFYONLY: verificados.
- Sucursales: 14 pruebas iniciales; commit `9a29802`.
- Horarios/bloqueos: 37 pruebas acumuladas; commit `ff65dbe`.
- Verificación SQL: Datos ejecutado dos veces; conteos 4/8/4/4 y cero conflictos.
- Validación final: 83 pruebas del Avance 4 y 8 unitarias existentes del Avance 3
  aprobadas; Release con cero errores y cero advertencias. Resultados en
  `artifacts/avance4-pruebas.txt`, `artifacts/avance3-regresion.txt` y
  `artifacts/avance4-build.txt`.
- Adjuntar a Trello: estos registros y capturas de sucursales, precarga de
  encargados, selector de agenda, confirmaciones y un conflicto rechazado.
  No se ha publicado contenido en un tablero externo.

## Inventario de archivos y límites de integración

Archivos nuevos: Controllers/SucursalesController.cs y HorariosController.cs;
Views/Sucursales/*, Views/Horarios/*, Views/Shared/_Avance4Exito.cshtml;
interfaces/repositorios `SucursalAdministracion` y `AgendaAdministracion` en Data;
interfaces/servicios/mappers homónimos en Services; DTOs de administración y
ViewModels SucursalFormulario/AgendaFormulario en Models; wwwroot/css/avance4.css;
Database/Avance4/Sebastian/*.sql; scripts/Avance4-Sebastian.ps1;
tests/WashLabs.Avance4/* y este documento.

Compartidos modificados: registros DI de Data y Services, dos enlaces en
Views/Shared/_Layout.cshtml y mensajes de binding en español en Program.cs.
No se cambian Productos ni Servicios en estos tres commits.

Al revisar se comprobó que `origin/main` seguía en `ab84c4e` y todavía no incluía
los seis commits previos del Avance 3. Por eso el PR hacia main incluye esa base
heredada. Los cambios propios se pueden revisar con
`git diff 975823c..feature/avance4-sebastian`.
No se hace merge automático. Paula debe comenzar cuando la rama completa esté
revisada e integrada en main. En cada instalación hay que aplicar los scripts;
el push de código no actualiza otras bases de datos.
