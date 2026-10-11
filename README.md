# WashLabs — EvoGroup

WashLabs es un proyecto de EvoGroup desarrollado con ASP.NET Core MVC y .NET 8. La base de datos prevista es SQL Server 2019 (WashLabsDB).

## Estado de esta etapa

Se prepararon la estructura, las referencias entre proyectos y Git. Entity Framework Core todavía no está instalado; no se ha configurado ni modificado la base de datos. Los módulos de productos, facturación y reservas quedan pendientes.

## Estructura

```text
WashLabs/
├── WashLabs.sln
├── WashLabs.csproj              # Aplicación MVC en la raíz
├── Controllers/
├── Models/                     # Modelos existentes de presentación
├── Views/
├── ViewModels/
├── Mappers/                    # ViewModel ↔ DTO cuando corresponda
├── wwwroot/
├── WashLabs.Data/
│   ├── Interfaces/
│   ├── Repositories/
│   ├── Context/
│   ├── Configurations/
│   └── DependencyInjection/
├── WashLabs.Models/
│   ├── Entities/
│   ├── DTOs/
│   └── Enums/
└── WashLabs.Services/
    ├── Interfaces/
    ├── Implementations/
    ├── Mappers/                 # Entidad ↔ DTO
    └── DependencyInjection/
```

Las carpetas preparadas usan `.gitkeep` para conservarse en Git. Los archivos existentes, incluidos `Class1.cs`, se mantienen. El proyecto web excluye los directorios de las bibliotecas de sus elementos implícitos para evitar compilación o publicación duplicada.

## Arquitectura

Vista → Controller → Service Interface → Service Implementation → Repository Interface → Repository Implementation → DbContext → SQL Server.

- WashLabs referencia WashLabs.Services y WashLabs.Models.
- WashLabs.Services referencia WashLabs.Data y WashLabs.Models.
- WashLabs.Data referencia WashLabs.Models.
- WashLabs.Models no referencia otros proyectos.

Los controllers consumirán interfaces de servicios; los servicios consumirán interfaces de repositorios. Solo las implementaciones de repositorios accederán al DbContext. Las consultas se realizarán mediante LINQ. Las referencias permiten componer las capas, pero estas reglas también deben respetarse en el código.

Los DTOs definirán entradas y salidas; los mappers transformarán datos. Las reglas de negocio pertenecerán a Services y la persistencia a Data. El registro de dependencias se incorporará cuando existan implementaciones.

## Desarrollo local

- Visual Studio 2022 con soporte para .NET 8 y la carga de trabajo ASP.NET y desarrollo web.
- SDK .NET 8 compatible con la versión de Visual Studio, o un SDK posterior capaz de compilar `net8.0`.
- SQL Server 2019 para las etapas posteriores de persistencia.

Abrir `WashLabs.sln` y seleccionar WashLabs como proyecto de inicio. Desde la raíz:

```powershell
dotnet sln WashLabs.sln list
dotnet build WashLabs.sln
dotnet run --project WashLabs.csproj
```

Los cuatro proyectos mantienen `TargetFramework` en `net8.0`. No se fija un SDK mediante `global.json` en esta etapa.

## Git y configuración local

El repositorio Git se ubica únicamente en la raíz de esta solución. Registrar progresivamente cada etapa revisada con commits pequeños. No se requiere un remoto para trabajar localmente.

`.gitignore` excluye bin, obj, .vs, configuración personal de Visual Studio, archivos temporales y nombres habituales de archivos con credenciales. No incluir contraseñas en archivos versionados: usar User Secrets o variables de entorno cuando se configure la conexión.

## Conservación de archivos

Las bibliotecas se copiaron desde sus ubicaciones hermanas originales y la solución referencia únicamente las copias internas. Los originales externos se conservaron porque están fuera del área de escritura de la sesión de preparación. No deben editarse como parte de esta solución.

El proyecto web permanece en la raíz debido a un bloqueo del sistema al trasladar Views. La carpeta anidada `WashLabs/`, que solo contiene bin y obj del intento anterior, conserva esos artefactos; está excluida del proyecto web y sus artefactos están ignorados por Git.

## Validación de preparación

Se verificaron los cuatro proyectos con `dotnet sln WashLabs.sln list` y se compiló la solución para `net8.0` con el SDK instalado 10.0.100: 0 errores y 0 advertencias. En la sesión automatizada fue necesario definir las rutas de entorno de Windows requeridas por NuGet únicamente para el proceso de compilación. No se cambió la configuración global ni el framework objetivo.
