/* =========================================================
   Migración de autenticación por correo electrónico.
   Actualiza dbo.Usuarios sin eliminar ni recrear tablas.
   Ejecutar sobre ProyectoEcommerceDB una sola vez; el script
   también puede volver a ejecutarse de forma segura.
   ========================================================= */

USE [ProyectoEcommerceDB];
GO

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
    THROW 50001, 'No existe dbo.Usuarios. Ejecute primero ProyectoEcommerceDB_Etapa1.sql.', 1;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'Nombre') IS NULL
   AND COL_LENGTH(N'dbo.Usuarios', N'NombreCompleto') IS NOT NULL
BEGIN
    EXEC sys.sp_rename N'dbo.Usuarios.NombreCompleto', N'Nombre', N'COLUMN';
END;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'Nombre') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios ADD Nombre NVARCHAR(120) NULL;
    UPDATE dbo.Usuarios SET Nombre = N'' WHERE Nombre IS NULL;
    ALTER TABLE dbo.Usuarios ALTER COLUMN Nombre NVARCHAR(120) NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'Apellidos') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios ADD Apellidos NVARCHAR(120) NULL;
    UPDATE dbo.Usuarios SET Apellidos = N'' WHERE Apellidos IS NULL;
    ALTER TABLE dbo.Usuarios ALTER COLUMN Apellidos NVARCHAR(120) NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'PasswordHash') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios ADD PasswordHash NVARCHAR(500) NULL;
END;
GO

UPDATE dbo.Usuarios
SET Telefono = N''
WHERE Telefono IS NULL;
GO

ALTER TABLE dbo.Usuarios
ALTER COLUMN Telefono NVARCHAR(30) NOT NULL;
GO

IF EXISTS
(
    SELECT LOWER(LTRIM(RTRIM(Correo)))
    FROM dbo.Usuarios
    GROUP BY LOWER(LTRIM(RTRIM(Correo)))
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 50002, 'Existen correos duplicados al normalizar. Corríjalos antes de ejecutar la migración.', 1;
END;
GO

UPDATE dbo.Usuarios
SET Correo = LOWER(LTRIM(RTRIM(Correo)));
GO

IF EXISTS
(
    SELECT 1
    FROM sys.key_constraints
    WHERE [name] = N'UQ_Usuarios_Correo'
      AND parent_object_id = OBJECT_ID(N'dbo.Usuarios')
)
BEGIN
    ALTER TABLE dbo.Usuarios DROP CONSTRAINT UQ_Usuarios_Correo;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_Usuarios_CorreoNormalizado'
      AND parent_object_id = OBJECT_ID(N'dbo.Usuarios')
)
BEGIN
    ALTER TABLE dbo.Usuarios DROP CONSTRAINT CK_Usuarios_CorreoNormalizado;
END;
GO

ALTER TABLE dbo.Usuarios
ALTER COLUMN Correo NVARCHAR(120) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL;
GO

ALTER TABLE dbo.Usuarios
ADD CONSTRAINT UQ_Usuarios_Correo UNIQUE (Correo);
GO

ALTER TABLE dbo.Usuarios WITH CHECK
ADD CONSTRAINT CK_Usuarios_CorreoNormalizado
    CHECK (Correo = LOWER(LTRIM(RTRIM(Correo))));
GO

PRINT N'Migración de autenticación aplicada correctamente.';
GO
