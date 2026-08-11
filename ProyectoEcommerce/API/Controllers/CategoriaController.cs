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
        public CategoriaController(ICategoriaLN categoriaLN) { _categoriaLN = categoriaLN; }

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
