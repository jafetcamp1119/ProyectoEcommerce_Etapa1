/* =========================================================
   Limpieza controlada de registros QA identificados explícitamente.
   El orden de eliminación respeta claves foráneas y toda la operación
   se confirma o revierte como una sola transacción.
   ========================================================= */

USE ProyectoEcommerceDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @UsuariosPrueba TABLE
    (
        UsuarioId INT NOT NULL PRIMARY KEY,
        Correo NVARCHAR(120) NOT NULL UNIQUE
    );

    INSERT INTO @UsuariosPrueba (UsuarioId, Correo)
    SELECT UsuarioId, Correo
    FROM dbo.Usuarios
    WHERE Correo IN
    (
        N'codex.product.admin.20260804@example.test',
        N'codex.product.client.20260804@example.test'
    );

    DECLARE @ProductosPrueba TABLE
    (
        ProductoId INT NOT NULL PRIMARY KEY,
        Codigo NVARCHAR(50) NOT NULL UNIQUE
    );

    INSERT INTO @ProductosPrueba (ProductoId, Codigo)
    SELECT ProductoId, Codigo
    FROM dbo.Productos
    WHERE Codigo IN
    (
        N'CODEX-PROD-QA-A',
        N'CODEX-PROD-QA-B',
        N'CODEX-PROD-QA-C',
        N'CODEX-PROD-QA-D'
    );

    /* Dependencias exactas de los carritos de las cuentas de prueba. */
    DELETE detalle
    FROM dbo.CarritoDetalle AS detalle
    INNER JOIN dbo.Carritos AS carrito ON carrito.CarritoId = detalle.CarritoId
    INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = carrito.UsuarioId;

    DELETE carrito
    FROM dbo.Carritos AS carrito
    INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = carrito.UsuarioId;

    /* Dependencias exactas de los cuatro productos QA confirmados. */
    DELETE registro FROM dbo.ProductoImagenes AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;
    DELETE registro FROM dbo.CarritoDetalle AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;
    DELETE registro FROM dbo.OrdenDetalle AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;
    DELETE registro FROM dbo.Calificaciones AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;
    DELETE registro FROM dbo.CompraProveedorDetalle AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;
    DELETE registro FROM dbo.Descuentos AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;
    DELETE registro FROM dbo.ListaDeseos AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;
    DELETE registro FROM dbo.MovimientosInventario AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;
    DELETE registro FROM dbo.ProductoProveedor AS registro INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = registro.ProductoId;

    DELETE bitacora
    FROM dbo.BitacoraSistema AS bitacora
    INNER JOIN @ProductosPrueba AS objetivo
        ON bitacora.Entidad = N'Producto'
       AND bitacora.EntidadId = CONVERT(NVARCHAR(80), objetivo.ProductoId);

    DELETE producto
    FROM dbo.Productos AS producto
    INNER JOIN @ProductosPrueba AS objetivo ON objetivo.ProductoId = producto.ProductoId;

    /* Dependencias exactas de las dos cuentas Codex. */
    DELETE registro FROM dbo.Calificaciones AS registro INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = registro.UsuarioId;
    DELETE registro FROM dbo.ListaDeseos AS registro INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = registro.UsuarioId;
    DELETE registro FROM dbo.MovimientosInventario AS registro INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = registro.UsuarioId;
    DELETE registro FROM dbo.HistorialAccesos AS registro INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = registro.UsuarioId;
    DELETE registro FROM dbo.HistorialAccesos AS registro WHERE registro.CorreoIntentado IN
    (
        N'codex.product.admin.20260804@example.test',
        N'codex.product.client.20260804@example.test'
    );
    DELETE registro FROM dbo.BitacoraSistema AS registro INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = registro.UsuarioId;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Ordenes AS orden
        INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = orden.UsuarioId
    )
        THROW 51001, N'La limpieza se detuvo: una cuenta Codex tiene órdenes que requieren revisión manual.', 1;

    DELETE usuario
    FROM dbo.Usuarios AS usuario
    INNER JOIN @UsuariosPrueba AS objetivo ON objetivo.UsuarioId = usuario.UsuarioId;

    IF EXISTS
    (
        SELECT 1 FROM dbo.Usuarios
        WHERE Correo IN
        (
            N'codex.product.admin.20260804@example.test',
            N'codex.product.client.20260804@example.test'
        )
    )
        THROW 51002, N'La limpieza no pudo eliminar las dos cuentas Codex.', 1;

    IF EXISTS
    (
        SELECT 1 FROM dbo.Productos
        WHERE Codigo IN
        (
            N'CODEX-PROD-QA-A',
            N'CODEX-PROD-QA-B',
            N'CODEX-PROD-QA-C',
            N'CODEX-PROD-QA-D'
        )
    )
        THROW 51003, N'La limpieza no pudo eliminar los cuatro productos QA.', 1;

    DECLARE @CuentasEliminadas INT = (SELECT COUNT(*) FROM @UsuariosPrueba);
    DECLARE @ProductosEliminados INT = (SELECT COUNT(*) FROM @ProductosPrueba);

    COMMIT TRANSACTION;

    SELECT @CuentasEliminadas AS CuentasCodexEliminadas,
           @ProductosEliminados AS ProductosQaEliminados;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
