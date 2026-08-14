/* Módulo de proveedores y compras de LessPrice.
   Este script es incremental, conserva los productos actuales y puede ejecutarse varias veces. */

USE [ProyectoEcommerceDB];
GO

SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Proveedores', N'U') IS NULL
    THROW 51000, 'Debe ejecutar primero ProyectoEcommerceDB_AmpliacionImportante.sql.', 1;
GO

IF COL_LENGTH(N'dbo.Proveedores', N'UrlImagen') IS NULL
    ALTER TABLE dbo.Proveedores ADD UrlImagen NVARCHAR(500) NULL;
GO

IF COL_LENGTH(N'dbo.Proveedores', N'FechaRegistro') IS NULL
    ALTER TABLE dbo.Proveedores ADD FechaRegistro DATETIME2(3) NOT NULL
        CONSTRAINT DF_Proveedores_FechaRegistro DEFAULT (SYSDATETIME());
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Proveedores')
      AND name = N'UX_Proveedores_Nombre'
)
    CREATE UNIQUE INDEX UX_Proveedores_Nombre ON dbo.Proveedores (Nombre);
GO

IF OBJECT_ID(N'dbo.ProveedorFamilias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProveedorFamilias
    (
        ProveedorFamiliaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProveedorFamilias PRIMARY KEY,
        ProveedorId INT NOT NULL,
        FamiliaId INT NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_ProveedorFamilias_Activo DEFAULT (1),
        CONSTRAINT UQ_ProveedorFamilias_Proveedor_Familia UNIQUE (ProveedorId, FamiliaId),
        CONSTRAINT FK_ProveedorFamilias_Proveedores FOREIGN KEY (ProveedorId)
            REFERENCES dbo.Proveedores (ProveedorId),
        CONSTRAINT FK_ProveedorFamilias_Familias FOREIGN KEY (FamiliaId)
            REFERENCES dbo.FamiliasProducto (FamiliaId)
    );
    CREATE INDEX IX_ProveedorFamilias_FamiliaId ON dbo.ProveedorFamilias (FamiliaId);
END;
GO

IF OBJECT_ID(N'dbo.ProveedorCategorias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProveedorCategorias
    (
        ProveedorCategoriaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProveedorCategorias PRIMARY KEY,
        ProveedorId INT NOT NULL,
        CategoriaId INT NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_ProveedorCategorias_Activo DEFAULT (1),
        CONSTRAINT UQ_ProveedorCategorias_Proveedor_Categoria UNIQUE (ProveedorId, CategoriaId),
        CONSTRAINT FK_ProveedorCategorias_Proveedores FOREIGN KEY (ProveedorId)
            REFERENCES dbo.Proveedores (ProveedorId),
        CONSTRAINT FK_ProveedorCategorias_Categorias FOREIGN KEY (CategoriaId)
            REFERENCES dbo.Categorias (CategoriaId)
    );
    CREATE INDEX IX_ProveedorCategorias_CategoriaId ON dbo.ProveedorCategorias (CategoriaId);
END;
GO

IF OBJECT_ID(N'dbo.ProductosProveedorCatalogo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductosProveedorCatalogo
    (
        ProductoProveedorCatalogoId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProductosProveedorCatalogo PRIMARY KEY,
        ProveedorCategoriaId INT NOT NULL,
        ImpuestoId INT NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        PrecioCompra DECIMAL(18,2) NOT NULL,
        ProductoId INT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_ProductosProveedorCatalogo_Activo DEFAULT (1),
        FechaActualizacion DATETIME2(3) NOT NULL
            CONSTRAINT DF_ProductosProveedorCatalogo_Fecha DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_ProductosProveedorCatalogo_Categoria_Nombre
            UNIQUE (ProveedorCategoriaId, Nombre),
        CONSTRAINT FK_ProductosProveedorCatalogo_ProveedorCategorias
            FOREIGN KEY (ProveedorCategoriaId)
            REFERENCES dbo.ProveedorCategorias (ProveedorCategoriaId),
        CONSTRAINT FK_ProductosProveedorCatalogo_Productos
            FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
        CONSTRAINT FK_ProductosProveedorCatalogo_Impuestos
            FOREIGN KEY (ImpuestoId) REFERENCES dbo.Impuestos (ImpuestoId),
        CONSTRAINT CK_ProductosProveedorCatalogo_Nombre
            CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0),
        CONSTRAINT CK_ProductosProveedorCatalogo_Precio CHECK (PrecioCompra >= 0)
    );
    CREATE INDEX IX_ProductosProveedorCatalogo_ProductoId
        ON dbo.ProductosProveedorCatalogo (ProductoId) WHERE ProductoId IS NOT NULL;
    CREATE INDEX IX_ProductosProveedorCatalogo_ImpuestoId
        ON dbo.ProductosProveedorCatalogo (ImpuestoId);
    CREATE INDEX IX_ProductosProveedorCatalogo_Disponibles
        ON dbo.ProductosProveedorCatalogo (ProveedorCategoriaId, Activo, ProductoId);
END;
GO

IF COL_LENGTH(N'dbo.ProductosProveedorCatalogo', N'ImpuestoId') IS NULL
    ALTER TABLE dbo.ProductosProveedorCatalogo ADD ImpuestoId INT NULL;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ProductosProveedorCatalogo')
      AND name = N'ImpuestoId'
      AND is_nullable = 1
)
BEGIN
    DECLARE @ImpuestoPredeterminado INT = COALESCE
    (
        (SELECT ImpuestoId FROM dbo.Impuestos WHERE Nombre = N'IVA 13%'),
        (SELECT TOP (1) ImpuestoId FROM dbo.Impuestos WHERE Activo = 1 ORDER BY ImpuestoId)
    );

    IF @ImpuestoPredeterminado IS NULL
        THROW 51006, 'Debe existir al menos un impuesto activo antes de instalar proveedores.', 1;

    UPDATE oferta
    SET ImpuestoId = COALESCE(producto.ImpuestoId, @ImpuestoPredeterminado)
    FROM dbo.ProductosProveedorCatalogo oferta
    LEFT JOIN dbo.Productos producto ON producto.ProductoId = oferta.ProductoId
    WHERE oferta.ImpuestoId IS NULL;

    ALTER TABLE dbo.ProductosProveedorCatalogo ALTER COLUMN ImpuestoId INT NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ProductosProveedorCatalogo_Impuestos'
)
    ALTER TABLE dbo.ProductosProveedorCatalogo WITH CHECK
        ADD CONSTRAINT FK_ProductosProveedorCatalogo_Impuestos
        FOREIGN KEY (ImpuestoId) REFERENCES dbo.Impuestos (ImpuestoId);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ProductosProveedorCatalogo')
      AND name = N'IX_ProductosProveedorCatalogo_ImpuestoId'
)
    CREATE INDEX IX_ProductosProveedorCatalogo_ImpuestoId
        ON dbo.ProductosProveedorCatalogo (ImpuestoId);
GO

IF COL_LENGTH(N'dbo.ComprasProveedor', N'Numero') IS NULL
    ALTER TABLE dbo.ComprasProveedor ADD Numero NVARCHAR(40) NULL;
GO

UPDATE dbo.ComprasProveedor
SET Numero = CONCAT(N'COMP-', RIGHT(CONCAT(N'000000', CompraProveedorId), 6))
WHERE Numero IS NULL;
GO

ALTER TABLE dbo.ComprasProveedor ALTER COLUMN Numero NVARCHAR(40) NOT NULL;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ComprasProveedor')
      AND name = N'UX_ComprasProveedor_Numero'
)
    CREATE UNIQUE INDEX UX_ComprasProveedor_Numero ON dbo.ComprasProveedor (Numero);
GO

IF COL_LENGTH(N'dbo.ComprasProveedor', N'UsuarioId') IS NULL
    ALTER TABLE dbo.ComprasProveedor ADD UsuarioId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComprasProveedor_Usuarios')
    ALTER TABLE dbo.ComprasProveedor WITH CHECK ADD CONSTRAINT FK_ComprasProveedor_Usuarios
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId);
GO

IF COL_LENGTH(N'dbo.ComprasProveedor', N'FechaConfirmacion') IS NULL
    ALTER TABLE dbo.ComprasProveedor ADD FechaConfirmacion DATETIME2(3) NULL;
GO

IF COL_LENGTH(N'dbo.ComprasProveedor', N'ClaveConfirmacion') IS NULL
    ALTER TABLE dbo.ComprasProveedor ADD ClaveConfirmacion UNIQUEIDENTIFIER NULL;
GO

UPDATE dbo.ComprasProveedor SET ClaveConfirmacion = NEWID() WHERE ClaveConfirmacion IS NULL;
GO

ALTER TABLE dbo.ComprasProveedor ALTER COLUMN ClaveConfirmacion UNIQUEIDENTIFIER NOT NULL;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ComprasProveedor')
      AND name = N'UX_ComprasProveedor_ClaveConfirmacion'
)
    CREATE UNIQUE INDEX UX_ComprasProveedor_ClaveConfirmacion
        ON dbo.ComprasProveedor (ClaveConfirmacion);
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ComprasProveedor_Estado')
    ALTER TABLE dbo.ComprasProveedor DROP CONSTRAINT CK_ComprasProveedor_Estado;
GO

UPDATE dbo.ComprasProveedor SET Estado = N'CONFIRMADA' WHERE Estado = N'RECIBIDA';
GO

ALTER TABLE dbo.ComprasProveedor WITH CHECK ADD CONSTRAINT CK_ComprasProveedor_Estado
    CHECK (Estado IN (N'PENDIENTE', N'CONFIRMADA', N'CANCELADA'));
GO

IF COL_LENGTH(N'dbo.CompraProveedorDetalle', N'ProductoProveedorCatalogoId') IS NULL
    ALTER TABLE dbo.CompraProveedorDetalle ADD ProductoProveedorCatalogoId INT NULL;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_CompraProveedorDetalle_Oferta'
)
    ALTER TABLE dbo.CompraProveedorDetalle WITH CHECK ADD CONSTRAINT FK_CompraProveedorDetalle_Oferta
        FOREIGN KEY (ProductoProveedorCatalogoId)
        REFERENCES dbo.ProductosProveedorCatalogo (ProductoProveedorCatalogoId);
GO

IF COL_LENGTH(N'dbo.CompraProveedorDetalle', N'NombreProducto') IS NULL
    ALTER TABLE dbo.CompraProveedorDetalle ADD NombreProducto NVARCHAR(120) NULL;
GO

UPDATE detalle
SET NombreProducto = producto.Nombre
FROM dbo.CompraProveedorDetalle detalle
INNER JOIN dbo.Productos producto ON producto.ProductoId = detalle.ProductoId
WHERE detalle.NombreProducto IS NULL;
GO

ALTER TABLE dbo.CompraProveedorDetalle ALTER COLUMN NombreProducto NVARCHAR(120) NOT NULL;
GO

IF COL_LENGTH(N'dbo.CompraProveedorDetalle', N'Subtotal') IS NULL
    ALTER TABLE dbo.CompraProveedorDetalle ADD Subtotal DECIMAL(18,2) NULL;
GO

UPDATE dbo.CompraProveedorDetalle
SET Subtotal = ROUND(PrecioUnitario * Cantidad, 2)
WHERE Subtotal IS NULL;
GO

ALTER TABLE dbo.CompraProveedorDetalle ALTER COLUMN Subtotal DECIMAL(18,2) NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CompraProveedorDetalle_Subtotal')
    ALTER TABLE dbo.CompraProveedorDetalle WITH CHECK ADD CONSTRAINT CK_CompraProveedorDetalle_Subtotal
        CHECK (Subtotal >= 0);
GO

IF COL_LENGTH(N'dbo.MovimientosInventario', N'CompraProveedorId') IS NULL
    ALTER TABLE dbo.MovimientosInventario ADD CompraProveedorId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MovimientosInventario_ComprasProveedor')
    ALTER TABLE dbo.MovimientosInventario WITH CHECK ADD CONSTRAINT FK_MovimientosInventario_ComprasProveedor
        FOREIGN KEY (CompraProveedorId) REFERENCES dbo.ComprasProveedor (CompraProveedorId);
GO

IF COL_LENGTH(N'dbo.MovimientosInventario', N'StockAnterior') IS NULL
    ALTER TABLE dbo.MovimientosInventario ADD StockAnterior INT NULL;
GO

IF COL_LENGTH(N'dbo.MovimientosInventario', N'StockNuevo') IS NULL
    ALTER TABLE dbo.MovimientosInventario ADD StockNuevo INT NULL;
GO

IF OBJECT_ID(N'dbo.SecuenciaCodigoProducto', N'SO') IS NULL
    CREATE SEQUENCE dbo.SecuenciaCodigoProducto AS BIGINT START WITH 1 INCREMENT BY 1;
GO

IF OBJECT_ID(N'dbo.SecuenciaCompraProveedor', N'SO') IS NULL
    CREATE SEQUENCE dbo.SecuenciaCompraProveedor AS BIGINT START WITH 1 INCREMENT BY 1;
GO

DECLARE @Proveedores TABLE
(
    Nombre NVARCHAR(150),
    Correo NVARCHAR(150),
    Telefono NVARCHAR(30),
    Direccion NVARCHAR(250),
    UrlImagen NVARCHAR(500),
    Familia NVARCHAR(80)
);

INSERT @Proveedores VALUES
(N'Dos Pinos', N'ventas@dospinos.com', N'2437-3000', N'Alajuela, Costa Rica', N'proveedores/alimentos.svg', N'Alimentos y bebidas'),
(N'Suministros Casa Tica', N'pedidos@casatica.cr', N'2202-2202', N'Alajuela, Costa Rica', N'proveedores/hogar.svg', N'Hogar y limpieza'),
(N'Textiles y Calzado del Istmo', N'comercial@textilesistmo.cr', N'2203-3303', N'San José, Costa Rica', N'proveedores/ropa.svg', N'Ropa y accesorios'),
(N'Tecnología Pura Vida', N'pedidos@tecnologiapuravida.cr', N'2204-4404', N'Cartago, Costa Rica', N'proveedores/electronica.svg', N'Electrónica'),
(N'Bienestar Costarricense', N'ventas@bienestarcr.cr', N'2205-5505', N'Puntarenas, Costa Rica', N'proveedores/cuidado.svg', N'Cuidado personal');

UPDATE proveedor
SET Correo = datos.Correo,
    Telefono = datos.Telefono,
    Direccion = datos.Direccion,
    UrlImagen = COALESCE(proveedor.UrlImagen, datos.UrlImagen),
    Activo = 1
FROM dbo.Proveedores proveedor
INNER JOIN @Proveedores datos
    ON datos.Nombre = proveedor.Nombre
    OR datos.Correo = proveedor.Correo;

INSERT dbo.Proveedores (Nombre, Correo, Telefono, Direccion, UrlImagen, Activo)
SELECT Nombre, Correo, Telefono, Direccion, UrlImagen, 1
FROM @Proveedores datos
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Proveedores proveedor
    WHERE proveedor.Nombre = datos.Nombre OR proveedor.Correo = datos.Correo
);

INSERT dbo.ProveedorFamilias (ProveedorId, FamiliaId, Activo)
SELECT proveedor.ProveedorId, familia.FamiliaId, 1
FROM @Proveedores datos
INNER JOIN dbo.Proveedores proveedor ON proveedor.Nombre = datos.Nombre
INNER JOIN dbo.FamiliasProducto familia ON familia.Nombre = datos.Familia
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ProveedorFamilias relacion
    WHERE relacion.ProveedorId = proveedor.ProveedorId
      AND relacion.FamiliaId = familia.FamiliaId
);

INSERT dbo.ProveedorCategorias (ProveedorId, CategoriaId, Activo)
SELECT proveedor.ProveedorId, categoria.CategoriaId, 1
FROM @Proveedores datos
INNER JOIN dbo.Proveedores proveedor ON proveedor.Nombre = datos.Nombre
INNER JOIN dbo.FamiliasProducto familia ON familia.Nombre = datos.Familia
INNER JOIN dbo.Categorias categoria ON categoria.FamiliaId = familia.FamiliaId
WHERE
(
    datos.Nombre <> N'Dos Pinos'
    OR categoria.Nombre IN (N'Bebidas', N'Lácteos y huevos', N'Embutidos')
)
AND NOT EXISTS
(
    SELECT 1 FROM dbo.ProveedorCategorias relacion
    WHERE relacion.ProveedorId = proveedor.ProveedorId
      AND relacion.CategoriaId = categoria.CategoriaId
);
GO

DECLARE @Ofertas TABLE
(
    Proveedor NVARCHAR(150),
    Categoria NVARCHAR(80),
    Producto NVARCHAR(120),
    PrecioCompra DECIMAL(18,2)
);

INSERT @Ofertas VALUES
(N'Dos Pinos',N'Lácteos y huevos',N'Leche Entera 1 L',850),
(N'Dos Pinos',N'Lácteos y huevos',N'Yogurt Fresa 200 ml',520),
(N'Dos Pinos',N'Lácteos y huevos',N'Natilla 350 g',1350),
(N'Dos Pinos',N'Lácteos y huevos',N'Queso Turrialba 500 g',3100),
(N'Dos Pinos',N'Lácteos y huevos',N'Leche Deslactosada 1 L',980),
(N'Dos Pinos',N'Bebidas',N'Frescoleche Chocolate 250 ml',475),
(N'Dos Pinos',N'Bebidas',N'Frescoleche Fresa 250 ml',475),
(N'Dos Pinos',N'Bebidas',N'Frescoleche Vainilla 250 ml',475),
(N'Dos Pinos',N'Bebidas',N'Jugo de Naranja 1 L',1150),
(N'Dos Pinos',N'Bebidas',N'Té Frío Limón 500 ml',650),
(N'Dos Pinos',N'Embutidos',N'Jamón de Cerdo 250 g',1850),
(N'Dos Pinos',N'Embutidos',N'Salchichas 500 g',1450),
(N'Dos Pinos',N'Embutidos',N'Mortadela 250 g',1100),
(N'Dos Pinos',N'Embutidos',N'Chorizo Parrillero 500 g',2350),
(N'Dos Pinos',N'Embutidos',N'Salchichón 500 g',1750),
(N'Suministros Casa Tica',N'Productos de limpieza',N'Cloro Clorox 1 litro',850),
(N'Suministros Casa Tica',N'Productos de limpieza',N'Desinfectante Poett lavanda 900 ml',1350),
(N'Suministros Casa Tica',N'Productos de limpieza',N'Limpiador multiuso Ajax 500 ml',1250),
(N'Suministros Casa Tica',N'Productos de limpieza',N'Esponjas Scotch-Brite 3 unidades',900),
(N'Suministros Casa Tica',N'Productos de limpieza',N'Bolsas para basura jardín 10 unidades',1550),
(N'Suministros Casa Tica',N'Lavandería',N'Detergente Irex 1.5 kg',2600),
(N'Suministros Casa Tica',N'Lavandería',N'Suavizante Suavitel 850 ml',1850),
(N'Suministros Casa Tica',N'Lavandería',N'Jabón Azul en barra',550),
(N'Suministros Casa Tica',N'Lavandería',N'Quitamanchas Vanish 450 ml',2800),
(N'Suministros Casa Tica',N'Lavandería',N'Pinzas para ropa 24 unidades',1100),
(N'Suministros Casa Tica',N'Cocina',N'Toallas de cocina Scott 2 rollos',1450),
(N'Suministros Casa Tica',N'Cocina',N'Papel aluminio 7.5 metros',950),
(N'Suministros Casa Tica',N'Cocina',N'Bolsas resellables 20 unidades',1200),
(N'Suministros Casa Tica',N'Cocina',N'Servilletas cuadradas 100 unidades',850),
(N'Suministros Casa Tica',N'Cocina',N'Película adherente 30 metros',1150),
(N'Suministros Casa Tica',N'Baño',N'Basurero de baño',5500),
(N'Suministros Casa Tica',N'Baño',N'Organizador de ducha',3200),
(N'Suministros Casa Tica',N'Baño',N'jabonera',3000),
(N'Suministros Casa Tica',N'Baño',N'Cortina de baño',5500),
(N'Suministros Casa Tica',N'Baño',N'Alfombra de baño',10000),
(N'Suministros Casa Tica',N'Utensilios',N'Sartén antiadherente 24 cm',7200),
(N'Suministros Casa Tica',N'Utensilios',N'Olla de acero 3 litros',8900),
(N'Suministros Casa Tica',N'Utensilios',N'Cuchillo de cocina 8 pulgadas',4300),
(N'Suministros Casa Tica',N'Utensilios',N'Tabla para picar mediana',2800),
(N'Suministros Casa Tica',N'Utensilios',N'Cucharón de acero inoxidable',1900),
(N'Suministros Casa Tica',N'Organización del hogar',N'Caja organizadora 20 litros',4800),
(N'Suministros Casa Tica',N'Organización del hogar',N'Perchas plásticas 10 unidades',2100),
(N'Suministros Casa Tica',N'Organización del hogar',N'Canasta multiuso mediana',2600),
(N'Suministros Casa Tica',N'Organización del hogar',N'Zapatera de cuatro niveles',8500),
(N'Suministros Casa Tica',N'Organización del hogar',N'Repisa plástica modular',12500),
(N'Textiles y Calzado del Istmo',N'Ropa para mujer',N'Camiseta básica para mujer',4200),
(N'Textiles y Calzado del Istmo',N'Ropa para mujer',N'Jeans corte recto para mujer',12800),
(N'Textiles y Calzado del Istmo',N'Ropa para mujer',N'Blusa casual estampada',7500),
(N'Textiles y Calzado del Istmo',N'Ropa para mujer',N'Vestido casual de algodón',13500),
(N'Textiles y Calzado del Istmo',N'Ropa para mujer',N'Suéter liviano para mujer',9800),
(N'Textiles y Calzado del Istmo',N'Ropa para hombre',N'Camiseta básica para hombre',4300),
(N'Textiles y Calzado del Istmo',N'Ropa para hombre',N'Jeans clásico para hombre',13200),
(N'Textiles y Calzado del Istmo',N'Ropa para hombre',N'Camisa tipo polo',8500),
(N'Textiles y Calzado del Istmo',N'Ropa para hombre',N'Pantalón casual',11800),
(N'Textiles y Calzado del Istmo',N'Ropa para hombre',N'Suéter liviano para hombre',9900),
(N'Textiles y Calzado del Istmo',N'Ropa infantil',N'Camiseta infantil estampada',3200),
(N'Textiles y Calzado del Istmo',N'Ropa infantil',N'Short infantil de algodón',3800),
(N'Textiles y Calzado del Istmo',N'Ropa infantil',N'Pijama infantil',6500),
(N'Textiles y Calzado del Istmo',N'Ropa infantil',N'Conjunto infantil deportivo',7900),
(N'Textiles y Calzado del Istmo',N'Ropa infantil',N'Sudadera infantil',7200),
(N'Textiles y Calzado del Istmo',N'Calzado',N'Tenis casual unisex',16500),
(N'Textiles y Calzado del Istmo',N'Calzado',N'Sandalias playeras',5800),
(N'Textiles y Calzado del Istmo',N'Calzado',N'Botas de hule',9800),
(N'Textiles y Calzado del Istmo',N'Calzado',N'Zapato escolar negro',14500),
(N'Textiles y Calzado del Istmo',N'Calzado',N'Pantuflas acolchadas',6500),
(N'Textiles y Calzado del Istmo',N'Ropa interior',N'Medias deportivas 3 pares',3800),
(N'Textiles y Calzado del Istmo',N'Ropa interior',N'Bóxer de algodón 2 unidades',6200),
(N'Textiles y Calzado del Istmo',N'Ropa interior',N'Brasier básico',7600),
(N'Textiles y Calzado del Istmo',N'Ropa interior',N'Panty de algodón 3 unidades',5900),
(N'Textiles y Calzado del Istmo',N'Ropa interior',N'Camiseta interior',4200),
(N'Textiles y Calzado del Istmo',N'Accesorios',N'Gorra',4500),
(N'Textiles y Calzado del Istmo',N'Accesorios',N'Fajas',5500),
(N'Textiles y Calzado del Istmo',N'Accesorios',N'Relojes',15000),
(N'Textiles y Calzado del Istmo',N'Accesorios',N'Billetera',7000),
(N'Textiles y Calzado del Istmo',N'Accesorios',N'Pulseras',12900),
(N'Tecnología Pura Vida',N'Celulares',N'Samsung Galaxy A16 128 GB',82000),
(N'Tecnología Pura Vida',N'Celulares',N'Xiaomi Redmi Note 14 256 GB',112000),
(N'Tecnología Pura Vida',N'Celulares',N'Motorola Moto G55 256 GB',118000),
(N'Tecnología Pura Vida',N'Celulares',N'Honor X8c 256 GB',127000),
(N'Tecnología Pura Vida',N'Celulares',N'Nokia C32 128 GB',69000),
(N'Tecnología Pura Vida',N'Computadoras',N'Laptop Lenovo IdeaPad 15 pulgadas',235000),
(N'Tecnología Pura Vida',N'Computadoras',N'Laptop HP 15 pulgadas',248000),
(N'Tecnología Pura Vida',N'Computadoras',N'Laptop Acer Aspire 3',229000),
(N'Tecnología Pura Vida',N'Computadoras',N'Computadora de escritorio familiar',265000),
(N'Tecnología Pura Vida',N'Computadoras',N'Chromebook 14 pulgadas',175000),
(N'Tecnología Pura Vida',N'Audio',N'Sistema de parlantes',59000),
(N'Tecnología Pura Vida',N'Audio',N'Headset gamer',26000),
(N'Tecnología Pura Vida',N'Audio',N'Radio portátil',10000),
(N'Tecnología Pura Vida',N'Audio',N'Barra de sonido',40000),
(N'Tecnología Pura Vida',N'Audio',N'Audifonos Deportivos',24000),
(N'Tecnología Pura Vida',N'Accesorios electrónicos',N'Audífonos',20000),
(N'Tecnología Pura Vida',N'Accesorios electrónicos',N'Cable USB',8000),
(N'Tecnología Pura Vida',N'Accesorios electrónicos',N'Mouse inhalambrico',11000),
(N'Tecnología Pura Vida',N'Accesorios electrónicos',N'Teclado Inhalambrico',12000),
(N'Tecnología Pura Vida',N'Accesorios electrónicos',N'Parlante Bluetooth',26000),
(N'Tecnología Pura Vida',N'Electrodomésticos pequeños',N'Coffeemaker Oster 12 tazas',22500),
(N'Tecnología Pura Vida',N'Electrodomésticos pequeños',N'Olla arrocera Black+Decker',19800),
(N'Tecnología Pura Vida',N'Electrodomésticos pequeños',N'Licuadora Oster clásica',34500),
(N'Tecnología Pura Vida',N'Electrodomésticos pequeños',N'Plancha de vapor',16800),
(N'Tecnología Pura Vida',N'Electrodomésticos pequeños',N'Ventilador de mesa 12 pulgadas',21500),
(N'Bienestar Costarricense',N'Higiene personal',N'Jabón Protex 3 unidades',1350),
(N'Bienestar Costarricense',N'Higiene personal',N'Desodorante Rexona roll-on',1650),
(N'Bienestar Costarricense',N'Higiene personal',N'Gel de baño Dove 400 ml',2850),
(N'Bienestar Costarricense',N'Higiene personal',N'Talco para pies 100 g',1550),
(N'Bienestar Costarricense',N'Higiene personal',N'Pañuelos faciales 100 unidades',950),
(N'Bienestar Costarricense',N'Cuidado del cabello',N'Shampoo Sedal 340 ml',2100),
(N'Bienestar Costarricense',N'Cuidado del cabello',N'Acondicionador Pantene 400 ml',2950),
(N'Bienestar Costarricense',N'Cuidado del cabello',N'Tratamiento capilar Novex 400 g',4200),
(N'Bienestar Costarricense',N'Cuidado del cabello',N'Gel fijador Ego 250 ml',1850),
(N'Bienestar Costarricense',N'Cuidado del cabello',N'Peine de dientes anchos',850),
(N'Bienestar Costarricense',N'Cuidado de la piel',N'Protector solar FPS 50',6200),
(N'Bienestar Costarricense',N'Cuidado de la piel',N'Crema Nivea 200 ml',2800),
(N'Bienestar Costarricense',N'Cuidado de la piel',N'Limpiador facial Neutrogena',5200),
(N'Bienestar Costarricense',N'Cuidado de la piel',N'Gel de aloe vera 250 ml',3100),
(N'Bienestar Costarricense',N'Cuidado de la piel',N'Manteca de cacao en barra',950),
(N'Bienestar Costarricense',N'Cuidado dental',N'Pasta dental Colgate 100 ml',1450),
(N'Bienestar Costarricense',N'Cuidado dental',N'Cepillo dental Oral-B',1250),
(N'Bienestar Costarricense',N'Cuidado dental',N'Enjuague Listerine 500 ml',3100),
(N'Bienestar Costarricense',N'Cuidado dental',N'Hilo dental 50 metros',1450),
(N'Bienestar Costarricense',N'Cuidado dental',N'Pasta dental infantil 75 ml',1350),
(N'Bienestar Costarricense',N'Higiene femenina',N'Toallas Kotex nocturnas 8 unidades',1850),
(N'Bienestar Costarricense',N'Higiene femenina',N'Toallas Always día 10 unidades',1700),
(N'Bienestar Costarricense',N'Higiene femenina',N'Protectores diarios 20 unidades',1250),
(N'Bienestar Costarricense',N'Higiene femenina',N'Tampones 8 unidades',2400),
(N'Bienestar Costarricense',N'Higiene femenina',N'Jabón íntimo 200 ml',2850);

DECLARE @ImpuestoGeneralId INT =
    (SELECT ImpuestoId FROM dbo.Impuestos WHERE Nombre = N'IVA 13%');
DECLARE @ImpuestoReducidoId INT = COALESCE
(
    (SELECT ImpuestoId FROM dbo.Impuestos WHERE Nombre = N'IVA 1%'),
    @ImpuestoGeneralId
);

IF @ImpuestoGeneralId IS NULL
    THROW 51007, 'No existe el impuesto general requerido por los datos iniciales.', 1;

UPDATE oferta
SET PrecioCompra = datos.PrecioCompra,
    ImpuestoId = CASE
        WHEN categoria.Nombre IN (N'Lácteos y huevos', N'Embutidos')
            THEN @ImpuestoReducidoId
        ELSE @ImpuestoGeneralId
    END,
    FechaActualizacion = SYSDATETIME(),
    Activo = 1
FROM dbo.ProductosProveedorCatalogo oferta
INNER JOIN dbo.ProveedorCategorias relacion
    ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
INNER JOIN dbo.Proveedores proveedor ON proveedor.ProveedorId = relacion.ProveedorId
INNER JOIN dbo.Categorias categoria ON categoria.CategoriaId = relacion.CategoriaId
INNER JOIN @Ofertas datos
    ON datos.Proveedor = proveedor.Nombre
   AND datos.Categoria = categoria.Nombre
   AND datos.Producto = oferta.Nombre;

INSERT dbo.ProductosProveedorCatalogo
(
    ProveedorCategoriaId,
    ImpuestoId,
    Nombre,
    PrecioCompra,
    ProductoId,
    Activo
)
SELECT relacion.ProveedorCategoriaId,
       CASE
           WHEN categoria.Nombre IN (N'Lácteos y huevos', N'Embutidos')
               THEN @ImpuestoReducidoId
           ELSE @ImpuestoGeneralId
       END,
       datos.Producto,
       datos.PrecioCompra,
       NULL,
       1
FROM @Ofertas datos
INNER JOIN dbo.Proveedores proveedor ON proveedor.Nombre = datos.Proveedor
INNER JOIN dbo.Categorias categoria ON categoria.Nombre = datos.Categoria
INNER JOIN dbo.ProveedorCategorias relacion
    ON relacion.ProveedorId = proveedor.ProveedorId
   AND relacion.CategoriaId = categoria.CategoriaId
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.ProductosProveedorCatalogo oferta
    WHERE oferta.ProveedorCategoriaId = relacion.ProveedorCategoriaId
      AND oferta.Nombre = datos.Producto
);

;WITH Coincidencias AS
(
    SELECT oferta.ProductoProveedorCatalogoId, MIN(producto.ProductoId) ProductoId
    FROM dbo.ProductosProveedorCatalogo oferta
    INNER JOIN dbo.ProveedorCategorias relacion
        ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
    INNER JOIN dbo.Productos producto
        ON producto.CategoriaId = relacion.CategoriaId
       AND producto.Nombre = oferta.Nombre
    WHERE oferta.ProductoId IS NULL
    GROUP BY oferta.ProductoProveedorCatalogoId
    HAVING COUNT(*) = 1
)
UPDATE oferta
SET ProductoId = coincidencia.ProductoId
FROM dbo.ProductosProveedorCatalogo oferta
INNER JOIN Coincidencias coincidencia
    ON coincidencia.ProductoProveedorCatalogoId = oferta.ProductoProveedorCatalogoId;

DECLARE @ProductosIniciales TABLE
(
    Nombre NVARCHAR(120),
    Codigo NVARCHAR(50),
    Descripcion NVARCHAR(500)
);

INSERT @ProductosIniciales VALUES
(N'Frescoleche Chocolate 250 ml', N'PROD-DP-000001', N'Bebida láctea sabor chocolate.'),
(N'Leche Entera 1 L', N'PROD-DP-000002', N'Leche entera de un litro.');

INSERT dbo.Productos
(
    CategoriaId, ImpuestoId, Codigo, Nombre, Descripcion,
    PrecioVenta, Costo, Stock, StockMinimo, Activo, FechaCreacion
)
SELECT categoria.CategoriaId,
       oferta.ImpuestoId,
       inicial.Codigo,
       inicial.Nombre,
       inicial.Descripcion,
       ROUND(oferta.PrecioCompra * 1.30, 2),
       oferta.PrecioCompra,
       0,
       5,
       1,
       SYSDATETIME()
FROM @ProductosIniciales inicial
INNER JOIN dbo.ProductosProveedorCatalogo oferta
    ON oferta.Nombre = inicial.Nombre
INNER JOIN dbo.ProveedorCategorias relacion
    ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
INNER JOIN dbo.Proveedores proveedor
    ON proveedor.ProveedorId = relacion.ProveedorId
INNER JOIN dbo.Categorias categoria
    ON categoria.CategoriaId = relacion.CategoriaId
WHERE proveedor.Nombre = N'Dos Pinos'
  AND oferta.ProductoId IS NULL
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.Productos producto
      WHERE producto.CategoriaId = categoria.CategoriaId
        AND producto.Nombre = inicial.Nombre
  )
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.Productos producto
      WHERE producto.Codigo = inicial.Codigo
  );

UPDATE oferta
SET ProductoId = producto.ProductoId
FROM dbo.ProductosProveedorCatalogo oferta
INNER JOIN dbo.ProveedorCategorias relacion
    ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
INNER JOIN dbo.Proveedores proveedor
    ON proveedor.ProveedorId = relacion.ProveedorId
INNER JOIN dbo.Productos producto
    ON producto.CategoriaId = relacion.CategoriaId
   AND producto.Nombre = oferta.Nombre
INNER JOIN @ProductosIniciales inicial
    ON inicial.Nombre = oferta.Nombre
WHERE proveedor.Nombre = N'Dos Pinos'
  AND oferta.ProductoId IS NULL;

INSERT dbo.ProductoProveedor (ProductoId, ProveedorId, PrecioCompra, Activo)
SELECT oferta.ProductoId, relacion.ProveedorId, oferta.PrecioCompra, 1
FROM dbo.ProductosProveedorCatalogo oferta
INNER JOIN dbo.ProveedorCategorias relacion
    ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
WHERE oferta.ProductoId IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.ProductoProveedor actual
      WHERE actual.ProductoId = oferta.ProductoId
        AND actual.ProveedorId = relacion.ProveedorId
  );
GO

CREATE OR ALTER PROCEDURE dbo.sp_IncorporarProductoProveedor
    @ProductoProveedorCatalogoId INT,
    @Descripcion NVARCHAR(500) = NULL,
    @StockMinimo INT = 5,
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ProductoProveedorCatalogoId <= 0 OR @UsuarioId <= 0
        THROW 51001, 'Los datos para incorporar el producto no son válidos.', 1;
    IF @StockMinimo < 0
        THROW 51002, 'El stock mínimo no puede ser negativo.', 1;

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @ProveedorId INT;
        DECLARE @CategoriaId INT;
        DECLARE @Nombre NVARCHAR(120);
        DECLARE @PrecioCompra DECIMAL(18,2);
        DECLARE @ImpuestoId INT;
        DECLARE @ProductoId INT;

        SELECT @ProveedorId = relacion.ProveedorId,
               @CategoriaId = relacion.CategoriaId,
               @Nombre = oferta.Nombre,
               @PrecioCompra = oferta.PrecioCompra,
               @ImpuestoId = oferta.ImpuestoId,
               @ProductoId = oferta.ProductoId
        FROM dbo.ProductosProveedorCatalogo oferta WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.ProveedorCategorias relacion
            ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
        INNER JOIN dbo.Proveedores proveedor ON proveedor.ProveedorId = relacion.ProveedorId
        INNER JOIN dbo.Categorias categoria ON categoria.CategoriaId = relacion.CategoriaId
        INNER JOIN dbo.FamiliasProducto familia ON familia.FamiliaId = categoria.FamiliaId
        WHERE oferta.ProductoProveedorCatalogoId = @ProductoProveedorCatalogoId
          AND oferta.Activo = 1
          AND relacion.Activo = 1
          AND proveedor.Activo = 1
          AND categoria.Activo = 1
          AND familia.Activo = 1;

        IF @ProveedorId IS NULL
            THROW 51003, 'La oferta del proveedor no está disponible.', 1;
        IF @ProductoId IS NOT NULL
            THROW 51004, 'El producto ya fue incorporado a LessPrice.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Impuestos WHERE ImpuestoId = @ImpuestoId AND Activo = 1)
            THROW 51005, 'El impuesto seleccionado no está disponible.', 1;

        DECLARE @Codigo NVARCHAR(50);
        DECLARE @CodigoNumero BIGINT = NEXT VALUE FOR dbo.SecuenciaCodigoProducto;
        SET @Codigo = CONCAT(N'PROD-', RIGHT(CONCAT(N'000000', @CodigoNumero), 6));

        WHILE EXISTS (SELECT 1 FROM dbo.Productos WITH (UPDLOCK, HOLDLOCK) WHERE Codigo = @Codigo)
        BEGIN
            SET @CodigoNumero = NEXT VALUE FOR dbo.SecuenciaCodigoProducto;
            SET @Codigo = CONCAT(N'PROD-', RIGHT(CONCAT(N'000000', @CodigoNumero), 6));
        END;

        INSERT dbo.Productos
        (
            CategoriaId, ImpuestoId, Codigo, Nombre, Descripcion,
            PrecioVenta, Costo, Stock, StockMinimo, Activo, FechaCreacion
        )
        VALUES
        (
            @CategoriaId, @ImpuestoId, @Codigo, @Nombre, NULLIF(LTRIM(RTRIM(@Descripcion)), N''),
            ROUND(@PrecioCompra * 1.30, 2), @PrecioCompra, 0, @StockMinimo, 1, SYSDATETIME()
        );

        SET @ProductoId = CONVERT(INT, SCOPE_IDENTITY());

        UPDATE dbo.ProductosProveedorCatalogo
        SET ProductoId = @ProductoId,
            FechaActualizacion = SYSDATETIME()
        WHERE ProductoProveedorCatalogoId = @ProductoProveedorCatalogoId;

        INSERT dbo.ProductoProveedor (ProductoId, ProveedorId, PrecioCompra, Activo)
        VALUES (@ProductoId, @ProveedorId, @PrecioCompra, 1);

        INSERT dbo.BitacoraSistema (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
        VALUES
        (
            @UsuarioId, SYSDATETIME(), N'INCORPORAR_PRODUCTO', N'Producto',
            CONVERT(NVARCHAR(80), @ProductoId), CONCAT(N'Oferta del proveedor: ', @ProductoProveedorCatalogoId)
        );

        COMMIT TRANSACTION;

        SELECT @ProductoId ProductoId,
               @Codigo Codigo,
               @Nombre Nombre,
               @PrecioCompra PrecioCompra,
               ROUND(@PrecioCompra * 1.30, 2) PrecioVenta,
               0 Stock;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConfirmarCompraProveedor
    @ClaveConfirmacion UNIQUEIDENTIFIER,
    @ProveedorId INT,
    @UsuarioId INT,
    @DetalleJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ProveedorId <= 0 OR @UsuarioId <= 0 OR ISJSON(@DetalleJson) <> 1
        THROW 51101, 'Los datos de la compra no son válidos.', 1;

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @CompraExistenteId INT;
        SELECT @CompraExistenteId = CompraProveedorId
        FROM dbo.ComprasProveedor WITH (UPDLOCK, HOLDLOCK)
        WHERE ClaveConfirmacion = @ClaveConfirmacion;

        IF @CompraExistenteId IS NOT NULL
        BEGIN
            COMMIT TRANSACTION;
            SELECT CompraProveedorId, Numero, Total, Estado, Fecha
            FROM dbo.ComprasProveedor WHERE CompraProveedorId = @CompraExistenteId;
            RETURN;
        END;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Proveedores WITH (UPDLOCK, HOLDLOCK)
            WHERE ProveedorId = @ProveedorId AND Activo = 1
        )
            THROW 51102, 'El proveedor está inactivo o no existe.', 1;

        DECLARE @Detalle TABLE
        (
            ProductoProveedorCatalogoId INT NOT NULL PRIMARY KEY,
            Cantidad INT NOT NULL
        );

        IF EXISTS
        (
            SELECT ProductoProveedorCatalogoId
            FROM OPENJSON(@DetalleJson)
            WITH (ProductoProveedorCatalogoId INT '$.productoProveedorCatalogoId')
            GROUP BY ProductoProveedorCatalogoId
            HAVING COUNT(*) > 1
        )
            THROW 51103, 'La compra contiene productos repetidos.', 1;

        INSERT @Detalle (ProductoProveedorCatalogoId, Cantidad)
        SELECT ProductoProveedorCatalogoId, Cantidad
        FROM OPENJSON(@DetalleJson)
        WITH
        (
            ProductoProveedorCatalogoId INT '$.productoProveedorCatalogoId',
            Cantidad INT '$.cantidad'
        );

        IF NOT EXISTS (SELECT 1 FROM @Detalle)
            THROW 51104, 'Debe agregar al menos un producto a la compra.', 1;
        IF EXISTS (SELECT 1 FROM @Detalle WHERE Cantidad <= 0)
            THROW 51105, 'La cantidad debe ser mayor que cero.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM @Detalle detalle
            LEFT JOIN dbo.ProductosProveedorCatalogo oferta
                ON oferta.ProductoProveedorCatalogoId = detalle.ProductoProveedorCatalogoId
            LEFT JOIN dbo.ProveedorCategorias relacion
                ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
            LEFT JOIN dbo.Productos producto
                ON producto.ProductoId = oferta.ProductoId
            LEFT JOIN dbo.ProductoProveedor productoProveedor
                ON productoProveedor.ProductoId = oferta.ProductoId
               AND productoProveedor.ProveedorId = relacion.ProveedorId
            WHERE oferta.ProductoProveedorCatalogoId IS NULL
               OR oferta.Activo = 0
               OR oferta.ProductoId IS NULL
               OR relacion.Activo = 0
               OR relacion.ProveedorId <> @ProveedorId
               OR producto.ProductoId IS NULL
               OR producto.Activo = 0
               OR productoProveedor.ProductoId IS NULL
               OR productoProveedor.Activo = 0
        )
            THROW 51106, 'Uno de los productos no pertenece al proveedor o aún no fue incorporado.', 1;

        DECLARE @Total DECIMAL(18,2);
        SELECT @Total = ROUND(SUM(oferta.PrecioCompra * detalle.Cantidad), 2)
        FROM @Detalle detalle
        INNER JOIN dbo.ProductosProveedorCatalogo oferta
            ON oferta.ProductoProveedorCatalogoId = detalle.ProductoProveedorCatalogoId;

        DECLARE @NumeroSecuencia BIGINT = NEXT VALUE FOR dbo.SecuenciaCompraProveedor;
        DECLARE @Numero NVARCHAR(40) =
            CONCAT(N'COMP-', RIGHT(CONCAT(N'000000', @NumeroSecuencia), 6));

        INSERT dbo.ComprasProveedor
        (
            ProveedorId, UsuarioId, Numero, Fecha, FechaConfirmacion,
            Estado, Total, ClaveConfirmacion
        )
        VALUES
        (
            @ProveedorId, @UsuarioId, @Numero, SYSDATETIME(), SYSDATETIME(),
            N'CONFIRMADA', @Total, @ClaveConfirmacion
        );

        DECLARE @CompraProveedorId INT = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.CompraProveedorDetalle
        (
            CompraProveedorId, ProductoId, ProductoProveedorCatalogoId,
            NombreProducto, Cantidad, PrecioUnitario, Subtotal
        )
        SELECT @CompraProveedorId,
               oferta.ProductoId,
               oferta.ProductoProveedorCatalogoId,
               oferta.Nombre,
               detalle.Cantidad,
               oferta.PrecioCompra,
               ROUND(oferta.PrecioCompra * detalle.Cantidad, 2)
        FROM @Detalle detalle
        INNER JOIN dbo.ProductosProveedorCatalogo oferta
            ON oferta.ProductoProveedorCatalogoId = detalle.ProductoProveedorCatalogoId;

        DECLARE @Movimientos TABLE
        (
            ProductoId INT,
            Cantidad INT,
            StockAnterior INT,
            StockNuevo INT
        );

        UPDATE producto WITH (UPDLOCK)
        SET Stock = producto.Stock + detalle.Cantidad,
            Costo = oferta.PrecioCompra,
            PrecioVenta = ROUND(oferta.PrecioCompra * 1.30, 2)
        OUTPUT inserted.ProductoId,
               detalle.Cantidad,
               deleted.Stock,
               inserted.Stock
        INTO @Movimientos (ProductoId, Cantidad, StockAnterior, StockNuevo)
        FROM dbo.Productos producto
        INNER JOIN dbo.ProductosProveedorCatalogo oferta
            ON oferta.ProductoId = producto.ProductoId
        INNER JOIN @Detalle detalle
            ON detalle.ProductoProveedorCatalogoId = oferta.ProductoProveedorCatalogoId;

        INSERT dbo.MovimientosInventario
        (
            ProductoId, Tipo, Cantidad, Motivo, Fecha, UsuarioId,
            CompraProveedorId, StockAnterior, StockNuevo
        )
        SELECT ProductoId,
               N'ENTRADA',
               Cantidad,
               CONCAT(N'Compra a proveedor ', @Numero),
               SYSDATETIME(),
               @UsuarioId,
               @CompraProveedorId,
               StockAnterior,
               StockNuevo
        FROM @Movimientos;

        UPDATE relacion
        SET PrecioCompra = oferta.PrecioCompra,
            Activo = 1
        FROM dbo.ProductoProveedor relacion
        INNER JOIN dbo.ProductosProveedorCatalogo oferta
            ON oferta.ProductoId = relacion.ProductoId
        INNER JOIN @Detalle detalle
            ON detalle.ProductoProveedorCatalogoId = oferta.ProductoProveedorCatalogoId
        WHERE relacion.ProveedorId = @ProveedorId;

        INSERT dbo.BitacoraSistema (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
        VALUES
        (
            @UsuarioId, SYSDATETIME(), N'CONFIRMAR_COMPRA_PROVEEDOR', N'CompraProveedor',
            CONVERT(NVARCHAR(80), @CompraProveedorId), CONCAT(@Numero, N' - Total CRC ', @Total)
        );

        COMMIT TRANSACTION;

        SELECT @CompraProveedorId CompraProveedorId,
               @Numero Numero,
               @Total Total,
               N'CONFIRMADA' Estado,
               SYSDATETIME() Fecha;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

DECLARE @AdministradorId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Administrador');

IF NOT EXISTS (SELECT 1 FROM dbo.MenuOpciones WHERE Ruta = N'/proveedores')
    INSERT dbo.MenuOpciones (Nombre, Ruta, Icono, Orden, Activo)
    VALUES (N'Proveedores', N'/proveedores', N'cil-basket', 45, 1);
ELSE
    UPDATE dbo.MenuOpciones
    SET Nombre = N'Proveedores', Icono = N'cil-basket', Orden = 45, Activo = 1
    WHERE Ruta = N'/proveedores';

INSERT dbo.RolMenuOpciones (RolId, MenuOpcionId)
SELECT @AdministradorId, opcion.MenuOpcionId
FROM dbo.MenuOpciones opcion
WHERE opcion.Ruta = N'/proveedores'
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.RolMenuOpciones relacion
      WHERE relacion.RolId = @AdministradorId
        AND relacion.MenuOpcionId = opcion.MenuOpcionId
  );
GO
