using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers
{
    /// <summary>
    /// Gestiona la configuración de impuestos utilizada para desglosar precios y facturas.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    [Route("api/[controller]")]
    [ApiController]
    public class ImpuestoController : ControllerBase
    {
        private IImpuestoLN _impuestoLN { get; }
        public ImpuestoController(IImpuestoLN impuestoLN) { _impuestoLN = impuestoLN; }

        /// <summary>Lista los impuestos configurados.</summary>
        [HttpGet("Listar")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Listar()
        {
            var resultado = await _impuestoLN.ListarAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [HttpGet("Obtener/{id}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Obtener(int id)
        {
            var resultado = await _impuestoLN.ObtenerAsync(new TImpuesto { ImpuestoId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
            return Ok(resultado);
        }

        [HttpGet("Buscar")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Buscar(string nombre)
        {
            var resultado = await _impuestoLN.BuscarAsync(new TImpuesto { Nombre = nombre });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Crea un impuesto después de validar nombre y porcentaje.</summary>
        [HttpPost("Insertar")]
        public async Task<IActionResult> Insertar([FromBody] TImpuesto impuesto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _impuestoLN.InsertarAsync(impuesto);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Actualiza un impuesto existente.</summary>
        [HttpPut("Modificar")]
        public async Task<IActionResult> Modificar([FromBody] TImpuesto impuesto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _impuestoLN.ModificarAsync(impuesto);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        /// <summary>Desactiva el impuesto sin eliminar referencias históricas.</summary>
        [HttpDelete("Eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _impuestoLN.EliminarAsync(new TImpuesto { ImpuestoId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }
    }
}
