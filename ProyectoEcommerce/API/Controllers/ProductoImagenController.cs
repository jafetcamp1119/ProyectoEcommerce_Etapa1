using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;

namespace ProyectoEcommerce.API.Controllers;

// Controlador encargado de consultar y administrar
// las imágenes de los productos.
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ProductoImagenController : ControllerBase
{
    // Permite utilizar la lógica de negocio de imágenes.
    private readonly IProductoImagenLN _productoImagenLN;

    // Permite conocer la ubicación física del proyecto
    // para poder guardar los archivos en wwwroot.
    private readonly IWebHostEnvironment _environment;

    // Constructor del controlador.
    public ProductoImagenController(
        IProductoImagenLN productoImagenLN,
        IWebHostEnvironment environment)
    {
        // Guarda la lógica de negocio recibida.
        _productoImagenLN = productoImagenLN;

        // Guarda la información del entorno del proyecto.
        _environment = environment;
    }

    // Lista todas las imágenes asociadas a un producto.
    [HttpGet("ListarPorProducto/{productoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ListarPorProducto(int productoId)
    {
        // Verifica que el ID del producto sea válido.
        if (productoId <= 0)
            return NotFound();

        // Busca las imágenes del producto.
        // El administrador también puede consultar productos inactivos.
        var resultado =
            await _productoImagenLN.ListarPorProductoAsync(
                productoId,
                User.IsInRole("Administrador"));

        // Si el producto no existe, devuelve 404.
        if (resultado.Error == Mensajes.ProductoNoEncontrado)
            return NotFound(resultado);

        // Si ocurrió otro error, devuelve una respuesta incorrecta.
        if (!string.IsNullOrEmpty(resultado.Error))
            return BadRequest(resultado);

        // Devuelve las imágenes encontradas.
        return Ok(resultado);
    }

    // Obtiene la imagen principal que se usa
    // en el catálogo y en el detalle del producto.
    [HttpGet("ObtenerPrincipal/{productoId:int}")]
    [HttpGet("Principal/{productoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ObtenerPrincipal(int productoId)
    {
        // Verifica que el producto sea válido.
        if (productoId <= 0)
            return NotFound();

        // Busca la imagen principal.
        var resultado =
            await _productoImagenLN.ObtenerPrincipalAsync(
                productoId,
                User.IsInRole("Administrador"));

        // Si el producto no existe, devuelve 404.
        if (resultado.Error == Mensajes.ProductoNoEncontrado)
            return NotFound(resultado);

        // Si ocurrió algún otro problema, devuelve el error.
        if (!string.IsNullOrEmpty(resultado.Error))
            return BadRequest(resultado);

        // Devuelve la imagen principal encontrada.
        return Ok(resultado);
    }

    // Permite que solamente el administrador
    // pueda subir imágenes desde su computadora.
    [Authorize(Roles = "Administrador")]
    [HttpPost("Subir/{productoId:int}")]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> Subir(
        int productoId,
        [FromForm] List<IFormFile> archivos)
    {
        // Verifica que el ID del producto sea correcto.
        if (productoId <= 0)
        {
            return BadRequest(
                "Producto inválido.");
        }

        // Verifica que el administrador haya seleccionado archivos.
        if (archivos == null || archivos.Count == 0)
        {
            return BadRequest(
                "Debe seleccionar al menos una imagen.");
        }

        // Solo se permite seleccionar un máximo
        // de 3 imágenes por carga.
        if (archivos.Count > 3)
        {
            return BadRequest(
                "Solo puede seleccionar un máximo de 3 imágenes.");
        }

        // Consulta las imágenes que ya tiene el producto.
        var imagenesActuales =
            await _productoImagenLN.ListarPorProductoAsync(
                productoId,
                true);

        // Verifica si ocurrió un error al consultar
        // las imágenes actuales.
        if (!string.IsNullOrEmpty(imagenesActuales.Error))
        {
            return BadRequest(imagenesActuales);
        }

        // Cuenta cuántas imágenes tiene actualmente el producto.
        var cantidadActual =
            imagenesActuales.Data?.Count() ?? 0;

        // Calcula cuántas imágenes tendría
        // después de agregar las nuevas.
        var cantidadTotal =
            cantidadActual + archivos.Count;

        // Un producto puede tener como máximo 3 imágenes.
        if (cantidadTotal > 3)
        {
            return BadRequest(
                $"El producto puede tener como máximo 3 imágenes. " +
                $"Actualmente tiene {cantidadActual}.");
        }

        // Extensiones permitidas para las imágenes.
        var extensionesPermitidas = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        // Guarda los resultados de las imágenes registradas.
        var resultados = new List<object>();

        // Obtiene la ubicación de la carpeta wwwroot.
        var webRoot = _environment.WebRootPath;

        // Si wwwroot todavía no existe,
        // se crea su ubicación manualmente.
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot");
        }

        // Crea una carpeta específica para el producto.
        // Ejemplo: wwwroot/productos/15
        var carpetaProducto = Path.Combine(
            webRoot,
            "productos",
            productoId.ToString());

        // Si la carpeta no existe,
        // el sistema la crea automáticamente.
        Directory.CreateDirectory(carpetaProducto);

        // Recorre cada imagen seleccionada por el administrador.
        foreach (var archivo in archivos)
        {
            // Ignora archivos vacíos.
            if (archivo.Length == 0)
                continue;

            // Cada imagen puede pesar como máximo 5 MB.
            if (archivo.Length > 5 * 1024 * 1024)
            {
                return BadRequest(
                    $"La imagen {archivo.FileName} supera los 5 MB.");
            }

            // Obtiene la extensión del archivo.
            var extension =
                Path.GetExtension(archivo.FileName)
                    .ToLowerInvariant();

            // Verifica que la extensión sea permitida.
            if (!extensionesPermitidas.Contains(extension))
            {
                return BadRequest(
                    $"El archivo {archivo.FileName} " +
                    $"no es una imagen permitida.");
            }

            // Crea un nombre único para evitar
            // que dos archivos tengan el mismo nombre.
            var nombreArchivo =
                $"{Guid.NewGuid():N}{extension}";

            // Crea la dirección física donde se guardará la imagen.
            var rutaFisica =
                Path.Combine(
                    carpetaProducto,
                    nombreArchivo);

            // Guarda físicamente la imagen en el servidor.
            await using (var stream =
                new FileStream(
                    rutaFisica,
                    FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Crea la dirección que Angular utilizará
            // para mostrar la imagen.
            var urlImagen =
                $"{Request.Scheme}://{Request.Host}" +
                $"/productos/{productoId}/{nombreArchivo}";

            // Registra la información de la imagen
            // en la base de datos.
            var resultado =
                await _productoImagenLN.AgregarAsync(
                    productoId,
                    urlImagen,
                    Path.GetFileNameWithoutExtension(
                        archivo.FileName),
                    false);

            // Si ocurre un error al guardar en la base de datos,
            // también se elimina el archivo físico.
            if (!string.IsNullOrEmpty(resultado.Error))
            {
                if (System.IO.File.Exists(rutaFisica))
                {
                    System.IO.File.Delete(rutaFisica);
                }

                return BadRequest(resultado);
            }

            // Guarda la imagen registrada dentro del resultado.
            resultados.Add(resultado.Data!);
        }

        // Devuelve las imágenes que se subieron correctamente.
        return Ok(new
        {
            success = true,
            data = resultados
        });
    }

    // Permite que el administrador cambie
    // cuál imagen será la principal.
    [Authorize(Roles = "Administrador")]
    [HttpPut("Principal/{imagenId:int}")]
    public async Task<IActionResult> EstablecerPrincipal(
        int imagenId)
    {
        // Envía la solicitud a la lógica de negocio.
        var resultado =
            await _productoImagenLN
                .EstablecerPrincipalAsync(imagenId);

        // Si ocurre algún error, lo devuelve.
        if (!string.IsNullOrEmpty(resultado.Error))
            return BadRequest(resultado);

        // Devuelve el resultado correcto.
        return Ok(resultado);
    }

    // Permite que solamente el administrador
    // pueda eliminar una imagen.
    [Authorize(Roles = "Administrador")]
    [HttpDelete("Eliminar/{imagenId:int}")]
    public async Task<IActionResult> Eliminar(
        int imagenId)
    {
        // Elimina primero el registro
        // de la imagen en la base de datos.
        var resultado =
            await _productoImagenLN
                .EliminarAsync(imagenId);

        // Si ocurrió un error, se devuelve.
        if (!string.IsNullOrEmpty(resultado.Error))
            return BadRequest(resultado);

        // Obtiene los datos de la imagen eliminada.
        var imagen = resultado.Data;

        // Si existe información de la imagen,
        // también se intenta eliminar el archivo físico.
        if (imagen != null &&
            !string.IsNullOrWhiteSpace(imagen.UrlImagen))
        {
            try
            {
                // Obtiene solamente el nombre del archivo.
                var nombreArchivo =
                    Path.GetFileName(
                        new Uri(imagen.UrlImagen)
                            .LocalPath);

                // Obtiene nuevamente la ubicación de wwwroot.
                var webRoot = _environment.WebRootPath;

                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(
                        _environment.ContentRootPath,
                        "wwwroot");
                }

                // Construye la ubicación física de la imagen.
                var rutaFisica =
                    Path.Combine(
                        webRoot,
                        "productos",
                        imagen.ProductoId.ToString(),
                        nombreArchivo);

                // Si el archivo existe, lo elimina.
                if (System.IO.File.Exists(rutaFisica))
                {
                    System.IO.File.Delete(rutaFisica);
                }
            }
            catch
            {
                // Si el archivo físico ya no existe,
                // la eliminación de la BD sigue siendo válida.
            }
        }

        // Devuelve el resultado de la eliminación.
        return Ok(resultado);
    }
}