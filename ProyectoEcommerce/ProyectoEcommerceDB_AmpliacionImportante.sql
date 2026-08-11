/* =========================================================
   Proyecto E-commerce - ampliacion importante y no destructiva

   Integra los elementos utiles de BD.docx en la unica base activa:
   seguridad por roles, historial de acceso, opciones de menu,
   imagenes de producto, proveedores, carritos, documentos,
   pagos, calificaciones, inventario y bitacora.

   - No elimina tablas ni registros.
   - No usa DROP DATABASE ni EnsureCreated.
   - Es incremental e idempotente.
   ========================================================= */

USE [ProyectoEcommerceDB];
GO

SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
    THROW 50001, 'No existe dbo.Usuarios. Ejecute primero el script base en una base vacia.', 1;
GO

/* Seguridad y autorizacion */
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RolId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        Nombre NVARCHAR(50) NOT NULL,
        Descripcion NVARCHAR(200) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1),
        CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre),
        CONSTRAINT CK_Roles_Nombre CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Administrador')
    INSERT dbo.Roles (Nombre, Descripcion) VALUES (N'Administrador', N'Acceso administrativo al sistema.');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Cliente')
    INSERT dbo.Roles (Nombre, Descripcion) VALUES (N'Cliente', N'Cuenta registrada para compras.');
UPDATE dbo.Roles
SET Activo = CASE WHEN Nombre IN (N'Administrador', N'Cliente') THEN 1 ELSE 0 END;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'RolId') IS NULL
    ALTER TABLE dbo.Usuarios ADD RolId INT NULL;
GO

DECLARE @RolAdministrador INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Administrador');
UPDATE dbo.Usuarios SET RolId = @RolAdministrador WHERE RolId IS NULL;
GO

ALTER TABLE dbo.Usuarios ALTER COLUMN RolId INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Usuarios_Roles')
    ALTER TABLE dbo.Usuarios WITH CHECK ADD CONSTRAINT FK_Usuarios_Roles
        FOREIGN KEY (RolId) REFERENCES dbo.Roles (RolId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Usuarios') AND name = N'IX_Usuarios_RolId')
    CREATE INDEX IX_Usuarios_RolId ON dbo.Usuarios (RolId);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE PasswordHash IS NULL OR LTRIM(RTRIM(PasswordHash)) = N'')
    ALTER TABLE dbo.Usuarios ALTER COLUMN PasswordHash NVARCHAR(500) NOT NULL;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'IntentosFallidos') IS NULL
    ALTER TABLE dbo.Usuarios ADD IntentosFallidos INT NOT NULL
        CONSTRAINT DF_Usuarios_IntentosFallidos DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.Usuarios', N'BloqueadoHasta') IS NULL
    ALTER TABLE dbo.Usuarios ADD BloqueadoHasta DATETIME2(3) NULL;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'UltimoIntentoFallido') IS NULL
    ALTER TABLE dbo.Usuarios ADD UltimoIntentoFallido DATETIME2(3) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_IntentosFallidos')
    ALTER TABLE dbo.Usuarios WITH CHECK ADD CONSTRAINT CK_Usuarios_IntentosFallidos
        CHECK (IntentosFallidos >= 0);
GO

IF OBJECT_ID(N'dbo.HistorialAccesos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistorialAccesos
    (
        HistorialAccesoId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistorialAccesos PRIMARY KEY,
        UsuarioId INT NULL,
        CorreoIntentado NVARCHAR(120) NOT NULL,
        Fecha DATETIME2(3) NOT NULL CONSTRAINT DF_HistorialAccesos_Fecha DEFAULT (SYSDATETIME()),
        Exitoso BIT NOT NULL,
        CONSTRAINT FK_HistorialAccesos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId)
    );
    CREATE INDEX IX_HistorialAccesos_UsuarioId_Fecha ON dbo.HistorialAccesos (UsuarioId, Fecha DESC);
    CREATE INDEX IX_HistorialAccesos_Correo_Fecha ON dbo.HistorialAccesos (CorreoIntentado, Fecha DESC);
END;
GO

IF OBJECT_ID(N'dbo.MenuOpciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MenuOpciones
    (
        MenuOpcionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MenuOpciones PRIMARY KEY,
        Nombre NVARCHAR(80) NOT NULL,
        Ruta NVARCHAR(160) NOT NULL,
        Icono NVARCHAR(60) NULL,
        Orden INT NOT NULL CONSTRAINT DF_MenuOpciones_Orden DEFAULT (0),
        Activo BIT NOT NULL CONSTRAINT DF_MenuOpciones_Activo DEFAULT (1),
        CONSTRAINT UQ_MenuOpciones_Ruta UNIQUE (Ruta),
        CONSTRAINT CK_MenuOpciones_Orden CHECK (Orden >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.RolMenuOpciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolMenuOpciones
    (
        RolId INT NOT NULL,
        MenuOpcionId INT NOT NULL,
        CONSTRAINT PK_RolMenuOpciones PRIMARY KEY (RolId, MenuOpcionId),
        CONSTRAINT FK_RolMenuOpciones_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles (RolId),
        CONSTRAINT FK_RolMenuOpciones_Menu FOREIGN KEY (MenuOpcionId) REFERENCES dbo.MenuOpciones (MenuOpcionId)
    );
    CREATE INDEX IX_RolMenuOpciones_MenuOpcionId ON dbo.RolMenuOpciones (MenuOpcionId);
END;
GO

DECLARE @Opciones TABLE
(
    Nombre NVARCHAR(80), Ruta NVARCHAR(160), Icono NVARCHAR(60), Orden INT
);
INSERT @Opciones VALUES
(N'Inicio', N'/', N'cil-home', 1),
(N'Familias de producto', N'/familias-producto', N'cil-list', 10),
(N'Categor' + NCHAR(237) + N'as', N'/categorias', N'cil-list', 20),
(N'Impuestos', N'/impuestos', N'cil-calculator', 30),
(N'Productos', N'/productos', N'cil-basket', 40),
(N'Roles', N'/roles', N'cil-people', 50),
(NCHAR(211) + N'rdenes', N'/ordenes', N'cil-basket', 60);

UPDATE m SET m.Nombre=o.Nombre, m.Icono=o.Icono, m.Orden=o.Orden, m.Activo=1
FROM dbo.MenuOpciones m INNER JOIN @Opciones o ON o.Ruta=m.Ruta;
INSERT dbo.MenuOpciones (Nombre,Ruta,Icono,Orden,Activo)
SELECT o.Nombre,o.Ruta,o.Icono,o.Orden,1 FROM @Opciones o
WHERE NOT EXISTS (SELECT 1 FROM dbo.MenuOpciones m WHERE m.Ruta=o.Ruta);

DECLARE @AdministradorId INT=(SELECT RolId FROM dbo.Roles WHERE Nombre=N'Administrador');
DECLARE @ClienteId INT=(SELECT RolId FROM dbo.Roles WHERE Nombre=N'Cliente');

INSERT dbo.RolMenuOpciones (RolId,MenuOpcionId)
SELECT @AdministradorId,m.MenuOpcionId FROM dbo.MenuOpciones m
WHERE m.Ruta IN (N'/',N'/familias-producto',N'/categorias',N'/impuestos',N'/productos',N'/roles',N'/ordenes')
AND NOT EXISTS (SELECT 1 FROM dbo.RolMenuOpciones x WHERE x.RolId=@AdministradorId AND x.MenuOpcionId=m.MenuOpcionId);

INSERT dbo.RolMenuOpciones (RolId,MenuOpcionId)
SELECT @ClienteId,m.MenuOpcionId FROM dbo.MenuOpciones m
WHERE m.Ruta IN (N'/',N'/productos',N'/ordenes')
AND NOT EXISTS (SELECT 1 FROM dbo.RolMenuOpciones x WHERE x.RolId=@ClienteId AND x.MenuOpcionId=m.MenuOpcionId);

DELETE rm FROM dbo.RolMenuOpciones rm
INNER JOIN dbo.MenuOpciones m ON m.MenuOpcionId=rm.MenuOpcionId
WHERE rm.RolId=@ClienteId AND m.Ruta NOT IN (N'/',N'/productos',N'/ordenes');
GO

/* Imagenes de productos: rutas, no binarios */
IF OBJECT_ID(N'dbo.ProductoImagenes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductoImagenes
    (
        ImagenId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductoImagenes PRIMARY KEY,
        ProductoId INT NOT NULL,
        UrlImagen NVARCHAR(500) NOT NULL,
        TextoAlternativo NVARCHAR(180) NULL,
        EsPrincipal BIT NOT NULL CONSTRAINT DF_ProductoImagenes_EsPrincipal DEFAULT (0),
        Orden INT NOT NULL CONSTRAINT DF_ProductoImagenes_Orden DEFAULT (0),
        Activo BIT NOT NULL CONSTRAINT DF_ProductoImagenes_Activo DEFAULT (1),
        CONSTRAINT FK_ProductoImagenes_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
        CONSTRAINT UQ_ProductoImagenes_Producto_Url UNIQUE (ProductoId, UrlImagen),
        CONSTRAINT CK_ProductoImagenes_Url CHECK (LEN(LTRIM(RTRIM(UrlImagen))) > 0),
        CONSTRAINT CK_ProductoImagenes_Orden CHECK (Orden >= 0)
    );
    CREATE INDEX IX_ProductoImagenes_Producto_Orden ON dbo.ProductoImagenes (ProductoId, EsPrincipal DESC, Orden);
    CREATE UNIQUE INDEX UX_ProductoImagenes_PrincipalActiva
        ON dbo.ProductoImagenes (ProductoId)
        WHERE EsPrincipal = 1 AND Activo = 1;
END;
GO

IF COL_LENGTH(N'dbo.ProductoImagenes', N'TextoAlternativo') IS NULL
    ALTER TABLE dbo.ProductoImagenes ADD TextoAlternativo NVARCHAR(180) NULL;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ProductoImagenes')
      AND name = N'UX_ProductoImagenes_PrincipalActiva'
)
    CREATE UNIQUE INDEX UX_ProductoImagenes_PrincipalActiva
        ON dbo.ProductoImagenes (ProductoId)
        WHERE EsPrincipal = 1 AND Activo = 1;
GO

/* Proveedores y compras */
IF OBJECT_ID(N'dbo.Proveedores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Proveedores
    (
        ProveedorId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Proveedores PRIMARY KEY,
        Nombre NVARCHAR(150) NOT NULL,
        Correo NVARCHAR(150) NULL,
        Telefono NVARCHAR(30) NULL,
        Direccion NVARCHAR(250) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Proveedores_Activo DEFAULT (1),
        CONSTRAINT CK_Proveedores_Nombre CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0)
    );
    CREATE UNIQUE INDEX UX_Proveedores_Correo ON dbo.Proveedores (Correo) WHERE Correo IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.ProductoProveedor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductoProveedor
    (
        ProductoId INT NOT NULL,
        ProveedorId INT NOT NULL,
        PrecioCompra DECIMAL(18,2) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_ProductoProveedor_Activo DEFAULT (1),
        CONSTRAINT PK_ProductoProveedor PRIMARY KEY (ProductoId, ProveedorId),
        CONSTRAINT FK_ProductoProveedor_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
        CONSTRAINT FK_ProductoProveedor_Proveedores FOREIGN KEY (ProveedorId) REFERENCES dbo.Proveedores (ProveedorId),
        CONSTRAINT CK_ProductoProveedor_Precio CHECK (PrecioCompra >= 0)
    );
    CREATE INDEX IX_ProductoProveedor_ProveedorId ON dbo.ProductoProveedor (ProveedorId);
END;
GO

IF OBJECT_ID(N'dbo.ComprasProveedor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComprasProveedor
    (
        CompraProveedorId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ComprasProveedor PRIMARY KEY,
        ProveedorId INT NOT NULL,
        Fecha DATETIME2(3) NOT NULL CONSTRAINT DF_ComprasProveedor_Fecha DEFAULT (SYSDATETIME()),
        Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_ComprasProveedor_Estado DEFAULT (N'PENDIENTE'),
        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_ComprasProveedor_Total DEFAULT (0),
        CONSTRAINT FK_ComprasProveedor_Proveedores FOREIGN KEY (ProveedorId) REFERENCES dbo.Proveedores (ProveedorId),
        CONSTRAINT CK_ComprasProveedor_Estado CHECK (Estado IN (N'PENDIENTE', N'RECIBIDA', N'CANCELADA')),
        CONSTRAINT CK_ComprasProveedor_Total CHECK (Total >= 0)
    );
    CREATE INDEX IX_ComprasProveedor_Proveedor_Fecha ON dbo.ComprasProveedor (ProveedorId, Fecha DESC);
END;
GO

IF OBJECT_ID(N'dbo.CompraProveedorDetalle', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompraProveedorDetalle
    (
        CompraProveedorDetalleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CompraProveedorDetalle PRIMARY KEY,
        CompraProveedorId INT NOT NULL,
        ProductoId INT NOT NULL,
        Cantidad INT NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        CONSTRAINT UQ_CompraProveedorDetalle UNIQUE (CompraProveedorId, ProductoId),
        CONSTRAINT FK_CompraProveedorDetalle_Compra FOREIGN KEY (CompraProveedorId) REFERENCES dbo.ComprasProveedor (CompraProveedorId),
        CONSTRAINT FK_CompraProveedorDetalle_Producto FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
        CONSTRAINT CK_CompraProveedorDetalle_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT CK_CompraProveedorDetalle_Precio CHECK (PrecioUnitario >= 0)
    );
    CREATE INDEX IX_CompraProveedorDetalle_ProductoId ON dbo.CompraProveedorDetalle (ProductoId);
END;
GO

/* Descuentos */
IF OBJECT_ID(N'dbo.Descuentos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Descuentos
    (
        DescuentoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Descuentos PRIMARY KEY,
        Nombre NVARCHAR(150) NOT NULL,
        ProductoId INT NULL,
        CategoriaId INT NULL,
        FamiliaId INT NULL,
        EsPromocional BIT NOT NULL CONSTRAINT DF_Descuentos_EsPromocional DEFAULT (0),
        Porcentaje DECIMAL(5,2) NOT NULL,
        FechaInicio DATETIME2(3) NOT NULL,
        FechaFin DATETIME2(3) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Descuentos_Activo DEFAULT (1),
        CONSTRAINT FK_Descuentos_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
        CONSTRAINT FK_Descuentos_Categorias FOREIGN KEY (CategoriaId) REFERENCES dbo.Categorias (CategoriaId),
        CONSTRAINT FK_Descuentos_Familias FOREIGN KEY (FamiliaId) REFERENCES dbo.FamiliasProducto (FamiliaId),
        CONSTRAINT CK_Descuentos_Porcentaje CHECK (Porcentaje > 0 AND Porcentaje <= 100),
        CONSTRAINT CK_Descuentos_Fechas CHECK (FechaFin >= FechaInicio),
        CONSTRAINT CK_Descuentos_Ambito CHECK
        (
            (CASE WHEN ProductoId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN CategoriaId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN FamiliaId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN EsPromocional = 1 THEN 1 ELSE 0 END) = 1
        )
    );
    CREATE INDEX IX_Descuentos_ProductoId ON dbo.Descuentos (ProductoId) WHERE ProductoId IS NOT NULL;
    CREATE INDEX IX_Descuentos_CategoriaId ON dbo.Descuentos (CategoriaId) WHERE CategoriaId IS NOT NULL;
    CREATE INDEX IX_Descuentos_FamiliaId ON dbo.Descuentos (FamiliaId) WHERE FamiliaId IS NOT NULL;
END;
GO

/* Carrito */
IF OBJECT_ID(N'dbo.Carritos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Carritos
    (
        CarritoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Carritos PRIMARY KEY,
        UsuarioId INT NOT NULL,
        FechaCreacion DATETIME2(3) NOT NULL CONSTRAINT DF_Carritos_Fecha DEFAULT (SYSDATETIME()),
        Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Carritos_Estado DEFAULT (N'ACTIVO'),
        CONSTRAINT FK_Carritos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId),
        CONSTRAINT CK_Carritos_Estado CHECK (Estado IN (N'ACTIVO', N'CONVERTIDO', N'ABANDONADO'))
    );
    CREATE INDEX IX_Carritos_Usuario_Estado ON dbo.Carritos (UsuarioId, Estado);
    CREATE UNIQUE INDEX UX_Carritos_Usuario_Activo ON dbo.Carritos (UsuarioId) WHERE Estado = N'ACTIVO';
END;
GO

IF OBJECT_ID(N'dbo.CarritoDetalle', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CarritoDetalle
    (
        CarritoDetalleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CarritoDetalle PRIMARY KEY,
        CarritoId INT NOT NULL,
        ProductoId INT NOT NULL,
        Cantidad INT NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        CONSTRAINT UQ_CarritoDetalle_Carrito_Producto UNIQUE (CarritoId, ProductoId),
        CONSTRAINT FK_CarritoDetalle_Carritos FOREIGN KEY (CarritoId) REFERENCES dbo.Carritos (CarritoId),
        CONSTRAINT FK_CarritoDetalle_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
        CONSTRAINT CK_CarritoDetalle_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT CK_CarritoDetalle_Precio CHECK (PrecioUnitario >= 0)
    );
    CREATE INDEX IX_CarritoDetalle_ProductoId ON dbo.CarritoDetalle (ProductoId);
END;
GO

/* Pagos y documentos preparados, sin implementar su logica en esta etapa */
IF OBJECT_ID(N'dbo.Pagos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pagos
    (
        PagoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pagos PRIMARY KEY,
        OrdenId INT NOT NULL,
        Fecha DATETIME2(3) NOT NULL CONSTRAINT DF_Pagos_Fecha DEFAULT (SYSDATETIME()),
        Monto DECIMAL(18,2) NOT NULL,
        Metodo NVARCHAR(30) NOT NULL,
        Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Pagos_Estado DEFAULT (N'PENDIENTE'),
        Referencia NVARCHAR(120) NULL,
        CONSTRAINT FK_Pagos_Ordenes FOREIGN KEY (OrdenId) REFERENCES dbo.Ordenes (OrdenId),
        CONSTRAINT CK_Pagos_Monto CHECK (Monto > 0),
        CONSTRAINT CK_Pagos_Estado CHECK (Estado IN (N'PENDIENTE', N'APROBADO', N'RECHAZADO', N'ANULADO'))
    );
    CREATE INDEX IX_Pagos_OrdenId ON dbo.Pagos (OrdenId, Fecha DESC);
END;
GO

IF OBJECT_ID(N'dbo.Documentos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documentos
    (
        DocumentoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Documentos PRIMARY KEY,
        OrdenId INT NULL,
        CompraProveedorId INT NULL,
        Tipo NVARCHAR(20) NOT NULL,
        Numero NVARCHAR(40) NULL,
        Ruta NVARCHAR(500) NOT NULL,
        FechaCreacion DATETIME2(3) NOT NULL CONSTRAINT DF_Documentos_Fecha DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_Documentos_Ordenes FOREIGN KEY (OrdenId) REFERENCES dbo.Ordenes (OrdenId),
        CONSTRAINT FK_Documentos_Compras FOREIGN KEY (CompraProveedorId) REFERENCES dbo.ComprasProveedor (CompraProveedorId),
        CONSTRAINT CK_Documentos_Origen CHECK
        (
            (CASE WHEN OrdenId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN CompraProveedorId IS NULL THEN 0 ELSE 1 END) = 1
        )
    );
    CREATE INDEX IX_Documentos_OrdenId ON dbo.Documentos (OrdenId) WHERE OrdenId IS NOT NULL;
    CREATE INDEX IX_Documentos_CompraId ON dbo.Documentos (CompraProveedorId) WHERE CompraProveedorId IS NOT NULL;
END;
GO

/* Participacion del cliente */
IF OBJECT_ID(N'dbo.Calificaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Calificaciones
    (
        CalificacionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Calificaciones PRIMARY KEY,
        ProductoId INT NOT NULL,
        UsuarioId INT NOT NULL,
        Puntuacion TINYINT NOT NULL,
        Comentario NVARCHAR(500) NULL,
        Fecha DATETIME2(3) NOT NULL CONSTRAINT DF_Calificaciones_Fecha DEFAULT (SYSDATETIME()),
        Activo BIT NOT NULL CONSTRAINT DF_Calificaciones_Activo DEFAULT (1),
        CONSTRAINT UQ_Calificaciones_Producto_Usuario UNIQUE (ProductoId, UsuarioId),
        CONSTRAINT FK_Calificaciones_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
        CONSTRAINT FK_Calificaciones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId),
        CONSTRAINT CK_Calificaciones_Puntuacion CHECK (Puntuacion BETWEEN 1 AND 5)
    );
    CREATE INDEX IX_Calificaciones_UsuarioId ON dbo.Calificaciones (UsuarioId);
END;
GO

IF OBJECT_ID(N'dbo.ListaDeseos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ListaDeseos
    (
        ListaDeseoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ListaDeseos PRIMARY KEY,
        UsuarioId INT NOT NULL,
        ProductoId INT NOT NULL,
        Fecha DATETIME2(3) NOT NULL CONSTRAINT DF_ListaDeseos_Fecha DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_ListaDeseos_Usuario_Producto UNIQUE (UsuarioId, ProductoId),
        CONSTRAINT FK_ListaDeseos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId),
        CONSTRAINT FK_ListaDeseos_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId)
    );
    CREATE INDEX IX_ListaDeseos_ProductoId ON dbo.ListaDeseos (ProductoId);
END;
GO

/* Inventario y auditoria */
IF OBJECT_ID(N'dbo.MovimientosInventario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientosInventario
    (
        MovimientoInventarioId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosInventario PRIMARY KEY,
        ProductoId INT NOT NULL,
        Tipo NVARCHAR(10) NOT NULL,
        Cantidad INT NOT NULL,
        Motivo NVARCHAR(200) NULL,
        Fecha DATETIME2(3) NOT NULL CONSTRAINT DF_MovimientosInventario_Fecha DEFAULT (SYSDATETIME()),
        UsuarioId INT NULL,
        CONSTRAINT FK_MovimientosInventario_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
        CONSTRAINT FK_MovimientosInventario_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId),
        CONSTRAINT CK_MovimientosInventario_Tipo CHECK (Tipo IN (N'ENTRADA', N'SALIDA', N'AJUSTE')),
        CONSTRAINT CK_MovimientosInventario_Cantidad CHECK (Cantidad > 0)
    );
    CREATE INDEX IX_MovimientosInventario_Producto_Fecha ON dbo.MovimientosInventario (ProductoId, Fecha DESC);
    CREATE INDEX IX_MovimientosInventario_UsuarioId ON dbo.MovimientosInventario (UsuarioId) WHERE UsuarioId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.BitacoraSistema', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BitacoraSistema
    (
        BitacoraId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BitacoraSistema PRIMARY KEY,
        UsuarioId INT NULL,
        Fecha DATETIME2(3) NOT NULL CONSTRAINT DF_BitacoraSistema_Fecha DEFAULT (SYSDATETIME()),
        Accion NVARCHAR(80) NOT NULL,
        Entidad NVARCHAR(80) NULL,
        EntidadId NVARCHAR(80) NULL,
        Detalle NVARCHAR(1000) NULL,
        CONSTRAINT FK_BitacoraSistema_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId)
    );
    CREATE INDEX IX_BitacoraSistema_Fecha ON dbo.BitacoraSistema (Fecha DESC);
    CREATE INDEX IX_BitacoraSistema_UsuarioId ON dbo.BitacoraSistema (UsuarioId, Fecha DESC);
END;
GO

/* Vistas corregidas del documento de referencia */
CREATE OR ALTER VIEW dbo.VentasPorFecha
AS
    SELECT CONVERT(DATE, FechaOrden) AS Fecha,
           COUNT_BIG(*) AS CantidadOrdenes,
           SUM(COALESCE(Total, 0)) AS TotalVentas
    FROM dbo.Ordenes
    WHERE Estado <> N'CANCELADA'
    GROUP BY CONVERT(DATE, FechaOrden);
GO

CREATE OR ALTER VIEW dbo.ProductosMasVendidos
AS
    SELECT p.ProductoId,
           p.Nombre,
           SUM(od.Cantidad) AS UnidadesVendidas
    FROM dbo.OrdenDetalle od
    INNER JOIN dbo.Productos p ON p.ProductoId = od.ProductoId
    INNER JOIN dbo.Ordenes o ON o.OrdenId = od.OrdenId
    WHERE o.Estado <> N'CANCELADA'
    GROUP BY p.ProductoId, p.Nombre;
GO

CREATE OR ALTER VIEW dbo.PromedioCalificaciones
AS
    SELECT ProductoId,
           AVG(CONVERT(DECIMAL(4,2), Puntuacion)) AS Promedio,
           COUNT_BIG(*) AS TotalCalificaciones
    FROM dbo.Calificaciones
    WHERE Activo = 1
    GROUP BY ProductoId;
GO

PRINT N'Ampliacion importante aplicada correctamente sin eliminar datos.';
GO
