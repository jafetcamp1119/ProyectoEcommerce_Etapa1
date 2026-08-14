/* =========================================================
   Proyecto E-commerce - Base de datos Etapa 1
   Alcance equivalente al proyecto Ventas del profesor:
   - Catalogos basicos
   - Productos
   - Usuarios como clientes
   - Ordenes y su detalle
   ========================================================= */

USE [master];
GO

-- crea la base solamente si todavia no existe
IF DB_ID(N'ProyectoEcommerceDB') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [ProyectoEcommerceDB]');
END;
GO

USE [ProyectoEcommerceDB];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* =========================================================
   Salvaguarda no destructiva.
   Este script solo crea una instalacion vacia. Si el esquema
   ya existe, se detiene sin eliminar tablas ni datos. Para una
   base existente use ProyectoEcommerceDB_AmpliacionImportante.sql.
   ========================================================= */

-- si encuentra parte del esquema se detiene para no mezclar una instalacion nueva con datos existentes
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.FamiliasProducto', N'U') IS NOT NULL
BEGIN
    THROW 50000, 'El esquema ya existe. No se eliminaron datos; ejecute el script incremental de ampliacion.', 1;
END;
GO

/* =========================================================
   Usuarios
   Equivalente tecnico: Cliente del proyecto Ventas.
   Incluye autenticacion por correo y PasswordHash.
   ========================================================= */

CREATE TABLE dbo.Usuarios
(
    UsuarioId INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(80) NOT NULL,
    Apellidos NVARCHAR(120) NOT NULL,
    Correo NVARCHAR(120) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Telefono NVARCHAR(30) NOT NULL,
    PasswordHash NVARCHAR(500) NULL,
    Direccion NVARCHAR(250) NULL,
    Activo BIT NOT NULL
        CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
    FechaRegistro DATETIME2(3) NOT NULL
        CONSTRAINT DF_Usuarios_FechaRegistro DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Usuarios
        PRIMARY KEY (UsuarioId),
    CONSTRAINT UQ_Usuarios_Correo
        UNIQUE (Correo),
    CONSTRAINT CK_Usuarios_CorreoNormalizado
        CHECK (Correo = LOWER(LTRIM(RTRIM(Correo))))
);
GO

/* =========================================================
   Familias de productos
   Catalogo basado en el patron de Categoria de Ventas.
   ========================================================= */

CREATE TABLE dbo.FamiliasProducto
(
    FamiliaId INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(80) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    -- aqui se guarda la ruta de la imagen de la familia
    -- queda en null cuando la familia usa el placeholder de LessPrice
    UrlImagen NVARCHAR(500) NULL,
    Activo BIT NOT NULL
        CONSTRAINT DF_FamiliasProducto_Activo DEFAULT (1),

    CONSTRAINT PK_FamiliasProducto
        PRIMARY KEY (FamiliaId),
    CONSTRAINT UQ_FamiliasProducto_Nombre
        UNIQUE (Nombre)
);
GO

/* =========================================================
   Categorias
   Catalogo dependiente de una familia de productos.
   ========================================================= */

CREATE TABLE dbo.Categorias
(
    CategoriaId INT IDENTITY(1,1) NOT NULL,
    FamiliaId INT NOT NULL,
    Nombre NVARCHAR(80) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    -- aqui se guarda la ruta de la imagen de la categoria
    -- no es obligatoria porque la tarjeta puede mostrar sus iniciales
    UrlImagen NVARCHAR(500) NULL,
    Activo BIT NOT NULL
        CONSTRAINT DF_Categorias_Activo DEFAULT (1),

    CONSTRAINT PK_Categorias
        PRIMARY KEY (CategoriaId),
    CONSTRAINT UQ_Categorias_Familia_Nombre
        UNIQUE (FamiliaId, Nombre),
    CONSTRAINT FK_Categorias_FamiliasProducto
        FOREIGN KEY (FamiliaId)
        REFERENCES dbo.FamiliasProducto (FamiliaId)
);
GO

/* =========================================================
   Impuestos
   Catalogo basado en el patron de Categoria de Ventas.
   ========================================================= */

CREATE TABLE dbo.Impuestos
(
    ImpuestoId INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(80) NOT NULL,
    Porcentaje DECIMAL(5,2) NOT NULL,
    FechaInicio DATE NOT NULL
        CONSTRAINT DF_Impuestos_FechaInicio DEFAULT (CONVERT(DATE, GETDATE())),
    FechaFin DATE NULL,
    Activo BIT NOT NULL
        CONSTRAINT DF_Impuestos_Activo DEFAULT (1),

    CONSTRAINT PK_Impuestos
        PRIMARY KEY (ImpuestoId),
    CONSTRAINT UQ_Impuestos_Nombre
        UNIQUE (Nombre),
    CONSTRAINT CK_Impuestos_Porcentaje
        CHECK (Porcentaje >= 0 AND Porcentaje <= 100),
    CONSTRAINT CK_Impuestos_Fechas
        CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio)
);
GO

/* =========================================================
   Productos
   Equivalente tecnico: Producto del proyecto Ventas.
   ========================================================= */

CREATE TABLE dbo.Productos
(
    ProductoId INT IDENTITY(1,1) NOT NULL,
    CategoriaId INT NOT NULL,
    ImpuestoId INT NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    PrecioVenta DECIMAL(18,2) NOT NULL,
    Costo DECIMAL(18,2) NOT NULL,
    Stock INT NOT NULL
        CONSTRAINT DF_Productos_Stock DEFAULT (0),
    StockMinimo INT NOT NULL
        CONSTRAINT DF_Productos_StockMinimo DEFAULT (5),
    Activo BIT NOT NULL
        CONSTRAINT DF_Productos_Activo DEFAULT (1),
    FechaCreacion DATETIME2(3) NOT NULL
        CONSTRAINT DF_Productos_FechaCreacion DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Productos
        PRIMARY KEY (ProductoId),
    CONSTRAINT UQ_Productos_Codigo
        UNIQUE (Codigo),
    CONSTRAINT FK_Productos_Categorias
        FOREIGN KEY (CategoriaId)
        REFERENCES dbo.Categorias (CategoriaId),
    CONSTRAINT FK_Productos_Impuestos
        FOREIGN KEY (ImpuestoId)
        REFERENCES dbo.Impuestos (ImpuestoId),
    CONSTRAINT CK_Productos_PrecioVenta
        CHECK (PrecioVenta >= 0),
    CONSTRAINT CK_Productos_Costo
        CHECK (Costo >= 0),
    CONSTRAINT CK_Productos_Stock
        CHECK (Stock >= 0),
    CONSTRAINT CK_Productos_StockMinimo
        CHECK (StockMinimo >= 0)
);
GO

/* =========================================================
   Ordenes
   Equivalente tecnico: Pedido del proyecto Ventas.
   En esta etapa no se procesan pagos, facturas ni inventario.
   ========================================================= */

CREATE TABLE dbo.Ordenes
(
    OrdenId INT IDENTITY(1,1) NOT NULL,
    UsuarioId INT NOT NULL,
    FechaOrden DATETIME2(3) NOT NULL
        CONSTRAINT DF_Ordenes_FechaOrden DEFAULT (SYSDATETIME()),
    Estado NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Ordenes_Estado DEFAULT (N'PENDIENTE'),
    Moneda CHAR(3) NOT NULL
        CONSTRAINT DF_Ordenes_Moneda DEFAULT ('CRC'),
    Total DECIMAL(18,2) NULL,

    CONSTRAINT PK_Ordenes
        PRIMARY KEY (OrdenId),
    CONSTRAINT FK_Ordenes_Usuarios
        FOREIGN KEY (UsuarioId)
        REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT CK_Ordenes_Estado
        CHECK (Estado IN (N'PENDIENTE', N'CONFIRMADA', N'CANCELADA')),
    CONSTRAINT CK_Ordenes_Total
        CHECK (Total IS NULL OR Total >= 0)
);
GO

/* =========================================================
   Detalle de orden
   Equivalente tecnico: DetallesPedido del proyecto Ventas.
   No tendra LN ni Controller independiente en esta etapa.
   ========================================================= */

CREATE TABLE dbo.OrdenDetalle
(
    OrdenDetalleId INT IDENTITY(1,1) NOT NULL,
    OrdenId INT NOT NULL,
    ProductoId INT NOT NULL,
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(18,2) NOT NULL,
    PorcentajeImpuesto DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_OrdenDetalle_PorcentajeImpuesto DEFAULT (0),
    Subtotal DECIMAL(18,2) NOT NULL,
    TotalLinea DECIMAL(18,2) NOT NULL,

    CONSTRAINT PK_OrdenDetalle
        PRIMARY KEY (OrdenDetalleId),
    CONSTRAINT UQ_OrdenDetalle_Orden_Producto
        UNIQUE (OrdenId, ProductoId),
    CONSTRAINT FK_OrdenDetalle_Ordenes
        FOREIGN KEY (OrdenId)
        REFERENCES dbo.Ordenes (OrdenId),
    CONSTRAINT FK_OrdenDetalle_Productos
        FOREIGN KEY (ProductoId)
        REFERENCES dbo.Productos (ProductoId),
    CONSTRAINT CK_OrdenDetalle_Cantidad
        CHECK (Cantidad > 0),
    CONSTRAINT CK_OrdenDetalle_PrecioUnitario
        CHECK (PrecioUnitario >= 0),
    CONSTRAINT CK_OrdenDetalle_PorcentajeImpuesto
        CHECK (PorcentajeImpuesto >= 0 AND PorcentajeImpuesto <= 100),
    CONSTRAINT CK_OrdenDetalle_Subtotal
        CHECK (Subtotal >= 0),
    CONSTRAINT CK_OrdenDetalle_TotalLinea
        CHECK (TotalLinea >= 0)
);
GO

/* =========================================================
   Indices para las llaves foraneas y busquedas basicas.
   ========================================================= */

-- estos indices ayudan a buscar relaciones y ordenar sin recorrer toda la tabla
CREATE INDEX IX_Categorias_FamiliaId
    ON dbo.Categorias (FamiliaId);

CREATE INDEX IX_Productos_CategoriaId
    ON dbo.Productos (CategoriaId);

CREATE INDEX IX_Productos_ImpuestoId
    ON dbo.Productos (ImpuestoId);

CREATE INDEX IX_Productos_Nombre
    ON dbo.Productos (Nombre);

CREATE INDEX IX_Ordenes_UsuarioId
    ON dbo.Ordenes (UsuarioId);

CREATE INDEX IX_Ordenes_FechaOrden
    ON dbo.Ordenes (FechaOrden);

CREATE INDEX IX_OrdenDetalle_ProductoId
    ON dbo.OrdenDetalle (ProductoId);
GO

PRINT N'ProyectoEcommerceDB - Etapa 1 creada correctamente.';
GO
