/* =========================================================
   Completa el flujo de venta del Cliente: datos históricos de orden,
   restricciones, documentos y confirmación segura de inventario.
   Es incremental y no recrea la base de datos.
   ========================================================= */

USE ProyectoEcommerceDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* Datos históricos propios de la orden de venta. */
IF COL_LENGTH(N'dbo.Ordenes', N'TipoOrden') IS NULL
BEGIN
    ALTER TABLE dbo.Ordenes ADD TipoOrden NVARCHAR(10) NOT NULL
        CONSTRAINT DF_Ordenes_TipoOrden DEFAULT (N'VENTA') WITH VALUES;
END;
GO

IF COL_LENGTH(N'dbo.Ordenes', N'DireccionEnvio') IS NULL
BEGIN
    ALTER TABLE dbo.Ordenes ADD DireccionEnvio NVARCHAR(500) NULL;
END;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Ordenes_Estado')
    ALTER TABLE dbo.Ordenes DROP CONSTRAINT CK_Ordenes_Estado;
GO

ALTER TABLE dbo.Ordenes WITH CHECK ADD CONSTRAINT CK_Ordenes_Estado
    CHECK (Estado IN (N'PROFORMA', N'PENDIENTE', N'CONFIRMADA', N'FACTURADA', N'CANCELADA'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Ordenes_TipoOrden')
BEGIN
    ALTER TABLE dbo.Ordenes WITH CHECK ADD CONSTRAINT CK_Ordenes_TipoOrden
        CHECK (TipoOrden IN (N'VENTA', N'COMPRA'));
END;
GO

IF COL_LENGTH(N'dbo.OrdenDetalle', N'PorcentajeDescuento') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenDetalle ADD PorcentajeDescuento DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_OrdenDetalle_PorcentajeDescuento DEFAULT (0) WITH VALUES;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenDetalle_PorcentajeDescuento')
BEGIN
    ALTER TABLE dbo.OrdenDetalle WITH CHECK ADD CONSTRAINT CK_OrdenDetalle_PorcentajeDescuento
        CHECK (PorcentajeDescuento >= 0 AND PorcentajeDescuento <= 100);
END;
GO

/* Documentos conserva sus nombres Database First: Tipo, Numero, Ruta y FechaCreacion. */
IF COL_LENGTH(N'dbo.Documentos', N'CorreoDestino') IS NULL
BEGIN
    ALTER TABLE dbo.Documentos ADD CorreoDestino NVARCHAR(120) COLLATE SQL_Latin1_General_CP1_CI_AS NULL;
END;
GO

ALTER TABLE dbo.Documentos ALTER COLUMN CorreoDestino
    NVARCHAR(120) COLLATE SQL_Latin1_General_CP1_CI_AS NULL;
GO

IF COL_LENGTH(N'dbo.Documentos', N'EnviadoCorreo') IS NULL
BEGIN
    ALTER TABLE dbo.Documentos ADD EnviadoCorreo BIT NOT NULL
        CONSTRAINT DF_Documentos_EnviadoCorreo DEFAULT (0) WITH VALUES;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Documentos') AND name = N'UX_Documentos_Numero')
BEGIN
    CREATE UNIQUE INDEX UX_Documentos_Numero ON dbo.Documentos (Numero) WHERE Numero IS NOT NULL;
END;
GO

/*
   Único punto que descuenta inventario para una venta.
   Verifica que la orden esté pendiente y tenga stock, bloquea los productos
   para evitar sobreventa, descuenta existencias, registra movimientos de
   inventario y cambia la orden a CONFIRMADA.
   Funciona dentro de una transacción exterior o crea la suya si se invoca directamente.
*/
CREATE OR ALTER PROCEDURE dbo.sp_ConfirmarOrdenVenta
    @OrdenId INT,
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TransaccionPropia BIT = CASE WHEN @@TRANCOUNT = 0 THEN 1 ELSE 0 END;

    IF @TransaccionPropia = 1 BEGIN TRANSACTION;

    BEGIN TRY
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Ordenes WITH (UPDLOCK, HOLDLOCK)
            WHERE OrdenId = @OrdenId
              AND UsuarioId = @UsuarioId
              AND TipoOrden = N'VENTA'
              AND Estado = N'PENDIENTE'
        )
            THROW 51000, N'La orden de venta no existe o no está pendiente.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.OrdenDetalle WHERE OrdenId = @OrdenId)
            THROW 51002, N'La orden no contiene productos.', 1;

        DECLARE @ProductosSinStock INT;
        SELECT @ProductosSinStock = COUNT(*)
        FROM dbo.OrdenDetalle od
        INNER JOIN dbo.Productos p WITH (UPDLOCK, HOLDLOCK) ON p.ProductoId = od.ProductoId
        WHERE od.OrdenId = @OrdenId
          AND (p.Activo = 0 OR p.Stock < od.Cantidad);

        IF @ProductosSinStock > 0
            THROW 51001, N'Uno o más productos ya no tienen stock suficiente. Revisa tu carrito.', 1;

        UPDATE p
           SET p.Stock = p.Stock - od.Cantidad
        FROM dbo.Productos p
        INNER JOIN dbo.OrdenDetalle od ON od.ProductoId = p.ProductoId
        WHERE od.OrdenId = @OrdenId;

        INSERT INTO dbo.MovimientosInventario (ProductoId, Tipo, Cantidad, Motivo, Fecha, UsuarioId)
        SELECT od.ProductoId, N'SALIDA', od.Cantidad,
               CONCAT(N'Venta confirmada - Orden #', @OrdenId), SYSDATETIME(), @UsuarioId
        FROM dbo.OrdenDetalle od
        WHERE od.OrdenId = @OrdenId;

        UPDATE dbo.Ordenes
           SET Estado = N'CONFIRMADA'
         WHERE OrdenId = @OrdenId;

        INSERT INTO dbo.BitacoraSistema (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
        VALUES (@UsuarioId, SYSDATETIME(), N'CONFIRMAR_ORDEN_VENTA', N'Orden',
                CONVERT(NVARCHAR(80), @OrdenId), N'Inventario descontado mediante sp_ConfirmarOrdenVenta.');

        IF @TransaccionPropia = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @TransaccionPropia = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
