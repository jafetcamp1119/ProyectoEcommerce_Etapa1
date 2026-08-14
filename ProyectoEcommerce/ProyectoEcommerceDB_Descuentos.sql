USE [ProyectoEcommerceDB];
GO

SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- toda la migracion queda en una transaccion para no dejar el esquema a medias
BEGIN TRANSACTION;

-- actualiza Descuentos sin recrear la tabla ni perder datos
-- acepta el esquema viejo con EsPromocional y tambien una ejecucion repetida
IF COL_LENGTH(N'dbo.Descuentos', N'TipoDescuento') IS NULL
BEGIN
    ALTER TABLE dbo.Descuentos ADD TipoDescuento NVARCHAR(20) NULL;
END;

IF COL_LENGTH(N'dbo.Descuentos', N'MontoFijo') IS NULL
BEGIN
    ALTER TABLE dbo.Descuentos ADD MontoFijo DECIMAL(18,2) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Descuentos', N'EsPromocional') IS NOT NULL
BEGIN
    -- SQL dinamico evita compilar una referencia a EsPromocional despues de haberla quitado
    EXEC sys.sp_executesql N'
        UPDATE dbo.Descuentos
        SET TipoDescuento = CASE
            WHEN EsPromocional = 1 THEN N''PROMOCIONAL''
            WHEN ProductoId IS NOT NULL THEN N''PRODUCTO''
            WHEN CategoriaId IS NOT NULL THEN N''CATEGORIA''
            WHEN FamiliaId IS NOT NULL THEN N''FAMILIA''
            ELSE TipoDescuento
        END
        WHERE TipoDescuento IS NULL;';
END;
ELSE
BEGIN
    UPDATE dbo.Descuentos
    SET TipoDescuento = CASE
        WHEN ProductoId IS NOT NULL THEN N'PRODUCTO'
        WHEN CategoriaId IS NOT NULL THEN N'CATEGORIA'
        WHEN FamiliaId IS NOT NULL THEN N'FAMILIA'
        ELSE TipoDescuento
    END
    WHERE TipoDescuento IS NULL;
END;

-- se detiene si algun registro viejo no se pudo acomodar en uno de los cuatro tipos
IF EXISTS (SELECT 1 FROM dbo.Descuentos WHERE TipoDescuento IS NULL)
    THROW 51010, N'No fue posible determinar el tipo de uno o más descuentos existentes.', 1;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Descuentos') AND name = N'CK_Descuentos_Ambito')
    ALTER TABLE dbo.Descuentos DROP CONSTRAINT CK_Descuentos_Ambito;

IF COL_LENGTH(N'dbo.Descuentos', N'EsPromocional') IS NOT NULL
BEGIN
    DECLARE @restriccionEsPromocional sysname;
    -- busca el nombre real de la restriccion DEFAULT para poder quitarla antes que la columna
    SELECT @restriccionEsPromocional = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID(N'dbo.Descuentos') AND c.name = N'EsPromocional';

    IF @restriccionEsPromocional IS NOT NULL
    BEGIN
        DECLARE @sqlEliminarRestriccion NVARCHAR(500) =
            N'ALTER TABLE dbo.Descuentos DROP CONSTRAINT ' + QUOTENAME(@restriccionEsPromocional) + N';';
        EXEC sys.sp_executesql @sqlEliminarRestriccion;
    END;

    ALTER TABLE dbo.Descuentos DROP COLUMN EsPromocional;
END;

ALTER TABLE dbo.Descuentos ALTER COLUMN TipoDescuento NVARCHAR(20) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Descuentos') AND name = N'CK_Descuentos_Tipo')
BEGIN
    ALTER TABLE dbo.Descuentos WITH CHECK ADD CONSTRAINT CK_Descuentos_Tipo
        CHECK (TipoDescuento IN (N'PRODUCTO', N'CATEGORIA', N'FAMILIA', N'PROMOCIONAL'));
END;

-- esta regla obliga a que cada descuento tenga exactamente un destino segun su tipo
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Descuentos') AND name = N'CK_Descuentos_Destino')
BEGIN
    ALTER TABLE dbo.Descuentos WITH CHECK ADD CONSTRAINT CK_Descuentos_Destino CHECK
    (
        (TipoDescuento = N'FAMILIA' AND FamiliaId IS NOT NULL AND CategoriaId IS NULL AND ProductoId IS NULL) OR
        (TipoDescuento = N'CATEGORIA' AND FamiliaId IS NULL AND CategoriaId IS NOT NULL AND ProductoId IS NULL) OR
        (TipoDescuento IN (N'PRODUCTO', N'PROMOCIONAL') AND FamiliaId IS NULL AND CategoriaId IS NULL AND ProductoId IS NOT NULL)
    );
END;

IF COL_LENGTH(N'dbo.Ordenes', N'DescuentoTotal') IS NULL
BEGIN
    ALTER TABLE dbo.Ordenes ADD DescuentoTotal DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Ordenes_DescuentoTotal DEFAULT (0) WITH VALUES;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Ordenes') AND name = N'CK_Ordenes_DescuentoTotal')
BEGIN
    ALTER TABLE dbo.Ordenes WITH CHECK ADD CONSTRAINT CK_Ordenes_DescuentoTotal
        CHECK (DescuentoTotal >= 0);
END;

-- agrega la opcion de descuentos al menu y la asigna solamente al Administrador
IF NOT EXISTS (SELECT 1 FROM dbo.MenuOpciones WHERE Ruta = N'/descuentos')
BEGIN
    INSERT INTO dbo.MenuOpciones (Nombre, Ruta, Icono, Orden, Activo)
    VALUES (N'Descuentos', N'/descuentos', N'cil-tags', 45, 1);
END;

DECLARE @menuDescuentosId INT = (SELECT MenuOpcionId FROM dbo.MenuOpciones WHERE Ruta = N'/descuentos');
DECLARE @rolAdministradorId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Administrador');

IF @rolAdministradorId IS NOT NULL AND NOT EXISTS
(
    SELECT 1 FROM dbo.RolMenuOpciones
    WHERE RolId = @rolAdministradorId AND MenuOpcionId = @menuDescuentosId
)
BEGIN
    INSERT INTO dbo.RolMenuOpciones (RolId, MenuOpcionId)
    VALUES (@rolAdministradorId, @menuDescuentosId);
END;

-- si llego hasta aqui deja definitivos esquema, datos y permiso de menu
COMMIT TRANSACTION;
GO
