# Imágenes de productos WashLabs

Se generaron cuatro imágenes originales de estilo fotográfico con la herramienta integrada de generación de imágenes. Se inspeccionaron y optimizaron a JPEG, calidad 90, 1024 × 1024 píxeles. Los cuatro originales PNG permanecen en el directorio de imágenes generadas de Codex; los archivos utilizados por la aplicación están físicamente en `wwwroot`.

| ID real | SKU | Nombre mostrado | Archivo | Tamaño |
| --- | --- | --- | --- | --- |
| 12 | A4-PAULA-001 | Shampoo | `wwwroot/images/productos/washlabs-shampoo.jpg` | 82 124 bytes |
| 13 | A4-PAULA-002 | Cera | `wwwroot/images/productos/washlabs-cera.jpg` | 91 844 bytes |
| 14 | A4-PAULA-003 | Aromatizante | `wwwroot/images/productos/washlabs-aromatizante.jpg` | 75 328 bytes |
| 15 | A4-PAULA-004 | Microfibra | `wwwroot/images/productos/washlabs-microfibra.jpg` | 180 201 bytes |

Las cuatro imágenes utilizan envases o presentación negros, marca WASHLABS plateada, fondo gris claro, iluminación suave y sombras de estudio. Se tomó como referencia visual el shampoo existente del proyecto. La cera es un tarro de perfil bajo; el aromatizante tiene atomizador; las microfibras incluyen paños negros y grises con banda de marca. No se descargaron imágenes de terceros ni se reemplazaron archivos existentes.

## Integración

- `Mappers/ImagenProductoMapper.cs`: asociación de respaldo por pareja exacta ID/SKU. No depende de los nombres ni de si se ejecutó la limpieza del prefijo.
- `Views/Shared/_ProductoImagen.cshtml`: conserva una imagen local válida configurada. Si falta, es inválida o es el marcador `sin-imagen.svg`, utiliza la imagen nueva correspondiente y comprueba que exista. Agrega versión de caché con `asp-append-version`.
- Se mantienen los contenedores, tamaños y reglas existentes de `object-fit: contain`, sin cambios de CSS, precios, modelos de persistencia, consultas, reservas ni facturación.
- Las tarjetas del catálogo y del inicio y los detalles ya comparten este parcial. El inicio conserva su selección existente de tres productos; se verificó que el aromatizante que aparece actualmente allí utiliza la nueva imagen.
- `Database/Avance4/Paula/05_ImagenesProductosWashLabs.sql`: script opcional para persistir las cuatro rutas. Comprueba base, ID, SKU y nombre (con o sin prefijo), actualiza exclusivamente `ImagenUrl` cuando está vacía o es el marcador conocido, y revierte la transacción ante errores. No reemplaza rutas personalizadas, no inserta ni elimina productos y es idempotente.

**No se modificaron datos SQL. No hace falta ejecutar el script para visualizar las imágenes en esta versión de la aplicación.** El script sirve para guardar las rutas también en la base de datos.

## Verificaciones realizadas

- `dotnet build WashLabs.sln --no-restore -p:OutputPath=bin/ImagenesCatalogo/`: cero errores y cero advertencias. Se usa una salida separada para respetar la instancia existente que ocupa las DLL de Debug.
- `dotnet run --project tests/WashLabs.Verificacion --no-restore -- --imagenes-catalogo`: 31 comprobaciones aprobadas, incluidas las ocho comprobaciones unitarias iniciales del arnés existente.
- Los cuatro detalles generan la ruta correcta con versión y no muestran el marcador de imagen ausente. Los cuatro archivos se sirven por HTTP 200 con tipo `image/jpeg` y contenido JPEG válido.
- Catálogo, filtros por nombre/categoría y búsqueda sin resultados comprobados. El inicio conserva sus tarjetas y colores de precios.
- Edge navegó al catálogo real con emulación explícita de viewport de 1440, 768 y 390 píxeles. Verificó `naturalWidth` y `naturalHeight` de 1024, correspondencia imagen/producto, carga de las cuatro imágenes, `object-fit: contain`, ausencia de marcadores, precios `#007C9E` y ausencia de desbordamiento horizontal.
- Capturas guardadas en `artifacts/imagenes-catalogo/catalogo-1440.png`, `catalogo-768.png` y `catalogo-390.png`.
- Verificación mediante lecturas y solicitudes GET; no se alteraron datos reales.

## Archivos de esta tarea

Además de las cuatro imágenes, el mapper, el parcial y el script SQL anteriores:

- `tests/WashLabs.Verificacion/Program.cs`: opción `--imagenes-catalogo`.
- `tests/WashLabs.Verificacion/PruebasImagenesCatalogo.cs`: comprobaciones de rutas, imágenes y presentación.
- `tests/WashLabs.Verificacion/NavegadorCatalogo.cs`: arnés Edge mediante CDP, sin dependencias nuevas.
- `docs/prompts-imagenes-productos.md`: prompts finales de las cuatro imágenes.
- `docs/imagenes-productos-washlabs.md`: este informe.

Para repetir las pruebas, iniciar la compilación actualizada desde la raíz del repositorio en Development en `http://127.0.0.1:5197`, usando la conexión existente. Requiere Microsoft Edge y acceso al SQL Server configurado.

Se conservaron los cambios de la tarea anterior. No se realizaron commits ni push.
