/* =========================================================
   Configura la opción de menú del carrito para el rol Cliente.
   La transacción hace que menú y permiso se apliquen juntos o se reviertan.
   ========================================================= */

USE ProyectoEcommerceDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.Carritos', N'U') IS NULL
   OR OBJECT_ID(N'dbo.CarritoDetalle', N'U') IS NULL
   OR OBJECT_ID(N'dbo.MenuOpciones', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RolMenuOpciones', N'U') IS NULL
    THROW 51100, N'Faltan tablas requeridas. Ejecute primero la ampliación importante.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.MenuOpciones WHERE Ruta = N'/carrito')
    BEGIN
        INSERT dbo.MenuOpciones (Nombre, Ruta, Icono, Orden, Activo)
        VALUES (N'Carrito', N'/carrito', N'cil-basket', 45, 1);
    END;
    ELSE
    BEGIN
        UPDATE dbo.MenuOpciones
        SET Nombre = N'Carrito', Icono = N'cil-basket', Orden = 45, Activo = 1
        WHERE Ruta = N'/carrito';
    END;

    DECLARE @RolClienteId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Cliente' AND Activo = 1);
    DECLARE @MenuCarritoId INT = (SELECT MenuOpcionId FROM dbo.MenuOpciones WHERE Ruta = N'/carrito');

    IF @RolClienteId IS NULL
        THROW 51101, N'No existe un rol Cliente activo.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.RolMenuOpciones
        WHERE RolId = @RolClienteId AND MenuOpcionId = @MenuCarritoId
    )
        INSERT dbo.RolMenuOpciones (RolId, MenuOpcionId)
        VALUES (@RolClienteId, @MenuCarritoId);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'Acceso al carrito aplicado de forma idempotente.';
GO
