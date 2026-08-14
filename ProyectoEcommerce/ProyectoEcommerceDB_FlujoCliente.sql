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

-- estas columnas guardan lo que se uso al comprar para que una orden vieja no cambie despues
-- COL_LENGTH deja correr el script otra vez sin intentar agregar la misma columna
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

-- quita la regla anterior para reemplazarla por la lista completa de estados del flujo
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

-- Documentos conserva sus nombres Database First y agrega solo correo y estado de envio
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

-- este procedimiento es el unico punto que descuenta inventario por una venta
-- revisa orden y stock, bloquea productos, resta cantidades y confirma la orden
-- puede usar la transaccion de la LN o crear una propia si se ejecuta solo
CREATE OR ALTER PROCEDURE dbo.sp_ConfirmarOrdenVenta
    @OrdenId INT,
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- @@TRANCOUNT dice si el procedimiento ya entro dentro de otra transaccion
    DECLARE @TransaccionPropia BIT = CASE WHEN @@TRANCOUNT = 0 THEN 1 ELSE 0 END;

    -- solo abre y cierra transaccion cuando nadie de afuera le paso una
    IF @TransaccionPropia = 1 BEGIN TRANSACTION;

    BEGIN TRY
        -- primero bloquea la orden mientras revisa dueño, tipo y estado
        -- UPDLOCK y HOLDLOCK mantienen ese bloqueo hasta Commit o Rollback
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

        -- una orden sin lineas no puede afectar inventario ni confirmarse
        IF NOT EXISTS (SELECT 1 FROM dbo.OrdenDetalle WHERE OrdenId = @OrdenId)
            THROW 51002, N'La orden no contiene productos.', 1;

        -- aqui bloquea los productos y cuenta cuantos estan inactivos o sin cantidad suficiente
        DECLARE @ProductosSinStock INT;
        SELECT @ProductosSinStock = COUNT(*)
        FROM dbo.OrdenDetalle od
        INNER JOIN dbo.Productos p WITH (UPDLOCK, HOLDLOCK) ON p.ProductoId = od.ProductoId
        WHERE od.OrdenId = @OrdenId
          AND (p.Activo = 0 OR p.Stock < od.Cantidad);

        -- THROW detiene todo y la LN reconoce 51001 como conflicto de stock
        IF @ProductosSinStock > 0
            THROW 51001, N'Uno o más productos ya no tienen stock suficiente. Revisa tu carrito.', 1;

        -- cuando todos pasaron la revision ya resta cada cantidad comprada
        UPDATE p
           SET p.Stock = p.Stock - od.Cantidad
        FROM dbo.Productos p
        INNER JOIN dbo.OrdenDetalle od ON od.ProductoId = p.ProductoId
        WHERE od.OrdenId = @OrdenId;

        -- guarda una salida de inventario por producto para poder seguir el historial
        INSERT INTO dbo.MovimientosInventario (ProductoId, Tipo, Cantidad, Motivo, Fecha, UsuarioId)
        SELECT od.ProductoId, N'SALIDA', od.Cantidad,
               CONCAT(N'Venta confirmada - Orden #', @OrdenId), SYSDATETIME(), @UsuarioId
        FROM dbo.OrdenDetalle od
        WHERE od.OrdenId = @OrdenId;

        -- la orden se confirma solamente despues de actualizar todo el stock
        UPDATE dbo.Ordenes
           SET Estado = N'CONFIRMADA'
         WHERE OrdenId = @OrdenId;

        -- deja una nota general de quien confirmo la venta
        INSERT INTO dbo.BitacoraSistema (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
        VALUES (@UsuarioId, SYSDATETIME(), N'CONFIRMAR_ORDEN_VENTA', N'Orden',
                CONVERT(NVARCHAR(80), @OrdenId), N'Inventario descontado mediante sp_ConfirmarOrdenVenta.');

        -- Commit solo corresponde aqui si el procedimiento fue quien abrio la transaccion
        IF @TransaccionPropia = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- ante cualquier error devuelve sus cambios y vuelve a lanzar el mismo error hacia la LN
        IF @TransaccionPropia = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
