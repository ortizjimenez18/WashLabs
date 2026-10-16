# Implementación y demostración del Avance 3

Proyecto WashLabs, EvoGroup, Universidad Técnica Nacional, ISW-711.

## Alcance implementado

Se mantienen MVC .NET 8, las cuatro capas y SQL Server con compatibilidad 150.
Productos y asistente reutilizan sus consultas y vistas existentes. Facturación
filtra en SQL por el usuario autenticado: administrador general, encargado de su
sucursal y cliente propietario. Los documentos históricos siguen visibles para
el personal autorizado aunque su cliente esté inactivo; no se reactiva al cliente.
La interpretación de conservación de documentos históricos debe confirmarse con
el profesor si la rúbrica académica exige otra política.

Reservas usa el ID de la sesión, nunca un encargado fijo. El administrador consulta
todas; el encargado solo su sucursal y el cliente sus citas. En Crear se selecciona
sucursal entre las permitidas y se valida de nuevo en SQL. El cambio de sucursal
recarga el formulario; primero elegir sucursal y después completar respuestas.
Se conservan disponibilidad, preguntas almacenadas y propiedad del vehículo.

WashPoints acredita una unidad cuando una cita Pendiente, Confirmada o Reprogramada
se completa por personal autorizado, con cliente y vehículo activos y propiedad
coherente. La actualización condicional bloquea la cita. Dentro de la misma
transacción ReadCommitted se bloquea el vehículo antes de consultar/crear su tarjeta,
se comprueba el movimiento ACUMULA, se incrementa el contador y se registra la unidad.
Los índices únicos protegen una tarjeta por vehículo y una acumulación por cita.
Un conflicto SQL controlado devuelve fallo sin guardar parcialmente; la pantalla
permite reintentar. Un error inesperado revierte al disponer la transacción.

Repetir una cita completada no agrega movimientos. Las citas históricas completadas
sin movimiento no se reparan automáticamente. El contador y el historial se consultan
por matrícula en WashPoints. Meta actual: 10. Redención, reinicio y premios quedan
para etapas posteriores. No se instalan triggers de acreditación.

Facturación permite consultar proformas y facturas con el mismo alcance por rol.
Crear una reserva genera su proforma y el servicio con precio pactado en la misma
transacción Serializable. El encargado puede incorporar unidades enteras de productos
antes de finalizar, cambiar cantidades y quitar productos adicionales. Completar convierte el mismo documento en FACTURA/Pendiente,
actualiza los totales desde las columnas calculadas SQL y acredita WashPoints dentro
de la transacción existente. Cancelar conserva la proforma con estado Anulada.
No se crean documentos para citas históricas sin proforma ni se registran pagos.
El informe y las pruebas actuales están en [integración de facturación](integracion-facturacion.md).
La ampliación de filtros, presentación comercial, edición e impresión se documenta
en [reestructuración del facturador](facturador-avance3.md), con verificaciones actuales
en SQL y navegador de escritorio/móvil. Los resultados posteriores de este documento
corresponden a la auditoría inicial.

## Acceso de demostración pendiente de aprobación

La base real conserva cuatro cuentas activas con hashes incompatibles: tres
encargados y un cliente. Los hashes válidos del administrador y otro cliente no
deben reemplazarse. Se conserva PasswordHasher de ASP.NET Core Identity, cookies,
roles y revocación de sesiones cuando cambia el hash, el rol o la sucursal.

El equipo debe identificar y aprobar explícitamente las cuentas DEMO. Después,
ejecutar interactivamente por cada cuenta aprobada:

```powershell
dotnet run --project WashLabs.csproj -- --configurar-acceso
```

Elegir el correo real del registro DEMO y una contraseña privada de 12–128 caracteres;
se introduce oculta y se confirma. Solo cambia PasswordHash y FechaActualizacion de
ese usuario activo, sin crear cuentas ni cambiar roles o sucursales. No poner la
contraseña en argumentos, scripts, capturas ni documentación. Este procedimiento
ya existía y no se ejecutó sobre WashLabsDB durante esta implementación.

Los nueve usuarios reales de la base no tienen identificación registrada. El detalle
ya muestra el dato del registro y usa “No registrada” si falta. Preparado
`Database/IdentificacionesDemoAvance3.sql`: llenar la lista exclusivamente con IDs
DEMO revisados; asigna `DEMO-A3-ID` solo a campos vacíos, dentro de una transacción.
Son identificaciones sintéticas, no cédulas de personas. Requiere aprobación antes
de ejecutarse. No altera campos ya completos, estado activo, roles ni contraseñas.

## Pruebas y reproducción

No había proyectos de pruebas. Se agregó una herramienta de verificación sin nuevas
librerías, separada del proyecto web:

```powershell
dotnet run --project tests/WashLabs.Verificacion -- --solo-unitarias
dotnet run --project tests/WashLabs.Verificacion
```

La segunda ejecución lee User Secrets o `ConnectionStrings__WashLabsDB` y realiza
solo consultas SQL. Resultado ejecutado: 202 verificaciones, incluidas 8 en memoria;
productos, categorías/opciones, totales/IVA, cuatro facturas administrativas,
listados/detalles por rol, reservas ajenas y tres respuestas. No prueba contraseñas
reales ni inicia sesiones HTTP autenticadas.

Para SQL que escribe se autorizó y creó `WashLabs_Pruebas_Avance3`, sin tocar
WashLabsDB. El esquema se deriva del modelo EF: 34 tablas y sus FK/índices, pero no
incluye todos los CHECK del esquema original. Los usuarios DEMO de esta base tienen
contraseñas aleatorias no publicadas y no sirven como credenciales de demostración.

```powershell
$env:WASHLABS_TEST_CONNECTION = 'Server=localhost;Database=WashLabs_Pruebas_Avance3;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15'
dotnet run --project tests/WashLabs.Verificacion -- --script-pruebas
# Solo en una base aislada nueva o sin los datos de preparación, con aprobación:
dotnet run --project tests/WashLabs.Verificacion -- --preparar-pruebas
dotnet run --project tests/WashLabs.Verificacion -- --washpoints
dotnet run --project tests/WashLabs.Verificacion -- --http-autenticado
```

El primer comando genera `artifacts/EstructuraPruebasAvance3.sql` y no lo ejecuta.
El preparador rechaza bases distintas del nombre exacto y no sobrescribe datos de
preparación. El ejecutor acepta solo el prefijo WashLabs_Pruebas_Avance3 y agrega
dos vehículos TST y nueve citas PRUEBA AVANCE 3 por ejecución; permanecen disponibles
para inspección. No borra registros ni bases. No apuntar estas pruebas a una base
de producción renombrada con ese prefijo.

Se ejecutaron y aprobaron A–I: una acumulación, repetición, independencia por vehículo,
pendiente sin completar, cancelada, No asistió, consulta coherente, fallo simulado
con rollback y dos solicitudes concurrentes de la misma cita. También pasó completar
dos citas distintas del mismo vehículo sin perder incrementos. La primera ejecución
de esta prueba adicional detectó un interbloqueo; el código se corrigió y la ejecución
posterior terminó correctamente. Los datos de ambas ejecuciones permanecen identificados
como pruebas. Estos resultados no sustituyen la prueba sobre una copia del esquema
completo con sus CHECK ni la demostración con usuarios reales autorizados.

Con la aplicación iniciada:

```powershell
dotnet run --project WashLabs.csproj -- --urls http://127.0.0.1:5189
powershell -NoProfile -File tests/VerificarHttp.ps1
sqlcmd -S localhost -E -C -d WashLabsDB -b -i Database/VerificarAvance3.sql
```

Pasaron 18 rutas HTTP anónimas: páginas públicas 200, registros inexistentes 404 y
pantallas privadas 302 al acceso. Además pasaron 28 verificaciones HTTP autenticadas
en la base aislada: login/cookie por los tres roles, vistas privadas, rechazo del
mantenimiento al cliente, antifalsificación y cierre de sesión. Para estas pruebas
se renuevan únicamente los hashes de las tres cuentas sintéticas `@example.invalid`
y se inicia/cierra un servidor temporal; la contraseña aleatoria permanece en memoria.
No se ejecutó navegador ni revisión visual real;
no se generaron capturas. Dentro del sandbox hubo fallos de TLS/DPAPI; las consultas
y el servidor se ejecutaron fuera del sandbox con permisos concedidos. No se debilitó
la seguridad ni se modificó la configuración global para resolverlos.

## Pruebas manuales y capturas reales

Iniciar sesión por cada rol con credenciales autorizadas, capturar la pantalla real
y evitar datos privados. Revisar escritorio y móvil (por ejemplo, 390 px de ancho),
menú, botones, tablas desplazables, mensajes y estados vacíos.

| Evidencia | Ruta y pasos |
|---|---|
| Inicio | `/` y navegación según cada rol |
| Productos público | `/Productos`, filtrar categoría y abrir dos detalles distintos |
| Mantenimiento | `/Productos/Mantenimiento` y `DetalleMantenimiento/ID` con personal autorizado |
| Detalle producto | `/Productos/Detalle/ID`: categoría, imagen, precio, marca y presentación |
| Facturas | `/Facturacion` como administrador: cuatro documentos y distintos clientes |
| Facturas 1/2/3 líneas y mixta | `/Facturacion/Detalle/ID` para los cuatro registros; revisar encabezado, identificación aprobada, líneas, IVA y total |
| Reservas | `/Reservas` como cada encargado, administrador y cliente; mostrar diferencias |
| Detalle reserva | `/Reservas/Detalle/ID`: fechas, horas, estado/color y tres respuestas |
| IDs ajenos | Cambiar manualmente IDs con cliente/encargado: esperar 404; consultar un ID inexistente |
| Reserva nueva | `/Reservas/Crear`: elegir sucursal permitida, vehículo propio y horario disponible; usar base de pruebas o autorización para guardar |
| Vehículos | `/Vehiculos` y `/Vehiculos/Crear`; crear solo en pruebas o con aprobación |
| Acumulación | En pruebas, completar servicio desde el detalle de reserva y consultar `/WashPoints`; repetir y verificar que el contador no cambia |
| Asistente | `/Asistente` y widget: cinco categorías, opciones, respuesta final, reiniciar y error de red |
| Acceso y cierre | `/Cuenta/Entrar`; cliente no entra al mantenimiento; cerrar mediante el botón POST; privado vuelve a pedir acceso |

## Pendientes de entrega humana

- Revisar los cambios y comprender la transacción, sus bloqueos y los filtros SQL.
- Autorizar cuentas DEMO e identificaciones; probar sesiones completas y cerrar sesión.
- Hacer revisión visual, capturas auténticas y documento académico con explicación de pantallas.
- Preparar propuesta adicional, cronograma e informe; condición anterior al 15/10/2026
  y firma auténtica del profesor. El cronograma debe reflejar trabajo real, no fechas inventadas.
- Confirmar con el docente duración del video: plantilla 8–10 minutos frente a máximo 5.
- El historial local consultado contiene tres commits y un autor. Registrar las
  contribuciones reales de ambos integrantes para alcanzar al menos seis commits,
  sin cambiar autores ni fabricar aportes.
- Preparar video real, firmas digitales auténticas, comprimido sin secretos y autoevaluación.

No se generaron commits, capturas, firmas, constancias ni evidencias académicas simuladas.
