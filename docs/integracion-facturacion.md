# Integración de reservas, facturación y WashPoints

WashLabs · EvoGroup · Rama `cambios-realizados-avance3` · 13 de octubre de 2026.

Este informe conserva la validación inicial de la integración. La ampliación posterior
del historial, la edición de productos y la impresión está en
[reestructuración del facturador](facturador-avance3.md).

## A. Estado de la implementación

Ya existían MVC .NET 8, servicios, repositorios LINQ, contexto Database First,
validaciones de disponibilidad y propiedad del vehículo, consultas de documentos
con alcance por rol y una transacción de acreditación de WashPoints. También
existían las columnas calculadas de detalle y los índices únicos por cita,
vehículo y acumulación. No existían triggers, creación automática de proformas,
conversión al completar ni incorporación de productos desde la interfaz.

Se implementaron:

- Reserva y proforma con servicio en una transacción Serializable.
- Precio de sucursal, con tarifa base como alternativa; precio y descripción
  conservados en el detalle. La descripción respeta el límite SQL de 300 caracteres.
- Detección de solicitudes repetidas por cliente, vehículo, sucursal, servicio,
  fecha y hora; rechazo de conflictos de agenda y del vehículo entre sucursales.
- Conversión del mismo documento a FACTURA/Pendiente al completar la cita.
- Productos adicionales desde el catálogo, exclusivamente en proformas activas
  por encargado de la sucursal o administrador.
- Totales de cabecera calculados desde los importes persistidos del detalle.
- Cancelación autorizada con conservación de la proforma Anulada.
- Tipos, vínculos entre documentos y reservas, IVA y total por línea en Razor.
- Pruebas de concurrencia, rollback, permisos, precios y flujo HTTP completo.

Se corrigió el tratamiento de interbloqueos envueltos por EF Core y se obtuvo el
bloqueo exclusivo del vehículo antes de validar su propiedad, evitando la
conversión concurrente de bloqueos compartidos. Los conflictos SQL controlados
de creación, productos y finalización no confirman operaciones parciales y permiten
reintentar. Un error inesperado revierte la transacción y se propaga al manejador
existente de errores de la aplicación.

Pendiente: inspección visual humana en Visual Studio/navegador. No se implementaron
pagos, inventario, redención ni reinicio de ciclos. Las citas históricas sin proforma
no generan documentos automáticamente; completar una de ellas devuelve fallo sin
cambiar la cita ni acreditar puntos. No se modificaron facturas históricas.

La incorporación mínima de productos acepta unidades enteras positivas, con el
límite compatible con decimal(10,2). Repetir el mismo producto con la misma cantidad
en el mismo documento conserva la línea existente; una cantidad distinta agrega
otra línea. No existe edición o eliminación de líneas dentro de este alcance.

## B. Archivos modificados por esta integración

La comparación utilizada es contra el índice inicial de Git, que ya contenía
cambios del equipo. Esos cambios permanecen preparados; esta integración quedó
sin preparar ni confirmar.

| Ruta | Motivo, modificación y resultado |
| --- | --- |
| `Controllers/ReservasController.cs` | Confirmaciones de creación/finalización y POST de cancelación con identidad real y antifalsificación. |
| `Controllers/FacturacionController.cs` | Catálogo en el detalle y POST autorizado de productos. |
| `WashLabs.Data/Repositories/ReservaRepository.cs` | Reserva/proforma atómicas, precio de sucursal, detección de repetición, bloqueo del vehículo, conflictos y carga del documento relacionado. |
| `WashLabs.Data/Repositories/FacturaRepository.cs` | Consulta de ambos tipos, suma de importes SQL e incorporación transaccional de productos con bloqueo de cita. |
| `WashLabs.Data/Repositories/OperacionRepository.cs` | Conversión dentro de la transacción de WashPoints, rollback explícito y cancelación con proforma Anulada. |
| `WashLabs.Data/Interfaces/IFacturaRepository.cs` | Contrato para agregar productos. |
| `WashLabs.Data/Interfaces/IOperacionRepository.cs` | Contrato de cancelación. |
| `WashLabs.Services/Implementations/FacturaService.cs` | Delega la incorporación de productos al repositorio existente. |
| `WashLabs.Services/Implementations/OperacionService.cs` | Valida IDs y delega la cancelación. |
| `WashLabs.Services/Interfaces/IFacturaService.cs` | Expone la incorporación de productos al controlador. |
| `WashLabs.Services/Interfaces/IOperacionService.cs` | Expone la cancelación al controlador. |
| `WashLabs.Models/DTOs/FacturaDTO.cs` | Tipo, cita, permisos de incorporación, impuesto y total por línea. |
| `WashLabs.Models/DTOs/ReservaDTO.cs` | Identificador opcional del documento relacionado. |
| `WashLabs.Services/Mappers/FacturaMapper.cs` | Mapea tipo, estado, líneas e incorporación disponible según la cita. |
| `WashLabs.Services/Mappers/ReservaMapper.cs` | Mapea el identificador del documento de la cita. |
| `Views/Reservas/Detalle.cshtml` | Vínculo directo al documento y acción de cancelación en estados permitidos. |
| `Views/Facturacion/Index.cshtml` | Identifica PROFORMA y FACTURA en el historial. |
| `Views/Facturacion/Detalle.cshtml` | Encabezado, referencia a cita, IVA/total por línea y formulario de productos. |
| `tests/WashLabs.Verificacion/Program.cs` | Opción `--integracion`. |
| `tests/WashLabs.Verificacion/PruebasIntegracionFacturacion.cs` | Nuevo conjunto de pruebas SQL de creación, conversión, rollback, concurrencia y permisos. |
| `tests/WashLabs.Verificacion/PruebasWashPoints.cs` | Fixtures de proformas y estados para conservar las verificaciones transaccionales anteriores. |
| `tests/WashLabs.Verificacion/PruebasHttpAutenticadas.cs` | Detalles Razor, permisos y recorrido HTTP completo; permite seleccionar el ensamblado de prueba. |
| `Database/VerificarIntegracionFacturacion.sql` | Nuevo diagnóstico de solo lectura compatible con SQL Server 2019. |
| `README.md` | Enlace al informe de la integración. |
| `docs/avance3.md` | Actualiza el alcance funcional anterior. |
| `docs/persistencia.md` | Describe la coordinación transaccional y los totales sin trigger. |
| `docs/integracion-facturacion.md` | Informe, resultados, pasos manuales y evidencia Git. |

No se modificaron el esquema EF, las entidades, los catálogos ni los estilos para
esta integración. Las modificaciones de `WashLabsDbContext.cs` mostradas por Git
ya estaban preparadas al comenzar.

## C. Flujo de facturación

1. MVC exige autenticación; para Cliente fija el ClienteId desde la sesión.
2. El servicio mantiene las validaciones de DTO y fecha futura. El repositorio
   verifica actor activo, alcance, sucursal, cliente, vehículo, preguntas,
   servicio, agenda y reservas incompatibles dentro de la transacción.
3. Guarda la cita Pendiente y su documento PROFORMA/Proforma, relacionado por
   CitaId, ClienteId y SucursalId. Agrega una línea del servicio, cantidad 1,
   PrecioSucursal o TarifaBase e IVA del 13%.
4. SQL calcula subtotal, impuesto y total de cada línea. El repositorio suma los
   subtotales e impuestos persistidos y guarda Total = Subtotal + Impuesto.
5. El cliente y el personal autorizado pueden consultar la proforma. El encargado
   puede incorporar productos; sus precios se obtienen de SQL y quedan pactados.
6. Completar valida actor/sucursal/estado, bloquea la cita, obtiene su proforma y
   conserva FacturaId y todas las líneas. Establece FACTURA/Pendiente y la fecha
   de emisión definitiva. No genera un segundo documento ni un pago.
7. Completa cita, factura y WashPoints con un único commit. Ante fallo, rollback.

La identificación del cliente se muestra desde la relación existente con Usuario;
no se agregó un campo de identificación histórica al encabezado. Cancelar mantiene
el documento PROFORMA, cambia su estado a Anulada y conserva sus líneas e importes.

## D. WashPoints

Se conserva la transacción ReadCommitted de `CompletarCitaAsync`. Su actualización
condicional de estado bloquea la cita y serializa dos solicitudes sobre el mismo ID.
La conversión de la proforma se realiza con el mismo DbContext y la misma transacción;
no invoca otra transacción de facturación.

Antes de consultar o crear la tarjeta se mantiene el bloqueo exclusivo del vehículo.
La tarjeta conserva su relación con el vehículo y la matrícula consultada por el
módulo existente. Se comprueba ACUMULA por CitaId, se agrega exactamente una unidad
y se actualiza el contador. Los índices `UQ_TarjetasWP_Vehiculo` y
`UX_MovimientosWP_CitaAcumula` complementan la protección del código.

Pendientes, confirmadas o reprogramadas solo acumulan después de una finalización
exitosa. Canceladas y No asistió no se finalizan. Repetir una finalización exitosa
no vuelve a convertir ni acreditar. Dos citas de un mismo vehículo conservan ambos
incrementos. Productos y cancelación también bloquean la cita antes de cambiar su
documento, coordinándose con la finalización.

## E. Base de datos

No se ejecutaron INSERT, UPDATE, DELETE, DDL, migraciones, seeds ni cambios de
contraseñas sobre WashLabsDB. No hay cambios de esquema pendientes de aprobación.
Los estados y restricciones actuales permiten toda la integración.

Se preparó y ejecutó `Database/VerificarIntegracionFacturacion.sql` contra WashLabsDB
exclusivamente en modo de solo lectura. Confirmó los cuatro estados de factura,
los índices únicos, las tres columnas calculadas persistidas, los CHECK y cero
triggers. Sus consultas de anomalías devolvieron cero filas: sin duplicados de
documentos o acumulaciones, sin diferencias cliente/sucursal entre cita y factura,
y sin diferencias entre cabecera y subtotales/impuestos del detalle.

Tablas utilizadas: Citas, Facturas, DetallesFactura, EstadosCita, EstadosFactura,
Usuarios, Roles, Sucursales, Vehiculos, Servicios, SucursalServicios, HorariosSucursal,
BloqueosAgenda, PreguntasReserva, RespuestasReserva, Productos, CategoriasProducto,
TarjetasWashPoints, MovimientosWashPoints y ConfiguracionWashPoints. PagosFactura
se consulta en pruebas para confirmar ausencia de pagos inventados.

Relaciones: Cita–Factura es opcional uno a uno con `UX_Facturas_Cita` filtrado por
CitaId no nulo; Factura–Detalles es uno a muchos; cita/factura relacionan cliente
y sucursal; detalle referencia servicio o producto mediante CHECK; tarjeta es
única por vehículo y ACUMULA es único por cita.

Las escrituras de verificación se realizaron solo en WashLabs_Pruebas_Avance3.
Se conservaron datos sintéticos TST: sucursales, servicio/tarifas, horarios,
categoría/productos, vehículos, citas, proformas/facturas y movimientos. El runner
HTTP renovó los hashes de las tres cuentas sintéticas para iniciar sesiones; no
cambió cuentas originales. La preparación de una base nueva no se ejecutó en esta
sesión, pues la base aislada ya existía. Las pruebas agregaron los estados de factura
que faltaban en esa base.

El esquema de pruebas generado por EF conserva columnas calculadas e índices, pero
no reproduce todos los CHECK originales. Si se requiere una copia idéntica, el DBA
debe restaurar un respaldo autorizado en una base de pruebas con nombre y archivos
físicos distintos, sin sobrescribir WashLabsDB. Este trabajo no realizó esa copia;
el runner HTTP exige las tres cuentas sintéticas de la base preparada, por lo que
no debe ejecutarse sin adaptar sus fixtures contra una copia con cuentas reales.

## F. Compilación y resultados ejecutados

| Conjunto, última ejecución completa | Comprobaciones específicas | Resultado |
| --- | ---: | --- |
| Pruebas comunes en memoria | 8 distintas, repetidas en cada runner | Aprobadas |
| `--integracion` | 39 | Aprobadas |
| `--washpoints` | 10 | Aprobadas |
| `--http-autenticado` | 44 | Aprobadas |

Total: 101 casos distintos; 117 comprobaciones aprobadas en el último ciclo completo
de los tres runners, incluyendo las 8 comunes repetidas tres veces. Fallidas en ese
ciclo: 0. Los 16 requisitos mínimos de pruebas están cubiertos. La inspección visual
manual no se ejecutó. Tampoco se ejecutó el preparador contra una base nueva ni se
escribieron datos de prueba sobre la base original.

Durante iteraciones previas se corrigieron un importe esperado incorrecto, un
correo de fixture que no permitía repetir la suite y el interbloqueo envuelto por
EF. Esas ejecuciones incompletas no se cuentan como suites aprobadas. El aislamiento
impidió inicialmente la conexión cifrada; las ejecuciones SQL autorizadas posteriores
completaron correctamente las suites.

`dotnet build WashLabs.sln --no-restore -p:OutputPath=bin/Integracion/` y
`dotnet build tests/WashLabs.Verificacion --no-restore`: ambos con 0 errores y
0 advertencias. No hizo falta restore. El intento de build en la salida habitual
encontró ensamblados bloqueados por una aplicación en ejecución; se utilizó otra
salida sin detenerla.

Para repetir sobre la base sintética existente, desde la raíz del repositorio:

```powershell
dotnet build WashLabs.sln --no-restore -p:OutputPath=bin/Integracion/
dotnet build tests/WashLabs.Verificacion --no-restore
$env:WASHLABS_TEST_CONNECTION='Server=localhost;Database=WashLabs_Pruebas_Avance3;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15'
dotnet run --project tests/WashLabs.Verificacion --no-build -- --integracion
dotnet run --project tests/WashLabs.Verificacion --no-build -- --washpoints
$env:WASHLABS_TEST_APP_PATH='bin/Integracion/WashLabs.dll'
dotnet run --project tests/WashLabs.Verificacion --no-build -- --http-autenticado
```

El HTTP runner necesita los fixtures creados por `--integracion`; conserva las
restricciones que comprueban el nombre de la base y las tres cuentas sintéticas.

## G. Pruebas manuales en Visual Studio y navegador

1. Abrir WashLabs.sln y seleccionar WashLabs como proyecto inicial. Usar una
   conexión local a WashLabs_Pruebas_Avance3 para cualquier operación que escriba.
   Detener la instancia propia antes de compilar con la salida habitual y ejecutar
   sin aplicar migraciones ni seeds a la base original.
2. Iniciar sesión con un cliente de prueba conocido. Registrar/elegir un vehículo
   propio y crear una reserva con sucursal, servicio, fecha futura y horario
   disponible. Confirmar el mensaje y el enlace al documento.
3. Abrir el documento: debe decir PROFORMA/Proforma y mostrar cita, cliente,
   identificación disponible, sucursal, línea del servicio e importes. Comprobar
   el precio contra PrecioSucursal o TarifaBase en la base aislada.
4. Salir e ingresar como encargado de esa sucursal. Abrir la proforma y agregar
   dos productos con cantidades enteras positivas. Confirmar las tres líneas,
   subtotal, IVA y total. Repetir el mismo envío no debe duplicar su línea.
5. Abrir la reserva y completar el servicio. Confirmar el mensaje. Volver al
   documento: mismo identificador, tipo FACTURA, estado Pendiente, tres líneas y
   totales conservados. No debe existir un pago creado por esta acción.
6. Abrir WashPoints y comprobar exactamente un lavado y un movimiento para esa
   cita/matrícula. Repetir el POST o probar dos finalizaciones simultáneas en la
   base aislada: sin nueva factura ni segundo lavado.
7. Crear otra reserva y cancelarla. Comprobar cita Cancelada, PROFORMA/Anulada,
   ausencia de acción de completar y ausencia de acumulación.
8. Comprobar que un cliente no ve acciones de productos/completar, que un encargado
   de otra sucursal no puede acceder al documento/cita y que el administrador
   conserva su alcance global. Verificar productos, listados y detalles existentes.

Estas comprobaciones visuales se entregan como pasos pendientes; las verificaciones
HTTP automatizadas sí ejecutaron creación, dos productos, finalización, doble envío,
vistas y permisos con cookies y antifalsificación reales.

## H. Estado de Git

Rama verificada al inicio: `cambios-realizados-avance3`. No se cambió de rama,
no se crearon commits y no se realizó push. Los cambios preparados del equipo
permanecen preparados. Los nuevos cambios quedan disponibles para revisión.
El archivo no versionado preexistente cuyo nombre empieza por `y pruebas del Avance 3`
se conservó sin modificar.

Las salidas finales de Git se incluyen a continuación. `git diff --stat` describe
los cambios sin preparar sobre el índice; no incluye archivos nuevos no versionados
ni los cambios que ya estaban preparados al comenzar.


```text
> git status --short
MM Controllers/FacturacionController.cs
MM Controllers/ReservasController.cs
A  Database/IdentificacionesDemoAvance3.sql
M  Database/SeedFacturacionReservasAvance3.sql
A  Database/VerificarAvance3.sql
MM README.md
 M Views/Facturacion/Detalle.cshtml
 M Views/Facturacion/Index.cshtml
M  Views/Reservas/Crear.cshtml
 M Views/Reservas/Detalle.cshtml
M  Views/Reservas/Index.cshtml
M  WashLabs.Data/Context/WashLabsDbContext.cs
MM WashLabs.Data/Interfaces/IFacturaRepository.cs
 M WashLabs.Data/Interfaces/IOperacionRepository.cs
M  WashLabs.Data/Interfaces/IReservaRepository.cs
MM WashLabs.Data/Repositories/FacturaRepository.cs
MM WashLabs.Data/Repositories/OperacionRepository.cs
MM WashLabs.Data/Repositories/ReservaRepository.cs
M  WashLabs.Models/DTOs/CrearReservaDTO.cs
 M WashLabs.Models/DTOs/FacturaDTO.cs
 M WashLabs.Models/DTOs/ReservaDTO.cs
MM WashLabs.Services/Implementations/FacturaService.cs
 M WashLabs.Services/Implementations/OperacionService.cs
M  WashLabs.Services/Implementations/ReservaService.cs
MM WashLabs.Services/Interfaces/IFacturaService.cs
 M WashLabs.Services/Interfaces/IOperacionService.cs
M  WashLabs.Services/Interfaces/IReservaService.cs
 M WashLabs.Services/Mappers/FacturaMapper.cs
 M WashLabs.Services/Mappers/ReservaMapper.cs
M  WashLabs.csproj
AM docs/avance3.md
MM docs/persistencia.md
A  tests/VerificarHttp.ps1
AM tests/WashLabs.Verificacion/Program.cs
AM tests/WashLabs.Verificacion/PruebasHttpAutenticadas.cs
AM tests/WashLabs.Verificacion/PruebasWashPoints.cs
A  tests/WashLabs.Verificacion/WashLabs.Verificacion.csproj
M  wwwroot/js/site.js
?? Database/VerificarIntegracionFacturacion.sql
?? docs/integracion-facturacion.md
?? tests/WashLabs.Verificacion/PruebasIntegracionFacturacion.cs
?? "y pruebas del Avance 3\357\200\242"

> git diff --stat
 Controllers/FacturacionController.cs               | 13 +++-
 Controllers/ReservasController.cs                  | 13 +++-
 README.md                                          |  2 +
 Views/Facturacion/Detalle.cshtml                   | 23 ++++--
 Views/Facturacion/Index.cshtml                     |  2 +-
 Views/Reservas/Detalle.cshtml                      | 10 ++-
 WashLabs.Data/Interfaces/IFacturaRepository.cs     |  1 +
 WashLabs.Data/Interfaces/IOperacionRepository.cs   |  1 +
 WashLabs.Data/Repositories/FacturaRepository.cs    | 54 +++++++++++++-
 WashLabs.Data/Repositories/OperacionRepository.cs  | 54 +++++++++++++-
 WashLabs.Data/Repositories/ReservaRepository.cs    | 46 ++++++++++--
 WashLabs.Models/DTOs/FacturaDTO.cs                 |  7 +-
 WashLabs.Models/DTOs/ReservaDTO.cs                 |  2 +-
 .../Implementations/FacturaService.cs              |  2 +
 .../Implementations/OperacionService.cs            |  3 +
 WashLabs.Services/Interfaces/IFacturaService.cs    |  1 +
 WashLabs.Services/Interfaces/IOperacionService.cs  |  1 +
 WashLabs.Services/Mappers/FacturaMapper.cs         |  8 +-
 WashLabs.Services/Mappers/ReservaMapper.cs         |  2 +-
 docs/avance3.md                                    | 14 ++--
 docs/persistencia.md                               | 12 ++-
 tests/WashLabs.Verificacion/Program.cs             |  5 ++
 .../PruebasHttpAutenticadas.cs                     | 86 +++++++++++++++++++++-
 tests/WashLabs.Verificacion/PruebasWashPoints.cs   | 10 +++
 24 files changed, 334 insertions(+), 38 deletions(-)

> git diff --check
(sin salida; código de salida 0)

> git diff --check --cached
(sin salida; código de salida 0)
```

