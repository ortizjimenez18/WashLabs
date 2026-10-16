# Reestructuración del facturador de WashLabs

EvoGroup · ISW-711 · .NET 8 MVC / EF Core / SQL Server 2019.
Rama: `cambios-realizados-avance3`. Validación: 13 de octubre de 2026.

## Resultado funcional

| Funcionalidad | Estado anterior | Cambio realizado | Prueba | Resultado |
| --- | --- | --- | --- | --- |
| Orden del historial | Fecha e ID descendentes, sin selector | Cuatro órdenes con desempate numérico | SQL: recientes, antiguas, mayor y menor | Aprobado |
| Filtros | Sin filtros | Tipo, estado, fechas, cliente/identificación, número y sucursal combinables por GET | SQL y HTTP con combinación de filtros | Aprobado |
| Rango de fechas | Sin filtro | Días completos incluidos; rango invertido rechazado | SQL y mensaje de validación HTTP | Aprobado |
| Permisos del listado | Filtro por rol | Conservado antes de todos los criterios de búsqueda | Cliente ajeno y encargado de otra sucursal | Aprobado |
| Estados y sucursales | Sin selectores | Estados reales y sucursales autorizadas desde SQL | Opciones consultadas y filtros por rol | Aprobado |
| Resumen | Sin indicadores | Facturas, proformas pendientes y total de facturas vigentes | Agregación SQL; excluye proformas y anuladas del importe | Aprobado |
| Sin resultados / limpiar | Estado vacío genérico | Mensaje de búsqueda y enlace que elimina parámetros | SQL y HTTP | Aprobado |
| Cuatro campos principales | Cuatro campos en tarjetas | Documento, cliente, fecha y total; insignias integradas en documento | Cuenta de cuatro columnas en HTML por cada rol | Aprobado |
| Encabezado comercial | Datos básicos del documento | Logo, empresa configurada, cliente, identificación, sucursal y contactos | DTO, HTTP y capturas de navegador | Aprobado |
| Catálogo en proforma | Selector completo de productos | Sección visible con búsqueda por nombre y categoría en SQL | Catálogo LINQ y búsqueda HTTP | Aprobado |
| Incorporación de productos | Ya guardaba precio y cantidad | Conservada en la misma orden mediante transacción común | SQL, varios productos y flujo HTTP | Aprobado |
| Cambio de cantidades | No existía | Cambio absoluto de unidades, preservando precio y descuento | SQL, reintento, concurrencia y HTTP | Aprobado |
| Retirada de productos | No existía | Elimina solo productos de proforma autorizada; conserva servicio | SQL, repetición y HTTP | Aprobado |
| Subtotal, IVA y total | Recalculaba al agregar y finalizar | Recalculados también al editar/eliminar desde valores persistidos | Importes exactos y rollback de cabecera | Aprobado |
| Factura definitiva | Conversión desde reservas ya implementada | Acción desde facturación que reutiliza la finalización existente | Tres líneas conservadas y edición posterior rechazada | Aprobado |
| WashPoints | Transacción e índices únicos existentes | Conservados; ninguna edición acredita lavados | Regresión, concurrencia y doble finalización | Aprobado |
| Pagos | Tabla existente, emisión sin pago automático | Muestra pagos/saldo únicamente cuando existen registros | Lectura existente; emisión sigue sin generar pagos | Sin cambios en el proceso de pago |
| Retorno al listado | Volvía al listado general | URL local validada conserva filtros al abrir/editar/finalizar | HTTP de edición con retorno filtrado | Aprobado |
| Impresión | Sin acción ni estilo específico | Botón, estilos A4 y ocultación de controles | Edge en modo impresión y PDF de prueba | Aprobado |
| Escritorio y móvil | Presentación básica | Historial y detalle adaptables; líneas completas visibles en móvil | Edge a 1366 y 390 px; capturas inspeccionadas | Aprobado |
| Regresiones | Suites anteriores | Se conservan reservas, productos, asistente y acceso por rol | 218 verificaciones anteriores, integración y WashPoints | Aprobado |

## Cómo funcionan los filtros

`FacturacionController.Index` recibe `FiltroFacturacionDTO`, valida los criterios y
entrega un ViewModel tipado. El servicio valida también el DTO antes de consultar
el repositorio. Ante un criterio inválido, MVC muestra los errores y carga únicamente
las opciones; no consulta ni muestra un listado amplio con un filtro incompleto.

El repositorio comienza con `Autorizadas(usuarioId)`: administrador global, encargado
de una sucursal activa o cliente propietario, siempre con cuenta activa. Después
agrega los `Where` por tipo, estado, fechas, cliente/identificación, número exacto
e ID de sucursal. La sucursal enviada por el navegador nunca amplía el alcance.
Solo el administrador ve el selector de sucursales.

El orden predeterminado es `Fecha DESC, FacturaId DESC`. Antiguas usa ambos criterios
ascendentes. Los órdenes por importe usan Total y desempatan por fecha e ID descendentes.
FacturaId se compara como `long`, no como texto. No se renumeraron documentos.

El resumen se obtiene con `GroupBy`, `Count` y `Sum` en SQL sobre la consulta filtrada.
Facturas cuenta documentos definitivos, incluidas anuladas para mantener trazabilidad.
Proformas pendientes cuenta PROFORMA/Proforma. Total facturado suma únicamente
FACTURA con estado distinto de Anulada; no representa cobros, y no suma proformas.
Convertir cambia el mismo registro, por lo que no cuenta el documento dos veces.

Parámetros GET: `TipoDocumento`, `EstadoFacturaId`, `FechaInicial`, `FechaFinal`,
`Cliente`, `NumeroDocumento`, `SucursalId` y `Orden` (`recientes`, `antiguas`, `mayor`,
`menor`). El número se escribe sin el prefijo visual `#`. Limpiar vuelve a `/Facturacion`
sin parámetros. El detalle recibe `volverUrl`, validado como ruta local de facturación.

## Productos y totales

El botón **+ Agregar producto** abre la sección del catálogo mediante un GET al
detalle. `IProductoService` reutiliza las consultas SQL por nombre y categoría;
solo se ofrecen productos activos. Se muestran categoría y precio consultados.

Agregar obtiene nuevamente el producto/precio en SQL; ningún precio del navegador
se acepta como precio pactado. Se conservan líneas independientes cuando se agrega
el mismo producto con una cantidad diferente. Repetir producto/cantidad que ya existe
conserva la línea existente. Para ajustar una línea se usa **Guardar cantidad**.
Las escrituras deshabilitan temporalmente su botón para evitar doble clic.

Cambiar cantidad usa el ID del detalle dentro del documento autorizado, cambia
unidades absolutas y conserva PrecioUnitario, Descuento e impuesto. Si el descuento
superara el nuevo importe de línea, rechaza el cambio. Se aceptan unidades enteras
positivas, como en la integración anterior. Quitar solo permite TipoLinea P; el
servicio original no se puede editar ni retirar. También se retiran las personalizaciones
relacionadas cuando existan. Repetir una retirada ya completada es seguro.

Las tres escrituras comparten `ModificarOrdenAsync`: identifica el documento
autorizado, bloquea su cita, verifica cliente/vehículo/sucursal activos y coherentes,
estado de cita editable y documento PROFORMA/Proforma. Después modifica líneas,
guarda sus columnas calculadas SQL, suma los importes persistidos y actualiza
Subtotal, Impuesto, Total y Origen dentro de la misma transacción. Un fallo revierte
el detalle y la cabecera. Las facturas definitivas y proformas anuladas no se editan.

Las columnas SQL mantienen cantidad × precio − descuento y redondeo decimal(18,2).
La cabecera suma los subtotales e impuestos guardados; Total = Subtotal + Impuesto.
No se agregó otra fórmula de importes en JavaScript ni un trigger. El POST redirige
al detalle actualizado, así que los importes se ven sin volver al historial.

## Reserva, factura y WashPoints

Se mantiene el flujo existente: reserva y proforma atómicas; servicio con precio
de sucursal o tarifa base; orden editable; finalización con factura definitiva
y un lavado. `Facturacion/CompletarCita` obtiene la cita real del documento autorizado
y llama a `IOperacionService.CompletarCitaAsync`. No confía en un CitaId del formulario.

La actualización condicional de la cita, la conversión del mismo FacturaId, sus
totales y la acreditación WashPoints comparten la transacción existente. Las
ediciones también bloquean la cita, coordinándose con la finalización. No hay
transacciones anidadas ni un segundo proceso de acumulación. Se conserva FACTURA/Pendiente;
emitir no registra pagos. Se muestran pagos/saldo solo a partir de PagoFactura existente.

## Archivos modificados en esta ampliación

| Ruta | Cambio |
| --- | --- |
| `Controllers/FacturacionController.cs` | Filtros GET, ViewModels, catálogo, cantidades, retirada, finalización y retorno seguro. |
| `WashLabs.Data/Repositories/FacturaRepository.cs` | IQueryable filtrado/ordenado, resumen y opciones SQL; transacción común de productos; lectura de configuración/pagos. |
| `WashLabs.Data/Interfaces/IFacturaRepository.cs` | Contratos de consultas, cantidades, retirada y configuración. |
| `WashLabs.Services/Implementations/FacturaService.cs` | Validación de filtros, delegación de edición y nombre comercial del detalle. |
| `WashLabs.Services/Interfaces/IFacturaService.cs` | Contratos de historial y gestión de productos. |
| `WashLabs.Services/Mappers/FacturaMapper.cs` | Estado, IDs de líneas, referencia de producto, impuesto y pagos reales. |
| `WashLabs.Models/DTOs/FacturaDTO.cs` | Estado, IDs de detalle, tipo, configuración y monto pagado. |
| `WashLabs.Models/DTOs/FiltroFacturacionDTO.cs` | Nuevo: criterios validados, resumen y opciones. |
| `ViewModels/FacturacionIndexViewModel.cs` | Nuevo: modelo tipado de filtros, historial e indicadores. |
| `ViewModels/FacturacionDetalleViewModel.cs` | Nuevo: documento, permisos y búsqueda del catálogo. |
| `Views/Facturacion/Index.cshtml` | Historial organizado con resumen, filtros y cuatro campos principales. |
| `Views/Facturacion/Detalle.cshtml` | Documento comercial, acciones y gestión de orden editable. |
| `Views/Shared/_EstadoFactura.cshtml` | Nuevo: insignias con colores consistentes. |
| `Views/Shared/_Layout.cshtml` | Sección Styles opcional; no cambia las demás vistas. |
| `wwwroot/css/facturacion.css` | Nuevo: estilos aislados del módulo, móvil e impresión. |
| `wwwroot/js/facturacion.js` | Nuevo: imprimir y evitar doble clic. El chatbot conserva su posición fija global, también en facturación. |
| `tests/WashLabs.Verificacion/Program.cs` | Opción `--facturador`. |
| `tests/WashLabs.Verificacion/PruebasFacturador.cs` | Nuevo: 44 comprobaciones SQL de filtros y gestión de proformas. |
| `tests/WashLabs.Verificacion/PruebasHttpAutenticadas.cs` | Filtros, edición, eliminación, finalización desde facturación y opción visual. |
| `tests/WashLabs.Verificacion/PruebasNavegadorFacturacion.cs` | Nuevo: Edge con perfil temporal, capturas y verificación de impresión, sin dependencias adicionales. |
| `README.md`, `docs/persistencia.md`, `docs/avance3.md` | Enlaces y alcance actualizados. |
| `docs/integracion-facturacion.md` | Conserva resultados anteriores y enlaza esta ampliación. |
| `docs/facturador-avance3.md` | Nuevo: reporte y evidencia de esta tarea. |

Las modificaciones anteriores de reservas y OperacionRepository/WashPoints se
conservaron sin nuevos cambios en esta ampliación. No se modificaron entidades,
DbContext, precios históricos, estilos globales ni dependencias del proyecto.

## Rutas de la interfaz

- GET `/Facturacion` o `/Facturacion/Index`: historial, filtros e indicadores.
- GET `/Facturacion/Detalle/{id}`: documento; acepta `mostrarProductos`,
  `busquedaProducto`, `categoriaProductoId` y `volverUrl`.
- POST `/Facturacion/AgregarProducto/{id}`: incorporación existente adaptada.
- POST `/Facturacion/CambiarCantidad/{id}`: nuevo; recibe detalleId y cantidad.
- POST `/Facturacion/QuitarProducto/{id}`: nuevo; recibe detalleId.
- POST `/Facturacion/CompletarCita/{id}`: nuevo; recupera la cita por el documento.

Los POST exigen Encargado/Administrador y antifalsificación. Las consultas exigen
autenticación y el alcance real del usuario en SQL. Completar desde Reservas sigue
funcionando.

## Compilación y pruebas ejecutadas

Compilación de solución y verificador: 0 errores y 0 advertencias. No hizo falta
restore. Se usó `bin/Integracion` para conservar la instancia existente que bloquea
la salida habitual.

| Última ejecución completa | Específicas | Comunes incluidas | Total aprobado |
| --- | ---: | ---: | ---: |
| `--facturador` | 44 | 8 | 52 |
| `--integracion` | 39 | 8 | 47 |
| `--washpoints` | 10 | 8 | 18 |
| `--http-autenticado`, con Edge | 72 | 8 | 80 |
| Verificador existente sin argumentos, solo lectura | 210 | 8 | 218 |
| Total | 375 | 40 | 415 |

415 comprobaciones aprobadas en el ciclo final, incluyendo pruebas comunes repetidas.
Fallidas en ese ciclo: 0. Al deduplicar las 8 comunes repetidas cinco veces, son
383 comprobaciones. La conexión desde el sandbox falló inicialmente por TLS; se
repitió con ejecución autorizada. Esa ejecución incompleta no se considera aprobada.

Se comprobaron con SQL ejemplos existentes de una, dos y tres líneas y documentos
mixtos. Edge validó historial y detalle a 1366 px y 390 px y el modo de impresión;
las capturas se inspeccionaron. Los importes y permisos se probaron con datos reales
persistidos. Pendientes: revisión humana de la presentación académica, impresión
en una impresora física y comprobación en otros navegadores/dispositivos; no se
declaran ejecutadas esas revisiones.

Evidencia local, con datos sintéticos y excluida de Git por `artifacts/`:

- `artifacts/facturador/historial-1366.png`
- `artifacts/facturador/historial-390.png`
- `artifacts/facturador/detalle-1366.png`
- `artifacts/facturador/detalle-390.png`
- `artifacts/facturador/proforma-prueba.pdf`

El PDF es una impresión de prueba del documento académico, sin facturación fiscal
electrónica. Los fixtures TST y las cuentas sintéticas se conservan en la base de
pruebas. El navegador utilizó un perfil temporal separado, sin sesiones personales.

## SQL y conservación de datos

No se requirieron ni propusieron cambios de esquema. No se ejecutó DDL ni escritura
de documentos, clientes o credenciales en WashLabsDB. El diagnóstico SQL existente
`Database/VerificarIntegracionFacturacion.sql` se ejecutó en modo de solo lectura y
confirmó estados, índices, columnas calculadas y ausencia de inconsistencias de
totales/relaciones o duplicados. El verificador anterior también consultó la base
original sin escrituras.

Todas las escrituras de pruebas usan exclusivamente WashLabs_Pruebas_Avance3,
comprobada por el runner. El nuevo fixture incluye un cliente inactivo sintético
para demostrar que los filtros de un cliente no permiten documentos ajenos;
no modifica clientes reales. Los hashes que renueva HTTP pertenecen únicamente
a las tres cuentas sintéticas de prueba. No se generan documentos históricos
ni se cambian números o importes de la base original.

## Repetir las verificaciones

Desde la raíz, con la base sintética ya preparada:

```powershell
dotnet build WashLabs.sln --no-restore -p:OutputPath=bin/Integracion/
dotnet build tests/WashLabs.Verificacion --no-restore
$env:WASHLABS_TEST_CONNECTION='Server=localhost;Database=WashLabs_Pruebas_Avance3;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15'
dotnet run --project tests/WashLabs.Verificacion --no-build -- --facturador
dotnet run --project tests/WashLabs.Verificacion --no-build -- --integracion
dotnet run --project tests/WashLabs.Verificacion --no-build -- --washpoints
$env:WASHLABS_TEST_APP_PATH='bin/Integracion/WashLabs.dll'
$env:WASHLABS_VISUAL_TESTS='1'
dotnet run --project tests/WashLabs.Verificacion --no-build -- --http-autenticado
dotnet run --project tests/WashLabs.Verificacion --no-build
```

La última orden usa la conexión local configurada y realiza solamente lecturas.
La opción visual requiere Microsoft Edge en Windows. Sin `WASHLABS_VISUAL_TESTS=1`,
HTTP ejecuta 66 comprobaciones específicas. La base de pruebas creada desde EF
no contiene todos los CHECK de la original; las restricciones originales se
verificaron en lectura. No ejecutar un preparador sobre WashLabsDB.

## Verificación manual

1. Abrir WashLabs.sln en Visual Studio. Para operaciones de escritura seleccionar
   la conexión a WashLabs_Pruebas_Avance3; detener la instancia propia antes de
   compilar a la salida habitual. Iniciar con cuentas de prueba conocidas.
2. Entrar como administrador a Facturación. Probar los cuatro órdenes, tipos,
   estados, intervalo de fechas, cliente/identificación, número y sucursal.
   Combinar criterios y comprobar que Limpiar filtros los elimina.
3. Abrir un documento desde una búsqueda; Volver al historial debe conservarla.
   Probar un rango invertido y una búsqueda sin coincidencias.
4. Crear una reserva como cliente, con vehículo propio y horario disponible.
   Abrir su proforma y comprobar servicio, cliente, sucursal e importes.
5. Entrar como encargado de esa sucursal. Abrir la proforma y seleccionar
   + Agregar producto. Buscar por nombre/categoría, agregar dos productos y
   revisar los tres detalles de la misma orden.
6. Cambiar una cantidad con Guardar cantidad, quitar otro producto y revisar
   subtotal/IVA/total tras cada operación. El servicio debe permanecer. Probar
   cantidad cero y confirmar rechazo. Reagregar el producto para tener tres líneas.
7. Completar cita y emitir factura desde el documento. Debe conservar número,
   detalles y cantidades, quedar FACTURA/Pendiente y mostrar un único lavado
   en WashPoints. Repetir la finalización no debe duplicar factura ni lavado.
8. Comprobar que la factura definitiva no muestra controles de edición. Intentar
   acciones con cliente o encargado de otra sucursal debe denegarse en el servidor.
9. Imprimir o guardar como PDF desde el botón. Revisar logo, encabezado, detalles
   y totales sin menú, asistente ni controles. Reducir la ventana a tamaño móvil
   y verificar que todas las cantidades, precios y subtotales estén visibles.

## Git

Se verificó y conservó `cambios-realizados-avance3`. No hubo commits ni push. Los
28 archivos previamente preparados permanecen preparados, sin cambios en el índice.
Se conservaron también los cambios de la integración anterior y el archivo no
versionado preexistente cuyo nombre empieza por `y pruebas del Avance 3`.
La implementación nueva queda sin preparar para revisión. La evidencia final de
Git se incluye a continuación; el diff contra el índice incluye también la integración
anterior y no cuenta archivos nuevos todavía no versionados.

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
 M Views/Shared/_Layout.cshtml
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
?? ViewModels/FacturacionDetalleViewModel.cs
?? ViewModels/FacturacionIndexViewModel.cs
?? Views/Shared/_EstadoFactura.cshtml
?? WashLabs.Models/DTOs/FiltroFacturacionDTO.cs
?? docs/facturador-avance3.md
?? docs/integracion-facturacion.md
?? tests/WashLabs.Verificacion/PruebasFacturador.cs
?? tests/WashLabs.Verificacion/PruebasIntegracionFacturacion.cs
?? tests/WashLabs.Verificacion/PruebasNavegadorFacturacion.cs
?? wwwroot/css/facturacion.css
?? wwwroot/js/facturacion.js
?? "y pruebas del Avance 3\357\200\242"

> git diff --stat
 Controllers/FacturacionController.cs               |  69 +++++++++-
 Controllers/ReservasController.cs                  |  13 +-
 README.md                                          |   4 +
 Views/Facturacion/Detalle.cshtml                   | 129 ++++++++++++++---
 Views/Facturacion/Index.cshtml                     |  89 +++++++++---
 Views/Reservas/Detalle.cshtml                      |  10 +-
 Views/Shared/_Layout.cshtml                        |   1 +
 WashLabs.Data/Interfaces/IFacturaRepository.cs     |   7 +
 WashLabs.Data/Interfaces/IOperacionRepository.cs   |   1 +
 WashLabs.Data/Repositories/FacturaRepository.cs    | 152 ++++++++++++++++++++-
 WashLabs.Data/Repositories/OperacionRepository.cs  |  54 +++++++-
 WashLabs.Data/Repositories/ReservaRepository.cs    |  46 ++++++-
 WashLabs.Models/DTOs/FacturaDTO.cs                 |   8 +-
 WashLabs.Models/DTOs/ReservaDTO.cs                 |   2 +-
 .../Implementations/FacturaService.cs              |  15 +-
 .../Implementations/OperacionService.cs            |   3 +
 WashLabs.Services/Interfaces/IFacturaService.cs    |   5 +
 WashLabs.Services/Interfaces/IOperacionService.cs  |   1 +
 WashLabs.Services/Mappers/FacturaMapper.cs         |  10 +-
 WashLabs.Services/Mappers/ReservaMapper.cs         |   2 +-
 docs/avance3.md                                    |  18 ++-
 docs/persistencia.md                               |  15 +-
 tests/WashLabs.Verificacion/Program.cs             |  10 ++
 .../PruebasHttpAutenticadas.cs                     | 126 ++++++++++++++++-
 tests/WashLabs.Verificacion/PruebasWashPoints.cs   |  10 ++
 25 files changed, 723 insertions(+), 77 deletions(-)

> git diff --check
(sin salida; código 0)

> git diff --check --cached
(sin salida; código 0)
```
