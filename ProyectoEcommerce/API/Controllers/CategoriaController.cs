using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers
{
    /// <summary>
    /// Gestiona categorías y expone al Cliente las categorías activas de una familia válida.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriaController : ControllerBase
    {
        private ICategoriaLN _categoriaLN { get; }
        // Permite conocer la ubicación del proyecto
        // para guardar las imágenes de las categorías.
        private readonly IWebHostEnvironment _environment;

        public CategoriaController(
            ICategoriaLN categoriaLN,
            IWebHostEnvironment environment)
        {
            _categoriaLN = categoriaLN;
            _environment = environment;
        }

        /// <summary>Lista categorías para la administración.</summary>
        [HttpGet("Listar")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Listar()
        {
            var resultado = await _categoriaLN.ListarAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Lista categorías administrativas pertenecientes a la familia indicada.</summary>
        [HttpGet("ListarPorFamilia/{familiaId:int}")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarPorFamilia(int familiaId)
        {
            var resultado = await _categoriaLN.ListarPorFamiliaAsync(familiaId);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Lista categorías activas de una familia activa para el catálogo del Cliente.</summary>
        [HttpGet("Cliente/{familiaId:int}")]
        [Authorize(Roles = "Cliente")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarClientePorFamilia(int familiaId)
        {
            var resultado = await _categoriaLN.ListarClientePorFamiliaAsync(familiaId);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [HttpGet("Obtener/{id}")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Obtener(int id)
        {
            var resultado = await _categoriaLN.ObtenerAsync(new TCategoria { CategoriaId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
            return Ok(resultado);
        }

        [HttpGet("Buscar")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Buscar(string nombre)
        {
            var resultado = await _categoriaLN.BuscarAsync(new TCategoria { Nombre = nombre });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Crea una categoría vinculada a una familia existente.</summary>
        [HttpPost("Insertar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Insertar([FromBody] TCategoria categoria)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _categoriaLN.InsertarAsync(categoria);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Actualiza una categoría manteniendo la relación real con su familia.</summary>
        [HttpPut("Modificar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Modificar([FromBody] TCategoria categoria)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _categoriaLN.ModificarAsync(categoria);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }


        // Permite al administrador subir o cambiar la imagen de una categoría.
        [Authorize(Roles = "Administrador")]
        [HttpPost("SubirImagen/{categoriaId:int}")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> SubirImagen(
            int categoriaId,
            IFormFile archivo)
        {
            // Verifica que el ID sea válido.
            if (categoriaId <= 0)
                return BadRequest("Categoría inválida.");

            // Verifica que se haya seleccionado una imagen.
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Debe seleccionar una imagen.");

            // Cada imagen puede pesar como máximo 5 MB.
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

            var extension =
                Path.GetExtension(archivo.FileName)
                    .ToLowerInvariant();

            // Verifica que el archivo sea una imagen permitida.
            if (!extensionesPermitidas.Contains(extension))
                return BadRequest(
                    "El archivo seleccionado no es una imagen permitida.");

            // Busca la categoría para comprobar que exista.
            var respuestaCategoria =
                await _categoriaLN.ObtenerAsync(
                    new TCategoria
                    {
                        CategoriaId = categoriaId
                    });

            if (respuestaCategoria.Data == null)
                return NotFound("La categoría no existe.");

            // Obtiene la ubicación de wwwroot.
            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            // Crea una carpeta específica para la categoría.
            var carpetaCategoria = Path.Combine(
                webRoot,
                "categorias",
                categoriaId.ToString());

            Directory.CreateDirectory(carpetaCategoria);

            // Genera un nombre único para la imagen.
            var nombreArchivo =
                $"{Guid.NewGuid():N}{extension}";

            var rutaFisica = Path.Combine(
                carpetaCategoria,
                nombreArchivo);

            // Guarda físicamente la imagen.
            await using (var stream =
                new FileStream(
                    rutaFisica,
                    FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Construye la URL que utilizará Angular.
            var urlImagen =
                $"{Request.Scheme}://{Request.Host}" +
                $"/categorias/{categoriaId}/{nombreArchivo}";

            // Guarda la URL en la categoría.
            var categoria = respuestaCategoria.Data;
            categoria.UrlImagen = urlImagen;

            var resultado =
                await _categoriaLN.ModificarAsync(categoria);

            // Si falla la base de datos,
            // elimina también el archivo físico.
            if (!string.IsNullOrEmpty(resultado.Error))
            {
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);

                return BadRequest(resultado);
            }

            return Ok(resultado);
        }

        /// <summary>Desactiva lógicamente una categoría.</summary>
        [HttpDelete("Eliminar/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _categoriaLN.EliminarAsync(new TCategoria { CategoriaId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }
    }
}
