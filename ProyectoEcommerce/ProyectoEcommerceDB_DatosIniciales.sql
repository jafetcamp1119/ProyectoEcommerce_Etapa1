/* =========================================================
   Datos iniciales de la etapa: familias, categorias e impuestos.
   Script incremental, idempotente y sin IDs identity escritos.
   ========================================================= */

USE [ProyectoEcommerceDB];
GO
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.FamiliasProducto', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Categorias', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Impuestos', N'U') IS NULL
    THROW 50100, 'Faltan tablas base. Ejecute primero el script de creacion y la ampliacion importante.', 1;
GO

DECLARE @Familias TABLE (Nombre NVARCHAR(80), Descripcion NVARCHAR(250));
INSERT @Familias (Nombre, Descripcion) VALUES
(N'Alimentos y bebidas', N'Productos alimenticios, ingredientes y bebidas de consumo diario.'),
(N'Hogar y limpieza', N'Art' + NCHAR(237) + N'culos para limpieza, cocina, ba' + NCHAR(241) + N'o y organizaci' + NCHAR(243) + N'n del hogar.'),
(N'Ropa y accesorios', N'Prendas, calzado y accesorios para toda la familia.'),
(N'Electr' + NCHAR(243) + N'nica', N'Dispositivos, equipos y accesorios electr' + NCHAR(243) + N'nicos.'),
(N'Cuidado personal', N'Productos de higiene y cuidado personal diario.');

UPDATE dbo.FamiliasProducto
SET Nombre = N'Electr' + NCHAR(243) + N'nica'
WHERE Nombre LIKE N'Electr%nica';

INSERT dbo.FamiliasProducto (Nombre, Descripcion, Activo)
SELECT f.Nombre, f.Descripcion, 1
FROM @Familias f
WHERE NOT EXISTS (SELECT 1 FROM dbo.FamiliasProducto x WHERE x.Nombre = f.Nombre);

UPDATE x
SET x.Nombre = f.Nombre,
    x.Descripcion = f.Descripcion,
    x.Activo = 1
FROM dbo.FamiliasProducto x
INNER JOIN @Familias f ON f.Nombre = x.Nombre;
GO

DECLARE @Categorias TABLE
(
    Familia NVARCHAR(80),
    Nombre NVARCHAR(80),
    Descripcion NVARCHAR(250)
);

INSERT @Categorias (Familia, Nombre, Descripcion) VALUES
(N'Alimentos y bebidas', N'Frutas', N'Frutas frescas y productos relacionados.'),
(N'Alimentos y bebidas', N'Verduras', N'Verduras y hortalizas.'),
(N'Alimentos y bebidas', N'Carnes', N'Carnes y cortes para consumo.'),
(N'Alimentos y bebidas', N'L' + NCHAR(225) + N'cteos y huevos', N'Leche, derivados l' + NCHAR(225) + N'cteos y huevos.'),
(N'Alimentos y bebidas', N'Granos y pastas', N'Granos, cereales y pastas.'),
(N'Alimentos y bebidas', N'Condimentos y b' + NCHAR(225) + N'sicos de cocina', N'Condimentos e ingredientes esenciales.'),
(N'Alimentos y bebidas', N'Enlatados', N'Alimentos conservados y enlatados.'),
(N'Alimentos y bebidas', N'Panader' + NCHAR(237) + N'a', N'Panes y productos de panader' + NCHAR(237) + N'a.'),
(N'Alimentos y bebidas', N'Bebidas', N'Bebidas para consumo diario.'),
(N'Hogar y limpieza', N'Productos de limpieza', N'Productos para limpieza general.'),
(N'Hogar y limpieza', N'Lavander' + NCHAR(237) + N'a', N'Productos para lavado y cuidado de ropa.'),
(N'Hogar y limpieza', N'Cocina', N'Articulos de uso en cocina.'),
(N'Hogar y limpieza', N'Ba' + NCHAR(241) + N'o', N'Art' + NCHAR(237) + N'culos para el ba' + NCHAR(241) + N'o.'),
(N'Hogar y limpieza', N'Utensilios', N'Utensilios de uso dom' + NCHAR(233) + N'stico.'),
(N'Hogar y limpieza', N'Organizaci' + NCHAR(243) + N'n del hogar', N'Soluciones para ordenar espacios.'),
(N'Ropa y accesorios', N'Ropa para mujer', N'Prendas para mujer.'),
(N'Ropa y accesorios', N'Ropa para hombre', N'Prendas para hombre.'),
(N'Ropa y accesorios', N'Ropa infantil', N'Prendas para ni' + NCHAR(241) + N'as y ni' + NCHAR(241) + N'os.'),
(N'Ropa y accesorios', N'Calzado', N'Calzado para diferentes edades.'),
(N'Ropa y accesorios', N'Ropa interior', N'Ropa interior y prendas basicas.'),
(N'Ropa y accesorios', N'Accesorios', N'Accesorios de vestir.'),
(N'Electr' + NCHAR(243) + N'nica', N'Celulares', N'Tel' + NCHAR(233) + N'fonos celulares.'),
(N'Electr' + NCHAR(243) + N'nica', N'Computadoras', N'Computadoras y equipos relacionados.'),
(N'Electr' + NCHAR(243) + N'nica', N'Audio', N'Equipos y accesorios de audio.'),
(N'Electr' + NCHAR(243) + N'nica', N'Accesorios electr' + NCHAR(243) + N'nicos', N'Complementos para dispositivos electr' + NCHAR(243) + N'nicos.'),
(N'Electr' + NCHAR(243) + N'nica', N'Electrodom' + NCHAR(233) + N'sticos peque' + NCHAR(241) + N'os', N'Electrodom' + NCHAR(233) + N'sticos compactos para el hogar.'),
(N'Cuidado personal', N'Higiene personal', N'Articulos de higiene diaria.'),
(N'Cuidado personal', N'Cuidado del cabello', N'Productos para el cabello.'),
(N'Cuidado personal', N'Cuidado de la piel', N'Productos para el cuidado de la piel.'),
(N'Cuidado personal', N'Cuidado dental', N'Productos de higiene bucal.'),
(N'Cuidado personal', N'Higiene femenina', N'Productos de higiene femenina.');

UPDATE c SET Nombre = N'L' + NCHAR(225) + N'cteos y huevos'
FROM dbo.Categorias c INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = c.FamiliaId
WHERE f.Nombre = N'Alimentos y bebidas' AND c.Nombre LIKE N'L%cteos y huevos';
UPDATE c SET Nombre = N'Condimentos y b' + NCHAR(225) + N'sicos de cocina'
FROM dbo.Categorias c INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = c.FamiliaId
WHERE f.Nombre = N'Alimentos y bebidas' AND c.Nombre LIKE N'Condimentos y %sicos de cocina';
UPDATE c SET Nombre = N'Panader' + NCHAR(237) + N'a'
FROM dbo.Categorias c INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = c.FamiliaId
WHERE f.Nombre = N'Alimentos y bebidas' AND c.Nombre LIKE N'Panader%a';
UPDATE c SET Nombre = N'Lavander' + NCHAR(237) + N'a'
FROM dbo.Categorias c INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = c.FamiliaId
WHERE f.Nombre = N'Hogar y limpieza' AND c.Nombre LIKE N'Lavander%a';
UPDATE c SET Nombre = N'Ba' + NCHAR(241) + N'o'
FROM dbo.Categorias c INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = c.FamiliaId
WHERE f.Nombre = N'Hogar y limpieza' AND c.Nombre LIKE N'Ba%o';
UPDATE c SET Nombre = N'Organizaci' + NCHAR(243) + N'n del hogar'
FROM dbo.Categorias c INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = c.FamiliaId
WHERE f.Nombre = N'Hogar y limpieza' AND c.Nombre LIKE N'Organizaci%n del hogar';
UPDATE c SET Nombre = N'Accesorios electr' + NCHAR(243) + N'nicos'
FROM dbo.Categorias c INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = c.FamiliaId
WHERE f.Nombre = N'Electr' + NCHAR(243) + N'nica' AND c.Nombre LIKE N'Accesorios electr%nicos';
UPDATE c SET Nombre = N'Electrodom' + NCHAR(233) + N'sticos peque' + NCHAR(241) + N'os'
FROM dbo.Categorias c INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = c.FamiliaId
WHERE f.Nombre = N'Electr' + NCHAR(243) + N'nica' AND c.Nombre LIKE N'Electrodom%sticos peque%os';

INSERT dbo.Categorias (FamiliaId, Nombre, Descripcion, Activo)
SELECT f.FamiliaId, c.Nombre, c.Descripcion, 1
FROM @Categorias c
INNER JOIN dbo.FamiliasProducto f ON f.Nombre = c.Familia
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Categorias x
    WHERE x.FamiliaId = f.FamiliaId AND x.Nombre = c.Nombre
);

UPDATE x
SET x.Nombre = c.Nombre,
    x.Descripcion = c.Descripcion,
    x.Activo = 1
FROM dbo.Categorias x
INNER JOIN dbo.FamiliasProducto f ON f.FamiliaId = x.FamiliaId
INNER JOIN @Categorias c ON c.Familia = f.Nombre AND c.Nombre = x.Nombre;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Impuestos WHERE Nombre = N'IVA 13%')
    INSERT dbo.Impuestos (Nombre, Porcentaje, FechaInicio, FechaFin, Activo)
    VALUES (N'IVA 13%', 13.00, CONVERT(DATE, GETDATE()), NULL, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Impuestos WHERE Nombre = N'Exento')
    INSERT dbo.Impuestos (Nombre, Porcentaje, FechaInicio, FechaFin, Activo)
    VALUES (N'Exento', 0.00, CONVERT(DATE, GETDATE()), NULL, 1);
GO

PRINT N'Datos iniciales aplicados sin duplicados.';
GO
