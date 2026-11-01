# Cambios visuales del catálogo — 31 de octubre de 2026

Se elimina únicamente el prefijo inicial exacto `A4 Paula - ` al presentar nombres de productos y servicios. La normalización también cubre los nombres de servicios en reservas y el inicio de las líneas históricas de comprobantes. No se modifican entidades ni descripciones almacenadas.

Los importes utilizan `.washlabs-price`, con variables `--washlabs-price-color: #007C9E` y `--washlabs-price-color-dark-bg: #66D9F0`. La segunda se aplica a las tarjetas oscuras existentes. Se conservan tamaños, distribución y formato `es-CR`, sin cambiar cantidades, impuestos, descuentos ni cálculos. En el selector de productos de proformas, el importe seleccionado se presenta debajo del selector para colorear solo el dinero; se incluye una alternativa sin JavaScript.

## Archivos modificados o agregados

- `wwwroot/css/precios.css`: variables y reglas monetarias compartidas.
- `wwwroot/js/facturacion.js`: actualización del precio seleccionado.
- `Views/Shared/_Layout.cshtml`: carga de los estilos monetarios después de los estilos de página.
- `Views/Shared/_ProductoCard.cshtml`: importe de tarjeta.
- `Views/Home/Index.cshtml`: importes de servicios claros y oscuros.
- `Views/Productos/Detalle.cshtml`: precio del detalle.
- `Views/Productos/Mantenimiento.cshtml`: importes del listado.
- `Views/Servicios/Index.cshtml`: tarifas y formato de moneda costarricense.
- `Views/Servicios/Detalle.cshtml`: tarifa y formato de moneda costarricense.
- `Views/Reservas/Crear.cshtml`: nombres del selector de servicios.
- `Views/Facturacion/Index.cshtml`: total facturado e importes del historial.
- `Views/Facturacion/Detalle.cshtml`: precios unitarios, impuestos, descuentos, subtotales, totales, pagos, saldos y precio seleccionado.
- `WashLabs.Services/Mappers/NombreCatalogo.cs`: normalización exacta reutilizable.
- `WashLabs.Services/Mappers/ProductoMapper.cs`: nombres de productos presentados.
- `WashLabs.Services/Mappers/ServicioMapper.cs`: nombres de servicios presentados.
- `WashLabs.Services/Mappers/InicioMapper.cs`: servicios de inicio.
- `WashLabs.Services/Mappers/ReservaMapper.cs`: servicios en historial y detalle de reservas.
- `WashLabs.Services/Mappers/FacturaMapper.cs`: presentación de líneas históricas.
- `Database/Avance4/Paula/04_LimpiarNombresCatalogo.sql`: corrección persistente pendiente de ejecución.
- `tests/WashLabs.Verificacion/Program.cs`: pruebas unitarias y acceso a la verificación de presentación.
- `tests/WashLabs.Verificacion/PruebasPresentacion.cs`: comprobación HTTP y de colores calculados en Edge sin escrituras SQL.
- `docs/cambios-visuales-avance4.md`: este informe.

## SQL Server

La consulta de solo lectura a `WashLabsDB` confirmó cuatro servicios y cuatro productos con el prefijo. Coinciden con el origen identificado en `Database/Avance4/Paula/02_DatosPrueba.sql`.

**No se modificaron datos de SQL Server.** El script `04_LimpiarNombresCatalogo.sql` queda pendiente de ejecución manual. Es transaccional e idempotente, comprueba la base de destino, compara el prefijo inicial exacto y modifica únicamente `Nombre` en `Servicios` y `Productos`. Muestra identificador y nombre anterior/nuevo; cualquier error revierte la transacción. No inserta ni elimina registros. Si una restricción de unicidad impide una corrección, revierte sin fusionar registros.

El script histórico de carga de datos permanece intacto. La normalización de presentación evita que el prefijo vuelva a mostrarse si se reutilizan esos datos de prueba.

## Verificación ejecutada

- `dotnet build WashLabs.sln --no-restore -p:OutputPath=bin/RevisionVisual/`: compilación correcta, cero errores y cero advertencias. La compilación inicial en la carpeta predeterminada encontró DLL ocupadas por una instancia existente; se utilizó una salida separada sin detenerla.
- `dotnet run --project tests/WashLabs.Verificacion --no-restore -- --solo-unitarias`: 20 pruebas aprobadas, incluidas eliminación exacta del prefijo y conservación de otros nombres, entidad, precio y descripción.
- `dotnet run --project tests/WashLabs.Verificacion --no-restore -- --presentacion`: 14 rutas verificadas con respuestas HTTP 200, ausencia del prefijo y carga de estilos. Edge comprobó los colores calculados y que todos los textos monetarios estuvieran marcados, usando copias efímeras de las vistas renderizadas con los estilos originales.
- Rutas revisadas: inicio; catálogo; detalle de producto; filtros por nombre y categoría; búsqueda sin coincidencias; listado y detalle de servicios; historial, creación y detalle de reservas; historial y detalle de facturación; proforma con selector de productos.
- El selector de proforma se comprobó cambiando la opción y verificando el importe mostrado. Las vistas autenticadas se consultaron con una sesión local de administrador generada por el arnés de pruebas; no se cambiaron contraseñas ni permisos.
- Las verificaciones de presentación realizaron únicamente lecturas SQL y solicitudes GET. No se crearon reservas, facturas ni productos. No se ejecutaron operaciones de facturación ni pruebas de escritura sobre datos reales.
- No se realizó una revisión manual por capturas ni de todos los registros existentes; la comprobación de presentación fue automatizada sobre las rutas indicadas.

Para repetir `--presentacion`, iniciar previamente la compilación actualizada en `http://127.0.0.1:5197`, en entorno Development, desde la raíz del repositorio. Requiere Edge, SQL Server local, la conexión ya configurada y acceso a las claves locales de ASP.NET. No se alteraron conexiones, cálculos, repositorios ni filtros del catálogo.

No se realizaron commits ni push.
