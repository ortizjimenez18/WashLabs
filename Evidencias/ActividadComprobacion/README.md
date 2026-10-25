# Actividad de comprobación de usuarios · WashLabs

Universidad Técnica Nacional · María Paula Murillo Rosales · 20/10/2026.

Implementación sobre la rama `cambios-realizados-avance3`, con Git limpio antes
de comenzar. Se mantienen los cuatro proyectos de la solución .NET 8 y sus
dependencias. No se agregaron paquetes, migraciones, commits ni cambios de conexión.

## Acceso y demostración

1. Abrir `WashLabs.sln`, detener la instancia anterior si sigue abierta y ejecutar el proyecto web en Development con su conexión
   existente en User Secrets.
2. Iniciar sesión con una cuenta **Administrador** existente.
3. Abrir **Mi WashLabs → Usuarios** o `/Usuarios`.
4. Comprobar el encabezado exacto **Lista de Usuarios (María Paula Murillo Rosales)**.
5. Seleccionar **Ver detalle** y después **Volver al listado**.

El listado incluye cuentas activas e inactivas. Correo (`Correo`) y estado
(`Activo`) son columnas reales del modelo. Solo el administrador accede al
directorio completo; clientes y encargados reciben el flujo de acceso denegado
existente. Los visitantes deben iniciar sesión. Un identificador inexistente o
inválido devuelve HTTP 404, usando la página de error existente.

No hay que crear usuarios: la comprobación inicial en `WashLabsDB` encontró **9**,
todos con rol válido. No se insertaron registros ni se modificaron sus contraseñas.
Si en otro entorno faltaran usuarios, crear únicamente los faltantes en
`/Cuenta/Registrarse`, que usa `CuentaService`, `PasswordHasher<Usuario>` y el rol
Cliente existente. No se incluyen contraseñas de demostración.

## Capas existentes

| Capa requerida | Proyecto existente | Responsabilidad |
| --- | --- | --- |
| Infrastructure | `WashLabs.Data` | `IUsuarioRepository` y `UsuarioRepository` consultan `WashLabsDbContext`, `dbo.Usuarios` y `dbo.Roles`. EF traduce la navegación Usuario.Rol a un JOIN. Una consulta por operación, sin seguimiento y con proyección de campos permitidos. |
| Application | `WashLabs.Services` y `WashLabs.Models` | `IUsuarioService` y `UsuarioService` transforman los resultados en `UsuarioDTO` y validan identificadores. El DTO no contiene entidades, hashes ni tokens. |
| Web | `WashLabs` | `UsuariosController` aplica autorización de Administrador y evita caché de datos personales. Vistas Razor con DTO, layout y componentes existentes, CSS limitado al directorio. |

No se renombran proyectos ni se duplican las funciones de registro o autenticación.
El repositorio no carga PasswordHash, identificación, dirección ni fecha de
nacimiento para estas pantallas. El detalle agrega teléfono y fecha de registro.

## Archivos creados

- `Controllers/UsuariosController.cs`
- `WashLabs.Data/Interfaces/IUsuarioRepository.cs`
- `WashLabs.Data/Repositories/UsuarioRepository.cs`
- `WashLabs.Models/DTOs/UsuarioDTO.cs`
- `WashLabs.Services/Interfaces/IUsuarioService.cs`
- `WashLabs.Services/Implementations/UsuarioService.cs`
- `Views/Usuarios/Index.cshtml`
- `Views/Usuarios/Detalle.cshtml`
- `wwwroot/css/usuarios.css`
- `Database/Actividad_Comprobacion_Usuarios_MariaPaula.sql`
- `tests/WashLabs.Verificacion/PruebasNavegadorUsuarios.cs`
- `Evidencias/ActividadComprobacion/README.md`
- Capturas reales generadas por las pruebas de navegador en esta carpeta.

## Archivos modificados

- `WashLabs.Data/DependencyInjection/ServiceCollectionExtensions.cs`: repositorio.
- `WashLabs.Services/DependencyInjection/ServiceCollectionExtensions.cs`: servicio.
- `Views/Shared/_Layout.cshtml`: acceso desde el menú para Administrador.
- `tests/WashLabs.Verificacion/Program.cs`: comprobación `--usuarios` de solo lectura.
- `tests/WashLabs.Verificacion/PruebasHttpAutenticadas.cs`: permisos, datos, detalle,
  navegación y ejecución opcional de capturas de usuarios.

Los archivos nuevos se incluyen automáticamente en sus proyectos SDK; no se
necesitan cambios en `.sln` ni `.csproj` para abrirlos y compilarlos en Visual Studio.

## Script y repetición de comprobaciones

`Database/Actividad_Comprobacion_Usuarios_MariaPaula.sql` usa `WashLabsDB`, cuenta
usuarios, lista los cinco campos con JOIN y verifica roles válidos. Solo lectura:
no crea tablas ni inserta, elimina o actualiza datos. Si hay menos de tres usuarios
informa mediante THROW que deben registrarse por el flujo de aplicación.

```powershell
sqlcmd -S localhost -E -C -b -f 65001 -i Database/Actividad_Comprobacion_Usuarios_MariaPaula.sql
dotnet run --project tests/WashLabs.Verificacion -- --usuarios
```

La compilación normal encontró DLL bloqueadas por una instancia previa de la
aplicación. Se conservó esa instancia y se compiló correctamente la solución con
salida independiente, sin errores ni advertencias:

```powershell
dotnet build WashLabs.sln --no-restore -p:OutDir=C:/Users/mpaul/source/repos/WashLabs/bin/ActividadComprobacion/
dotnet build tests/WashLabs.Verificacion --no-restore
```

El comando `--usuarios` aprobó **28 comprobaciones**, incluyendo las ocho existentes
de autenticación y mapeo: nueve usuarios reales, todos sus roles, nombres, correos
y estados coincidentes con SQL, sus detalles e identificadores inexistentes.
El script SQL también se ejecutó correctamente contra `WashLabsDB`.

La ejecución final de `--http-autenticado` aprobó **99 verificaciones HTTP y de
navegador**: autenticación de los tres roles, rechazo de anónimos, permisos del
directorio y del menú, título exacto, cuatro filas reales (tres activas y una
inactiva), campos y roles por fila, enlace de detalle y regreso, ausencia de hashes
en HTML, HTTP 404, diseño a 1366 y 390 px y cuatro capturas. También aprobaron
los flujos existentes comprobados de reservas, productos en facturación,
finalización de citas, acreditación de WashPoints, filtros, impresión y logout.
No se afirma una comprobación exhaustiva de todos los módulos del sistema.

La primera ejecución detectó que la prueba contaba solo las cuentas activas,
mientras el requisito exige todos los usuarios. Se corrigió la expectativa de la
prueba para incluir el usuario inactivo existente; el listado ya lo incluía.
`git diff --check` terminó sin incidencias.

Para las pruebas HTTP y capturas se usa exclusivamente la base ya existente
`WashLabs_Pruebas_Avance3`. El verificador exige esa base y sus tres cuentas
sintéticas activas. Renueva sus hashes con claves aleatorias en memoria y realiza
los flujos existentes de reservas y facturación sobre datos de prueba; conserva
esos resultados en la base aislada. No ejecutarlo apuntando a datos reales.

```powershell
$env:WASHLABS_TEST_CONNECTION='Server=localhost;Database=WashLabs_Pruebas_Avance3;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15'
$env:WASHLABS_TEST_APP_PATH='bin/ActividadComprobacion/WashLabs.dll'
$env:WASHLABS_VISUAL_TESTS='1'
dotnet run --project tests/WashLabs.Verificacion --no-build -- --http-autenticado
```

Las capturas se toman con Microsoft Edge headless y su protocolo de depuración,
reutilizando el método del Avance 3, sin dependencias adicionales ni sesiones del
usuario. Muestran **datos reales de SQL Server de la base aislada**, incluidos los
usuarios inactivos que existan allí; no son imágenes simuladas ni corresponden a
los nueve usuarios de `WashLabsDB`.

- `ListaUsuarios.png`: escritorio, 1366 px.
- `DetalleUsuario.png`: escritorio, 1366 px.
- `ListaUsuarios-movil.png`: móvil, 390 px, tabla con desplazamiento interno.
- `DetalleUsuario-movil.png`: móvil, 390 px.

Para capturar los nueve usuarios del entorno principal, iniciar sesión como
Administrador en ese entorno y tomar una captura de `/Usuarios`. Para ejecutar la
compilación normal en la carpeta Debug, cerrar primero la instancia previa que
mantiene sus DLL bloqueadas. No se requiere hacerlo para usar la salida independiente.
