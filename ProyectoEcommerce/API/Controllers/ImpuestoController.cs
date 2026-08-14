using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers
{
    // este controller recibe las solicitudes de la pantalla de impuestos
    // el Authorize de la clase deja entrar solamente al Administrador
    [Authorize(Roles = "Administrador")]
    [Route("api/[controller]")]
    [ApiController]
    public class ImpuestoController : ControllerBase
    {
        private IImpuestoLN _impuestoLN { get; }
        public ImpuestoController(IImpuestoLN impuestoLN) { _impuestoLN = impuestoLN; }

        // pide a la LN todos los impuestos configurados
        [HttpGet("Listar")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Listar()
        {
            var resultado = await _impuestoLN.ListarAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // recibe un ID desde la ruta y devuelve el impuesto o un 404
        [HttpGet("Obtener/{id}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Obtener(int id)
        {
            var resultado = await _impuestoLN.ObtenerAsync(new TImpuesto { ImpuestoId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
            return Ok(resultado);
        }

        // recibe el nombre desde el query string y devuelve sus coincidencias
        [HttpGet("Buscar")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Buscar(string nombre)
        {
            var resultado = await _impuestoLN.BuscarAsync(new TImpuesto { Nombre = nombre });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // recibe el formulario y la LN revisa nombre, porcentaje y fechas antes de guardar
        [HttpPost("Insertar")]
        public async Task<IActionResult> Insertar([FromBody] TImpuesto impuesto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _impuestoLN.InsertarAsync(impuesto);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // actualiza un impuesto existente con los datos que mando Angular
        [HttpPut("Modificar")]
        public async Task<IActionResult> Modificar([FromBody] TImpuesto impuesto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _impuestoLN.ModificarAsync(impuesto);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // manda el ID a la LN para desactivarlo sin borrar referencias historicas
        [HttpDelete("Eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _impuestoLN.EliminarAsync(new TImpuesto { ImpuestoId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }
    }
}
