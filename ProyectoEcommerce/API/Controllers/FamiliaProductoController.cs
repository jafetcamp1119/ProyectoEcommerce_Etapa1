using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers
{
    /// <summary>
    /// Gestiona las familias que agrupan el primer nivel del catálogo de productos.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FamiliaProductoController : ControllerBase
    {
        private IFamiliaProductoLN _familiaProductoLN { get; }

        // Permite conocer la ubicación del proyecto
        // para guardar las imágenes en wwwroot.
        private readonly IWebHostEnvironment _environment;

        public FamiliaProductoController(
            IFamiliaProductoLN familiaProductoLN,
            IWebHostEnvironment environment)
        {
            _familiaProductoLN = familiaProductoLN;
            _environment = environment;
        }

        /// <summary>Lista todas las familias para mantenimiento administrativo.</summary>
        [HttpGet("Listar")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Listar()
        {
            var resultado = await _familiaProductoLN.ListarAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Lista únicamente familias activas disponibles en la navegación del Cliente.</summary>
        [HttpGet("Cliente")]
        [Authorize(Roles = "Cliente")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarCliente()
        {
            var resultado = await _familiaProductoLN.ListarClienteAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [HttpGet("Obtener/{id}")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Obtener(int id)
        {
            var resultado = await _familiaProductoLN.ObtenerAsync(new TFamiliaProducto { FamiliaId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
            return Ok(resultado);
        }

        [HttpGet("Buscar")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Buscar(string nombre)
        {
            var resultado = await _familiaProductoLN.BuscarAsync(new TFamiliaProducto { Nombre = nombre });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Crea una familia después de validar datos obligatorios y duplicados.</summary>
        [HttpPost("Insertar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Insertar([FromBody] TFamiliaProducto familia)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _familiaProductoLN.InsertarAsync(familia);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Actualiza los datos de una familia existente.</summary>
        [HttpPut("Modificar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Modificar([FromBody] TFamiliaProducto familia)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _familiaProductoLN.ModificarAsync(familia);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }


        // Permite al administrador subir o cambiar la imagen de una familia.
        [Authorize(Roles = "Administrador")]
        [HttpPost("SubirImagen/{familiaId:int}")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> SubirImagen(
            int familiaId,
            IFormFile archivo)
        {
            // Verifica que el ID de la familia sea válido.
            if (familiaId <= 0)
                return BadRequest("Familia inválida.");

            // Verifica que se haya seleccionado una imagen.
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Debe seleccionar una imagen.");

            // La imagen puede pesar como máximo 5 MB.
            if (archivo.Length > 5 * 1024 * 1024)
                return BadRequest("La imagen supera los 5 MB.");

            // Extensiones permitidas.
            var extensionesPermitidas = new[]
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            // Obtiene y valida la extensión del archivo.
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
                return BadRequest("El archivo seleccionado no es una imagen permitida.");

            // Busca la familia para comprobar que exista.
            var respuestaFamilia = await _familiaProductoLN.ObtenerAsync(
                new TFamiliaProducto { FamiliaId = familiaId });

            if (respuestaFamilia.Data == null)
                return NotFound("La familia no existe.");

            // Obtiene la ubicación de wwwroot.
            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            // Crea una carpeta para guardar las imágenes de la familia.
            var carpetaFamilia = Path.Combine(
                webRoot,
                "familias",
                familiaId.ToString());

            Directory.CreateDirectory(carpetaFamilia);

            // Crea un nombre único para la nueva imagen.
            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";

            var rutaFisica = Path.Combine(
                carpetaFamilia,
                nombreArchivo);

            // Guarda físicamente la imagen.
            await using (var stream = new FileStream(
                rutaFisica,
                FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Crea la URL que utilizará Angular.
            var urlImagen =
                $"{Request.Scheme}://{Request.Host}" +
                $"/familias/{familiaId}/{nombreArchivo}";

            // Guarda la URL dentro de la familia.
            var familia = respuestaFamilia.Data;
            familia.UrlImagen = urlImagen;

            var resultado = await _familiaProductoLN.ModificarAsync(familia);

            if (!string.IsNullOrEmpty(resultado.Error))
            {
                // Si falla la BD, elimina el archivo que acabamos de crear.
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);

                return BadRequest(resultado);
            }

            return Ok(resultado);
        }

        /// <summary>Desactiva lógicamente una familia sin borrar sus relaciones.</summary>
        [HttpDelete("Eliminar/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _familiaProductoLN.EliminarAsync(new TFamiliaProducto { FamiliaId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }
    }
}
