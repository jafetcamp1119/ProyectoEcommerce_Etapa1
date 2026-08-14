using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers
{
    // recibe el mantenimiento de categorias y la consulta que usa el catalogo del Cliente
    // cada categoria sigue ligada a su familia durante todo el flujo
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriaController : ControllerBase
    {
        private ICategoriaLN _categoriaLN { get; }
        // permite encontrar wwwroot para guardar las imagenes de categorias
        private readonly IWebHostEnvironment _environment;

        public CategoriaController(
            ICategoriaLN categoriaLN,
            IWebHostEnvironment environment)
        {
            _categoriaLN = categoriaLN;
            _environment = environment;
        }

        // lista todas las categorias para administracion
        [HttpGet("Listar")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Listar()
        {
            var resultado = await _categoriaLN.ListarAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // recibe una familia y trae sus categorias activas e inactivas para administracion
        [HttpGet("ListarPorFamilia/{familiaId:int}")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarPorFamilia(int familiaId)
        {
            var resultado = await _categoriaLN.ListarPorFamiliaAsync(familiaId);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // para el Cliente solo devuelve categorias activas de una familia activa
        [HttpGet("Cliente/{familiaId:int}")]
        [Authorize(Roles = "Cliente")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarClientePorFamilia(int familiaId)
        {
            var resultado = await _categoriaLN.ListarClientePorFamiliaAsync(familiaId);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // trae una categoria por ID o devuelve 404
        [HttpGet("Obtener/{id}")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Obtener(int id)
        {
            var resultado = await _categoriaLN.ObtenerAsync(new TCategoria { CategoriaId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
            return Ok(resultado);
        }

        // busca categorias usando el nombre del query string
        [HttpGet("Buscar")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Buscar(string nombre)
        {
            var resultado = await _categoriaLN.BuscarAsync(new TCategoria { Nombre = nombre });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // guarda los cambios de la categoria, incluida su UrlImagen opcional
        [HttpPut("Modificar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Modificar([FromBody] TCategoria categoria)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _categoriaLN.ModificarAsync(categoria);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }


        // recibe una imagen, la guarda en wwwroot y actualiza UrlImagen por medio de la LN
        [Authorize(Roles = "Administrador")]
        [HttpPost("SubirImagen/{categoriaId:int}")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> SubirImagen(
            int categoriaId,
            IFormFile archivo)
        {
            // primero revisa el ID, que haya archivo y que no supere el limite
            if (categoriaId <= 0)
                return BadRequest("Categoría inválida.");

            // Verifica que se haya seleccionado una imagen.
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Debe seleccionar una imagen.");

            // el limite evita subir archivos demasiado pesados
            if (archivo.Length > 5 * 1024 * 1024)
                return BadRequest("La imagen supera los 5 MB.");

            // estos son los mismos formatos que permite escoger Angular
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

            // Contains compara la extension normalizada con la lista permitida
            if (!extensionesPermitidas.Contains(extension))
                return BadRequest(
                    "El archivo seleccionado no es una imagen permitida.");

            // no crea carpetas hasta confirmar que la categoria existe
            var respuestaCategoria =
                await _categoriaLN.ObtenerAsync(
                    new TCategoria
                    {
                        CategoriaId = categoriaId
                    });

            if (respuestaCategoria.Data == null)
                return NotFound("La categoría no existe.");

            // usa WebRootPath y si viene vacio arma wwwroot desde la raiz del proyecto
            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            // separa los archivos por categoria para que sus rutas sean faciles de reconocer
            var carpetaCategoria = Path.Combine(
                webRoot,
                "categorias",
                categoriaId.ToString());

            // se puede llamar aunque la carpeta ya exista
            Directory.CreateDirectory(carpetaCategoria);

            // el Guid evita que una imagen nueva sobrescriba otra por tener el mismo nombre
            var nombreArchivo =
                $"{Guid.NewGuid():N}{extension}";

            var rutaFisica = Path.Combine(
                carpetaCategoria,
                nombreArchivo);

            // copia el archivo recibido a su ruta dentro de wwwroot
            await using (var stream =
                new FileStream(
                    rutaFisica,
                    FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // esta URL completa es la que despues llega a [src] en Angular
            var urlImagen =
                $"{Request.Scheme}://{Request.Host}" +
                $"/categorias/{categoriaId}/{nombreArchivo}";

            // usa ModificarAsync para que la URL pase por las mismas reglas que cualquier cambio
            var categoria = respuestaCategoria.Data;
            categoria.UrlImagen = urlImagen;

            var resultado =
                await _categoriaLN.ModificarAsync(categoria);

            // si la BD no pudo guardar la URL tambien borra el archivo recien creado
            if (!string.IsNullOrEmpty(resultado.Error))
            {
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);

                return BadRequest(resultado);
            }

            return Ok(resultado);
        }

        // cambia Activo a false sin borrar productos ni relaciones
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
