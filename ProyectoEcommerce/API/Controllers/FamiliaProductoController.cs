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

        public FamiliaProductoController(IFamiliaProductoLN familiaProductoLN)
        {
            _familiaProductoLN = familiaProductoLN;
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
