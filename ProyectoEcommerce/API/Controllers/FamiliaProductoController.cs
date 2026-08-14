using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers
{
    // recibe las solicitudes de familias que forman el primer nivel del catalogo
    // Authorize obliga a tener sesion y cada ruta indica si ademas necesita ser Administrador
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FamiliaProductoController : ControllerBase
    {
        private IFamiliaProductoLN _familiaProductoLN { get; }

        // da la ubicacion del proyecto para guardar las imagenes dentro de wwwroot
        private readonly IWebHostEnvironment _environment;

        public FamiliaProductoController(
            IFamiliaProductoLN familiaProductoLN,
            IWebHostEnvironment environment)
        {
            _familiaProductoLN = familiaProductoLN;
            _environment = environment;
        }

        // lista todas las familias para mantenimiento, incluidas las inactivas
        [HttpGet("Listar")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Listar()
        {
            var resultado = await _familiaProductoLN.ListarAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // para el Cliente solo devuelve familias activas que se pueden navegar
        [HttpGet("Cliente")]
        [Authorize(Roles = "Cliente")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarCliente()
        {
            var resultado = await _familiaProductoLN.ListarClienteAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // trae una familia por ID o devuelve 404 si no existe
        [HttpGet("Obtener/{id}")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Obtener(int id)
        {
            var resultado = await _familiaProductoLN.ObtenerAsync(new TFamiliaProducto { FamiliaId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
            return Ok(resultado);
        }

        // busca familias cuyo nombre contiene el texto del query string
        [HttpGet("Buscar")]
        [Authorize(Roles = "Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Buscar(string nombre)
        {
            var resultado = await _familiaProductoLN.BuscarAsync(new TFamiliaProducto { Nombre = nombre });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // recibe una familia nueva y la LN revisa datos obligatorios y duplicados
        [HttpPost("Insertar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Insertar([FromBody] TFamiliaProducto familia)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _familiaProductoLN.InsertarAsync(familia);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // guarda nombre, descripcion, UrlImagen y estado de una familia existente
        [HttpPut("Modificar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Modificar([FromBody] TFamiliaProducto familia)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _familiaProductoLN.ModificarAsync(familia);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }


        // recibe una imagen del formulario, la guarda en wwwroot y pone su URL en la familia
        [Authorize(Roles = "Administrador")]
        [HttpPost("SubirImagen/{familiaId:int}")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> SubirImagen(
            int familiaId,
            IFormFile archivo)
        {
            // primero revisa que el ID y el archivo tengan datos utiles
            if (familiaId <= 0)
                return BadRequest("Familia inválida.");

            if (archivo == null || archivo.Length == 0)
                return BadRequest("Debe seleccionar una imagen.");

            // no deja archivos mayores a 5 MB para cuidar espacio y tiempo de carga
            if (archivo.Length > 5 * 1024 * 1024)
                return BadRequest("La imagen supera los 5 MB.");

            // solo acepta los formatos de imagen que usa el formulario de Angular
            var extensionesPermitidas = new[]
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            // ToLowerInvariant deja la extension igual aunque venga como .JPG o .Png
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
                return BadRequest("El archivo seleccionado no es una imagen permitida.");

            // busca la familia antes de crear carpetas o archivos para un ID que no existe
            var respuestaFamilia = await _familiaProductoLN.ObtenerAsync(
                new TFamiliaProducto { FamiliaId = familiaId });

            if (respuestaFamilia.Data == null)
                return NotFound("La familia no existe.");

            // normalmente WebRootPath ya apunta a wwwroot
            // el Path.Combine de abajo sirve de respaldo si el servidor no lo preparo
            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            // cada familia tiene su propia carpeta para no mezclar los archivos
            var carpetaFamilia = Path.Combine(
                webRoot,
                "familias",
                familiaId.ToString());

            // CreateDirectory no falla si la carpeta ya existia
            Directory.CreateDirectory(carpetaFamilia);

            // el Guid crea un nombre unico y conserva la extension validada
            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";

            var rutaFisica = Path.Combine(
                carpetaFamilia,
                nombreArchivo);

            // CopyToAsync copia lo que llego por HTTP al archivo de wwwroot
            await using (var stream = new FileStream(
                rutaFisica,
                FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // arma la URL publica que Angular usara en [src]
            var urlImagen =
                $"{Request.Scheme}://{Request.Host}" +
                $"/familias/{familiaId}/{nombreArchivo}";

            // manda la URL por el flujo normal de modificacion hasta la BD
            var familia = respuestaFamilia.Data;
            familia.UrlImagen = urlImagen;

            var resultado = await _familiaProductoLN.ModificarAsync(familia);

            if (!string.IsNullOrEmpty(resultado.Error))
            {
                // si la BD falla quita el archivo nuevo para no dejarlo suelto
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);

                return BadRequest(resultado);
            }

            return Ok(resultado);
        }

        // desactiva la familia sin borrar sus categorias ni productos
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
