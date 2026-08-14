# LessPrice — Etapa 1

El proyecto conserva la arquitectura de la solución original: Controllers, interfaces LN, implementaciones LN, `UnidadTrabajoEF`, repositorio genérico y Angular como cliente web.

## Arquitectura

```text
Angular → Controller API → Interfaz LN → Implementación LN
        → IUnidadTrabajoEF → RepositorioAD<TEntity> → SQL Server
```

No se creó una capa nueva ni se separó la cuenta en entidades `Usuario` y `Cliente`. El proyecto ya trabajaba con una sola entidad `Usuario`, por lo que esa estructura se conserva.

## Autenticación

La autenticación utiliza el correo electrónico como identificador. No existe un campo `Username` independiente.

- `POST api/Usuario/Registrar` crea una cuenta.
- `POST api/Usuario/IniciarSesion` valida correo y contraseña y devuelve un JWT.
- El resto de los endpoints requiere `Authorization: Bearer <token>`.
- Angular guarda temporalmente el JWT y los datos mínimos de sesión en `sessionStorage`, por lo que se conservan al recargar la pestaña pero no se reutilizan indefinidamente en una sesión nueva del navegador.
- Un guard protege el dashboard y un interceptor agrega el token a las solicitudes.
- El botón **Cerrar sesión** elimina la sesión y vuelve a `/auth`.

La seguridad de acceso se valida siempre en la API y queda persistida en SQL Server:

- Después de 3 credenciales incorrectas, la cuenta se bloquea durante 15 minutos y la API responde HTTP `423 Locked` con el tiempo restante.
- Los valores se configuran con `Seguridad:MaxIntentosFallidos` y `Seguridad:BloqueoMinutos`.
- Los intentos y el vencimiento se guardan en `Usuarios`; recargar Angular no evita el bloqueo.
- `HistorialAccesos` registra intentos exitosos y fallidos sin contraseñas, hashes ni tokens.
- Un acceso válido después del vencimiento limpia el contador y las fechas de bloqueo.
- Cada validación del JWT comprueba que el usuario y su rol sigan activos, que el rol no haya cambiado y que la cuenta no esté bloqueada. Por ello, desactivar, cambiar el rol o bloquear una cuenta revoca de inmediato su sesión anterior.

El correo se recorta, se convierte a minúsculas y tiene una restricción única en SQL Server. La contraseña nunca se guarda en texto plano; únicamente se persiste `PasswordHash`, generado y verificado con `PasswordHasher<Usuario>` de ASP.NET Core.

La configuración inicial consulta únicamente si existe algún usuario con el rol `Administrador`. En una instalación sin administradores, `GET /api/auth/setup-status` habilita `/configuracion-inicial` y `POST /api/auth/setup-admin` permite registrar el primer Administrador con cualquier correo válido. La API repite la comprobación dentro de una transacción `Serializable`; cuando ya existe un Administrador, rechaza el setup aunque se invoque manualmente. No existe un correo inicial reservado en `appsettings`. El registro público continúa asignando siempre el rol `Cliente`.

El componente `/auth` alterna dentro de la misma tarjeta entre:

- Inicio de sesión: correo electrónico y contraseña.
- Creación de cuenta: nombre, apellidos, correo electrónico, teléfono, contraseña y confirmación.

Después de un registro correcto se muestra **“La cuenta fue creada correctamente”**, se vuelve al modo de inicio de sesión y se conserva el correo registrado.

## Roles y administración de usuarios

Los únicos roles operativos son `Administrador` y `Cliente`. El registro público no recibe ni confía en un rol enviado por el cliente: la lógica de negocio asigna siempre `Cliente`.

- El menú se obtiene de las tablas existentes `MenuOpciones` y `RolMenuOpciones`.
- Un Cliente ve Inicio, Productos, Carrito y Órdenes; no puede abrir ni consumir endpoints administrativos.
- Un Administrador ve además Familias, Categorías, Impuestos y Roles.
- `/roles` lista nombre, correo, teléfono, rol, estado y fecha de registro, con búsqueda, filtros y paginación de 25/50/75/100 filas.
- `PUT api/Usuario/CambiarRol` y `PUT api/Usuario/CambiarEstado` requieren el rol `Administrador` y escriben en `BitacoraSistema`.
- La API impide desactivar o degradar al último Administrador activo.
- Las respuestas administrativas no incluyen `PasswordHash`, contraseñas ni tokens.

## Módulo de productos

El módulo usa la entidad y la tabla `Productos` existentes. La familia se obtiene mediante `Producto → Categoria → FamiliaProducto`; no se agregó una columna `FamiliaId` a `Productos` ni se modificó el esquema SQL.

Rutas Angular:

- `#/productos`: para Cliente muestra primero las familias activas; para Administrador conserva el catálogo administrativo.
- `#/productos/buscar`: búsqueda global de productos por texto.
- `#/productos/familia/:familiaId`: categorías activas de la familia elegida.
- `#/productos/familia/:familiaId/buscar`: búsqueda de productos limitada a la familia elegida.
- `#/productos/familia/:familiaId/categoria/:categoriaId`: productos de la categoría elegida.
- `#/productos/:productoId`: detalle navegable y recargable directamente.

Endpoints principales:

- `GET api/FamiliaProducto/Cliente`: familias activas ordenadas para el inicio del catálogo.
- `GET api/Producto/Catalogo`: productos activos de una familia obligatoria, con contrato seguro para Cliente.
- `GET api/Producto/Administracion`: productos activos e inactivos con datos completos; requiere Administrador.
- `GET api/Producto/Detalle/{id}` y `GET api/Producto/DetalleAdministracion/{id}`.
- `GET api/Producto/Catalogos`: familias, categorías e impuestos activos.
- `PUT api/Producto/Modificar` y `PUT api/Producto/CambiarEstado/{id}`; requieren Administrador.
- `GET api/ProductoImagen/ListarPorProducto/{id}` y `GET api/ProductoImagen/Principal/{id}`.

El listado se pagina en SQL Server y admite 25, 50, 75 o 100 elementos. Los filtros disponibles son texto por nombre/código, familia, categoría, precio mínimo/máximo, disponibilidad, orden y, únicamente para administración, estado activo/inactivo. El estado de inventario se deriva sin guardarse en otra columna: `Disponible` cuando `Stock > StockMinimo`, `Stock bajo` cuando `Stock > 0` y `Stock <= StockMinimo`, y `Agotado` cuando `Stock = 0`.

El contrato de Cliente expone el código comercial, pero no costo, stock exacto, stock mínimo, estado administrativo ni fecha de creación. La API también impide consultar productos, categorías o familias inactivos desde los endpoints de Cliente. La administración permite editar, activar y desactivar; los productos nuevos se originan en el catálogo de un proveedor y no existe creación arbitraria sin proveedor.

### Incorporar imágenes reales posteriormente

1. Crear primero el producto desde la administración y conservar su `ProductoId`.
2. Publicar el archivo real en el almacenamiento aprobado por el equipo; no registrar direcciones de prueba, externas o inventadas.
3. Registrar en `ProductoImagenes` el `ProductoId`, la ubicación real, un texto alternativo descriptivo, `Orden`, `EsPrincipal` y `Activo`.
4. Mantener solamente una imagen principal activa por producto. Las demás pueden registrarse como secundarias con órdenes consecutivos; no existe un máximo funcional de imágenes.
5. Comprobar el catálogo y `#/productos/:productoId`. Si no existen imágenes activas, la interfaz mostrará las iniciales del producto; una imagen principal activa reemplaza automáticamente ese marcador sin cambiar el componente.

La implementación entregada no inserta archivos de imagen, direcciones URL, Base64 ni registros ficticios en `ProductoImagenes`.

## Carrito de compras

El carrito conserva el flujo del proyecto: `CarritoController → ICarritoLN → CarritoLN → IUnidadTrabajoEF → RepositorioAD → SQL Server`. No se creó una capa ni un patrón adicional.

- `POST api/Carrito/Agregar` recibe solamente `ProductoId` y `Cantidad`.
- `GET api/Carrito/Actual` y su alias `GET api/Carrito/Resumen` devuelven el carrito abierto del Cliente autenticado.
- `PUT api/Carrito/Cantidad` cambia directamente la cantidad de una línea y vuelve a validar el stock.
- `DELETE api/Carrito/Detalle/{carritoDetalleId}` elimina solamente una línea perteneciente al carrito del Cliente autenticado.
- Todos los endpoints requieren JWT y el rol exacto `Cliente`; el `UsuarioId` se obtiene exclusivamente del claim `NameIdentifier`.
- La API busca o crea un único carrito con estado `ACTIVO`, que es el equivalente abierto existente en este esquema.
- Agregar de nuevo el mismo producto incrementa su cantidad en la fila existente. La restricción única `(CarritoId, ProductoId)` impide duplicados.
- La cantidad acumulada nunca puede superar el stock actual. El precio final con impuesto incluido se obtiene de `Productos.PrecioVenta`; no se confía en usuario, precio ni stock enviados por Angular.
- Agregar o editar el carrito no descuenta inventario. El resumen se recalcula en servidor, separando del precio final la base antes de impuestos y el impuesto incluido sin volver a sumarlo al total.
- Angular ofrece la acción en tarjetas y detalle únicamente al Cliente, deshabilita productos agotados, mantiene los filtros y actualiza el indicador del encabezado sin recargar el catálogo.

Las tarjetas de familias y productos comparten las clases globales `catalog-tile`, `catalog-tile-visual` y `catalog-tile-content`. El marcador por iniciales no descarga recursos ni persiste datos de imagen.

## Checkout, venta y facturación

El flujo de Cliente queda completo: productos → familias → categorías → productos → detalle → carrito → checkout → orden → factura PDF.

- `GET api/Orden/Checkout` prepara datos de Cliente y vuelve a calcular el carrito en servidor.
- `POST api/Orden/ConfirmarCompra` exige correo confirmado, dirección y método `TARJETA` o `EFECTIVO`; el sistema nunca solicita ni almacena datos bancarios.
- La confirmación usa una transacción SQL `Serializable`. Precios, impuestos, descuentos y stock se vuelven a leer de SQL Server; Angular no decide importes históricos.
- La orden se crea como `VENTA/PENDIENTE`, guarda el detalle histórico, registra el método de pago y llama a `sp_ConfirmarOrdenVenta` dentro de la misma transacción.
- `sp_ConfirmarOrdenVenta` bloquea los productos involucrados, rechaza sobreventa, descuenta stock una sola vez, registra `MovimientosInventario` y deja la orden `CONFIRMADA`.
- El carrito queda `CONVERTIDO` solamente si la transacción se confirma. Cualquier error previo revierte orden, detalle, pago, movimientos, stock y carrito.
- Después del commit se genera `wwwroot/documentos/facturas/Factura-{OrdenId:D6}.pdf`, se registra `Documentos` y la orden pasa a `FACTURADA`. Un fallo posterior de PDF o correo no revierte la venta confirmada.

La factura se genera con QuestPDF y contiene número, fecha, Cliente, correo usado, dirección, productos, cantidades, precios históricos, impuestos, descuentos, total, método de pago y numeración de páginas. Se registra como `FAC-{OrdenId:D6}` y se descarga únicamente mediante `GET api/Orden/Factura/{ordenId}`, que vuelve a comprobar propiedad de la orden o rol Administrador; la ruta pública del archivo no se expone en Angular.

El envío usa MailKit con Gmail SMTP. Los valores no sensibles incluidos son `Host=smtp.gmail.com`, `Port=587`, `Username=facturacionlessprices67@gmail.com`, `FromEmail=facturacionlessprices67@gmail.com`, `FromName=LessPrice` y `EnableSsl=true`. En el puerto 587, `EnableSsl` se aplica mediante STARTTLS. `Smtp:Enabled` permanece en `false` de forma predeterminada y `Smtp:Password` queda vacío: la App Password debe configurarse únicamente mediante User Secrets, una variable de entorno o el proveedor seguro del despliegue.

Para desarrollo local, desde la carpeta de la solución:

```powershell
dotnet user-secrets set "Smtp:Password" "TU_APP_PASSWORD_DE_GMAIL" --project API/ProyectoEcommerce.API.csproj
dotnet user-secrets set "Smtp:Enabled" "true" --project API/ProyectoEcommerce.API.csproj
```

## Órdenes

- `GET api/Orden/MisOrdenes` lista únicamente las ventas del Cliente autenticado.
- `GET api/Orden/Administracion` permite al Administrador filtrar todas las ventas por número, Cliente, estado y rango de fechas.
- `GET api/Orden/Detalle/{ordenId}` y `GET api/Orden/Factura/{ordenId}` aplican control de propiedad en la API, no solamente en Angular.
- `PUT api/Orden/Cancelar/{ordenId}` permite cancelar únicamente una venta propia que todavía esté `PENDIENTE`; no restaura inventario porque una orden pendiente aún no lo descontó.
- Las listas usan tarjetas, filtros y paginación de 25, 50, 75 o 100 registros; el detalle muestra importes históricos y el estado de envío de la factura.

## Proveedores y compras

El módulo mantiene tres conceptos distintos:

- `ProductosProveedorCatalogo` contiene ofertas que un proveedor puede suministrar. Cada oferta guarda categoría, precio de compra, impuesto y estado, y puede existir sin `ProductoId`.
- `Productos` contiene únicamente los artículos incorporados al catálogo vendible de LessPrice.
- `Productos.Stock` representa unidades compradas y solo cambia al confirmar una compra o una venta.

`ProveedorCategorias` relaciona categorías globales con cada proveedor. Desde `#/proveedores/catalogo/:proveedorId` se puede escoger una categoría existente sin duplicarla o crear una categoría global nueva y relacionarla dentro de la misma transacción. Si el nombre ya existe en esa familia, el backend reutiliza la categoría encontrada. La imagen opcional se carga después mediante el mismo endpoint y la misma carpeta de `CategoriaController`.

Dentro de una categoría del proveedor se puede:

- Relacionar un producto que ya existe en LessPrice, guardando solamente la oferta y `ProductoProveedor`.
- Crear una oferta nueva con nombre, precio de compra, `ImpuestoId` y estado. Esta acción no inserta en `Productos` ni modifica inventario.
- Incorporar una oferta disponible. `sp_IncorporarProductoProveedor` vuelve a leer precio, impuesto, proveedor y categoría; genera un código `PROD-000001`, calcula `PrecioVenta = PrecioCompra * 1.30`, crea el producto con stock 0 y enlaza `ProductoProveedor`.
- Editar precio, impuesto y estado de la oferta sin alterar los precios históricos ya guardados en compras anteriores.

La compra usa un carrito Angular independiente del carrito del Cliente. Proforma y correo recalculan los precios desde SQL Server y no cambian stock. `sp_ConfirmarCompraProveedor` valida proveedor activo, productos incorporados, relaciones activas y cantidades; toma nuevamente los precios, guarda cabecera y detalle histórico, aumenta stock, registra movimientos `ENTRADA` y bitácora dentro de una transacción `Serializable`. `ClaveConfirmacion` tiene índice único y hace idempotente un reintento, por lo que una misma confirmación no suma inventario dos veces.

Después del commit se reutilizan la generación PDF y la configuración SMTP existentes. El historial permite filtrar, paginar 25/50/75/100, ver detalle y obtener el PDF. Desactivar un proveedor bloquea proformas y compras nuevas, pero no elimina productos, stock, movimientos ni compras históricas; los Clientes pueden seguir adquiriendo las unidades disponibles.

Rutas administrativas principales:

- `#/proveedores`: menú de gestión, compras e historial.
- `#/proveedores/gestion`: mantenimiento e imagen de proveedores.
- `#/proveedores/catalogo/:proveedorId`: categorías y ofertas.
- `#/proveedores/compras/:proveedorId`: carrito, proforma y confirmación.
- `#/proveedores/historial`: historial y PDF.

## Base de datos

Para una instalación nueva, ejecutar en este orden:

```text
ProyectoEcommerceDB_Etapa1.sql
ProyectoEcommerceDB_AmpliacionImportante.sql
ProyectoEcommerceDB_DatosIniciales.sql
ProyectoEcommerceDB_Proveedores.sql
ProyectoEcommerceDB_Descuentos.sql
ProyectoEcommerceDB_Carrito.sql
ProyectoEcommerceDB_FlujoCliente.sql
```

Para una base creada con una versión anterior, ejecutar primero la migración de autenticación si todavía conserva la columna `Password` y luego la ampliación y los datos iniciales:

```text
ProyectoEcommerceDB_Autenticacion.sql
ProyectoEcommerceDB_AmpliacionImportante.sql
ProyectoEcommerceDB_DatosIniciales.sql
ProyectoEcommerceDB_Proveedores.sql
ProyectoEcommerceDB_Descuentos.sql
ProyectoEcommerceDB_Carrito.sql
ProyectoEcommerceDB_FlujoCliente.sql
```

Los archivos SQL están guardados en UTF-8. Si se ejecutan con `sqlcmd`, se debe indicar explícitamente esa codificación para conservar tildes y eñes, por ejemplo: `sqlcmd -S ".\SQLEXPRESS" -E -C -f 65001 -b -i "ProyectoEcommerceDB_Proveedores.sql"`. SQL Server Management Studio detecta el archivo UTF-8 directamente.

Los scripts son incrementales, idempotentes y no eliminan tablas ni datos. `ProyectoEcommerceDB_Etapa1.sql` se detiene si detecta una instalación existente para impedir una recreación destructiva. La ampliación conserva la estructura Database First e incorpora solo soporte útil para la evolución del mismo e-commerce: roles, historial de accesos, opciones de menú, imágenes de producto ordenadas, proveedores/compras, descuentos, carrito, pagos/documentos preparados, calificaciones, lista de deseos, movimientos de inventario y bitácora.

La tabla `ProductoImagenes` admite una imagen principal activa por producto, varias secundarias ordenadas, texto alternativo y evita repetir una misma ruta para el producto. Angular incluye el componente reutilizable `app-producto-carrusel`, que presenta tres imágenes a la vez, avanza sobre cinco o más registros y muestra un estado vacío sin inventar imágenes ni direcciones URL.

Los datos iniciales crean o normalizan exactamente 5 familias, 32 categorías vinculadas y los impuestos `IVA 13%`, `IVA 1%` y `Exento`. `ProyectoEcommerceDB_Proveedores.sql` agrega cinco proveedores reproducibles, entre ellos Dos Pinos, y ofertas costarricenses por categoría. En una base limpia deja incorporados solamente `Frescoleche Chocolate 250 ml` y `Leche Entera 1 L`; las demás ofertas de Dos Pinos permanecen disponibles para demostrar la incorporación. Los scripts se pueden ejecutar repetidamente sin duplicar relaciones ni catálogos.

`ProyectoEcommerceDB_Carrito.sql` agrega de forma idempotente la opción de menú `/carrito` al rol Cliente y no modifica permisos administrativos. `ProyectoEcommerceDB_FlujoCliente.sql` amplía de forma incremental `Ordenes`, `OrdenDetalle` y `Documentos`, normaliza las restricciones de estados/tipos y crea o actualiza `sp_ConfirmarOrdenVenta`; puede ejecutarse repetidamente. `ProyectoEcommerceDB_LimpiezaDatosCodex.sql` es una utilidad transaccional e idempotente de QA: elimina únicamente las cuentas y códigos temporales exactos documentados dentro del script, junto con sus dependencias, y se detiene si detecta relaciones no previstas. No debe usarse para eliminar datos reales.

Política de pruebas del proyecto: cualquier cuenta, producto, carrito, bitácora o relación temporal creada para una validación debe eliminarse al terminar la misma y comprobarse después por SQL. No se deben dejar registros con nombres, correos o códigos de QA en la base compartida.

La conexión incluida apunta a SQL Server Express:

```text
Server=.\SQLEXPRESS;Database=ProyectoEcommerceDB;Trusted_Connection=True;TrustServerCertificate=True;
```

Si se usa otra instancia, cambiar solamente el valor de `Server` en `API/appsettings.json`.

## Configuración JWT

Los valores de desarrollo están en `API/appsettings.json`, sección `Jwt`. En producción se debe proporcionar una clave secreta propia de al menos 32 bytes mediante configuración segura y no incluirla en el repositorio.

Claves requeridas:

```text
Jwt:Key
Jwt:Issuer
Jwt:Audience
Jwt:ExpiresMinutes
```

Control de intentos:

```text
Seguridad:MaxIntentosFallidos
Seguridad:BloqueoMinutos
```

AutoMapper se actualizó a `15.1.1` para corregir la vulnerabilidad de denegación de servicio detectada en la versión anterior. El proveedor permite ejecutarlo sin clave durante desarrollo y pruebas; para un despliegue de producción se debe obtener la licencia correspondiente y definir `AUTOMAPPER_LICENSE_KEY` en el entorno.

## Ejecutar la solución

Desde Visual Studio:

1. Abrir `ProyectoEcommerce.slnx`.
2. Seleccionar el perfil de inicio múltiple **API y Angular**.
3. Iniciar la solución.

El perfil levanta la API y el cliente. `npm start` usa `ng serve` sin `--open`, por lo que Angular se abre una sola vez mediante el proyecto JavaScript de Visual Studio. La API usa `launchBrowser: true` con `launchUrl: swagger` y abre una única pestaña adicional de Swagger. El resultado esperado son exactamente dos pestañas: Angular y Swagger.

Direcciones de desarrollo:

- Web Angular: `http://localhost:4200`.
- Autenticación: `http://localhost:4200/#/auth`.
- Swagger: `https://localhost:7129/swagger`.
- API: `https://localhost:7129/api`.

Durante `ng serve`, Angular consume `/api` mediante `proxy.conf.json`, que reenvía las solicitudes a `https://localhost:7129` con validación de certificado desactivada únicamente en desarrollo. El perfil múltiple de Visual Studio inicia la API solo en HTTPS, evitando que una instancia previa en `5010` provoque el error `address already in use`. El perfil HTTP separado continúa disponible en `http://localhost:5010` cuando se selecciona o ejecuta explícitamente.

Desde terminal:

```powershell
dotnet restore ProyectoEcommerce.slnx
dotnet run --project API/ProyectoEcommerce.API.csproj
```

En otra terminal:

```powershell
cd ProyectoEcommerce
npm install
npm start
```

## Verificación

Comandos principales:

```powershell
dotnet build ProyectoEcommerce.slnx --no-restore
cd ProyectoEcommerce
npm run build
```

También se debe verificar el flujo en el navegador: acceso directo al dashboard sin sesión, validaciones del registro, alta correcta como Cliente, correo duplicado, tres intentos incorrectos, pantalla de acceso bloqueado persistente al recargar, desbloqueo al vencer, inicio válido, menús por rol, guarda de `/roles`, revocación al cambiar rol/estado, familias activas, catálogo limitado por familia, detalle, agregado al carrito, cambio y eliminación de cantidades, límite de stock, checkout de tres pasos, correo confirmado, venta transaccional, carrito convertido, `Mis órdenes`, detalle protegido y descarga de la factura.

Para comprobar la transacción sin dejar datos, se recomienda ejecutar una orden temporal dentro de una transacción exterior, llamar `sp_ConfirmarOrdenVenta`, verificar dentro de la transacción la reducción de stock y el movimiento, ejecutar `ROLLBACK` y comprobar que stock, orden y movimiento recuperaron sus valores iniciales.

## Alcance funcional existente

Hay Controllers y lógica de negocio para familias de producto, categorías, impuestos, productos, imágenes de producto, usuarios y órdenes. Familias, categorías e impuestos validan duplicados y datos obligatorios; su acción de eliminar realiza desactivación lógica. La ruta `/categorias/:familiaId` filtra realmente por la familia seleccionada y el inicio calcula sus indicadores con datos de la API.

El carrito, checkout, venta, método de pago, factura PDF, envío SMTP configurable y consulta de órdenes están activos en Angular y API para el rol Cliente. La gestión de proveedores, el catálogo previo, las compras, las entradas de inventario y su historial están activos para Administrador. Las calificaciones continúan reservadas para una etapa posterior. `OrdenDetalle` conserva su entidad y mapeo Database First; los flujos mantienen la arquitectura existente sin repositorios paralelos ni capas nuevas.
