-- WashLabs / Avance 3. Seed idempotente de productos e ImagenUrl.
-- SQL Server 2019. Precios de demostracion en CRC, no cotizaciones comerciales.
-- Solo agrega ImagenUrl e inserta productos ausentes. No actualiza ni elimina filas.
USE [WashLabsDB];
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'WashLabsDB'
    THROW 51000, N'La base de destino debe ser WashLabsDB.', 1;
IF @@TRANCOUNT <> 0
    THROW 51001, N'Ejecute este script sin una transaccion previa abierta.', 1;
IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
   OR OBJECT_ID(N'dbo.CategoriasProducto', N'U') IS NULL
    THROW 51002, N'No se encontraron las tablas requeridas.', 1;

CREATE TABLE #SeedProductosAvance3
(
    SKU varchar(50) NOT NULL PRIMARY KEY,
    Categoria nvarchar(80) NOT NULL,
    Nombre nvarchar(120) NOT NULL,
    Descripcion nvarchar(700) NOT NULL,
    Precio decimal(18, 2) NOT NULL,
    Marca nvarchar(80) NOT NULL,
    Presentacion nvarchar(100) NOT NULL,
    ImagenUrl nvarchar(500) NOT NULL
);

INSERT INTO #SeedProductosAvance3
    (SKU, Categoria, Nombre, Descripcion, Precio, Marca, Presentacion, ImagenUrl)
VALUES
('WL-SHP-001', N'Shampoo', N'Shampoo automotriz pH neutro',
 N'Shampoo concentrado para lavado manual de carrocería; remueve suciedad y facilita el enjuague.',
 5500.00, N'WashLabs', N'Botella de 500 ml', N'/images/productos/shampoo-ph-neutro.png'),
('WL-CER-001', N'Ceras y selladores', N'Cera líquida de carnauba',
 N'Cera líquida para proteger la pintura y mejorar el brillo después del lavado y secado.',
 8500.00, N'WashLabs', N'Botella de 500 ml', N'/images/productos/cera-carnauba.png'),
('WL-ARO-001', N'Aromatizantes', N'Aromatizante para interior cítrico',
 N'Aromatizante en atomizador para refrescar el ambiente del habitáculo con fragancia cítrica.',
 3000.00, N'WashLabs', N'Atomizador de 120 ml', N'/images/productos/aromatizante-citrico.png'),
('WL-MIC-001', N'Microfibras y accesorios', N'Pack de microfibras multiuso',
 N'Tres paños de microfibra de 40 x 40 cm para limpieza y acabado de superficies del vehículo.',
 4500.00, N'WashLabs', N'Paquete de 3 unidades', N'/images/productos/microfibras-pack-3.png'),
('WL-LLA-001', N'Llantas y plásticos', N'Limpiador de llantas y aros',
 N'Limpiador para desprender suciedad de llantas y aros con ayuda de un cepillo y posterior enjuague.',
 6000.00, N'WashLabs', N'Atomizador de 500 ml', N'/images/productos/limpiador-llantas-aros.png'),
('WL-LLA-002', N'Llantas y plásticos', N'Abrillantador de llantas',
 N'Acabado satinado para los costados de las llantas; aplicar con espuma sobre la superficie limpia y seca.',
 6500.00, N'WashLabs', N'Botella de 500 ml', N'/images/productos/abrillantador-llantas.png');

BEGIN TRY
    BEGIN TRANSACTION;

    -- Serializa ejecuciones simultaneas de este mismo seed.
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'WashLabs.SeedAvance3', @LockMode = N'Exclusive',
        @LockOwner = N'Transaction', @LockTimeout = 15000;
    IF @LockResult < 0
        THROW 51003, N'No se pudo obtener el bloqueo del seed.', 1;

    -- Reutiliza las categorias existentes por nombre, sin asumir sus IDs.
    IF EXISTS
    (
        SELECT 1 FROM #SeedProductosAvance3 s
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.CategoriasProducto c WITH (UPDLOCK, HOLDLOCK)
            WHERE c.Nombre = s.Categoria AND c.Activa = 1
        )
    )
        THROW 51004, N'Falta una categoria activa requerida; no se crearan categorias automaticamente.', 1;

    IF COL_LENGTH(N'dbo.Productos', N'ImagenUrl') IS NULL
        ALTER TABLE dbo.Productos ADD ImagenUrl nvarchar(500) NULL;
    ELSE IF NOT EXISTS
    (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Productos') AND name = N'ImagenUrl'
          AND system_type_id = TYPE_ID(N'nvarchar') AND max_length = 1000
          AND is_nullable = 1 AND is_computed = 0
    )
        THROW 51005, N'ImagenUrl ya existe con una definicion diferente de NVARCHAR(500) NULL.', 1;

    -- No reutiliza un SKU que ahora pertenezca a otro producto.
    IF EXISTS
    (
        SELECT 1 FROM #SeedProductosAvance3 s
        JOIN dbo.CategoriasProducto c ON c.Nombre = s.Categoria
        JOIN dbo.Productos p WITH (UPDLOCK, HOLDLOCK) ON p.SKU = s.SKU
        WHERE p.Nombre <> s.Nombre OR p.CategoriaProductoId <> c.CategoriaProductoId
           OR p.EsPersonalizable = 1
    )
        THROW 51006, N'Un SKU del seed ya pertenece a otro producto; revise el conflicto.', 1;

    -- SQL dinamico: compila la referencia a ImagenUrl DESPUES de agregarla.
    -- Solo usa texto constante y la tabla temporal; no concatena entradas externas.
    DECLARE @Insertados int;
    EXEC sys.sp_executesql N'
        INSERT INTO dbo.Productos
            (CategoriaProductoId, Nombre, Descripcion, Precio, Marca, Presentacion,
             SKU, EsPersonalizable, Activo, ImagenUrl)
        SELECT c.CategoriaProductoId, s.Nombre, s.Descripcion, s.Precio,
               s.Marca, s.Presentacion, s.SKU, 0, 1, s.ImagenUrl
        FROM #SeedProductosAvance3 s
        JOIN dbo.CategoriasProducto c ON c.Nombre = s.Categoria
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.Productos p WITH (UPDLOCK, HOLDLOCK)
            WHERE p.SKU = s.SKU
               OR (p.CategoriaProductoId = c.CategoriaProductoId AND p.Nombre = s.Nombre)
        );
        SET @Cantidad = @@ROWCOUNT;',
        N'@Cantidad int OUTPUT', @Cantidad = @Insertados OUTPUT;

    COMMIT TRANSACTION;
    SELECT @Insertados AS ProductosInsertados,
           6 - @Insertados AS ProductosYaExistentes;
    DROP TABLE #SeedProductosAvance3;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    DROP TABLE IF EXISTS #SeedProductosAvance3;
    THROW;
END CATCH;
