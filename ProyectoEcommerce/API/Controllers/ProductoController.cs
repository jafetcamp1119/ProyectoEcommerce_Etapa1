using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;

namespace ProyectoEcommerce.API.Controllers;

// este controller comparte las consultas del catalogo y el mantenimiento de productos
// cada metodo marca si puede entrar cualquier usuario autenticado o solo el Administrador
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ProductoController : ControllerBase
{
    private readonly IProductoLN _productoLN;

    public ProductoController(IProductoLN productoLN)
    {
        _productoLN = productoLN;
    }

    // recibe filtros por query string y devuelve una pagina de productos activos
    [HttpGet("Catalogo")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Catalogo([FromQuery] TFiltroProductos filtro)
    {
        // para el Cliente obliga a navegar Familia -> Categoria o a escribir una busqueda global
        if (User.IsInRole("Cliente"))
        {
            if (filtro.CategoriaId.HasValue && !filtro.FamiliaId.HasValue)
                return BadRequest(new { success = false, error = "La categoría debe consultarse dentro de su familia." });
            if (!filtro.FamiliaId.HasValue && !filtro.CategoriaId.HasValue && string.IsNullOrWhiteSpace(filtro.Texto))
                return BadRequest(new { success = false, error = "Selecciona una familia o escribe un producto para buscar." });
        }
        var resultado = await _productoLN.ListarCatalogoAsync(filtro);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // trae productos activos e inactivos para la tabla administrativa
    [Authorize(Roles = "Administrador")]
    [HttpGet("Administracion")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Administracion([FromQuery] TFiltroProductos filtro)
    {
        var resultado = await _productoLN.ListarAdministracionAsync(filtro);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // trae el detalle publico de un producto activo o responde 404 si no esta disponible
    [HttpGet("Detalle/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Detalle(int id)
    {
        if (id <= 0) return NotFound();
        var resultado = await _productoLN.ObtenerCatalogoAsync(id);
        if (resultado.Error == Mensajes.ProductoNoEncontrado) return NotFound(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return StatusCode(StatusCodes.Status500InternalServerError, resultado);
        return Ok(resultado);
    }

    // trae todos los datos del producto aunque este inactivo para poder editarlo
    [Authorize(Roles = "Administrador")]
    [HttpGet("DetalleAdministracion/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DetalleAdministracion(int id)
    {
        if (id <= 0) return NotFound();
        var resultado = await _productoLN.ObtenerAdministracionAsync(id);
        if (resultado.Error == Mensajes.ProductoNoEncontrado) return NotFound(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return StatusCode(StatusCodes.Status500InternalServerError, resultado);
        return Ok(resultado);
    }

    // devuelve las listas que llenan los select de familia, categoria e impuesto
    [HttpGet("Catalogos")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Catalogos()
    {
        var resultado = await _productoLN.ListarCatalogosAsync();
        if (!string.IsNullOrEmpty(resultado.Error)) return StatusCode(StatusCodes.Status500InternalServerError, resultado);
        return Ok(resultado);
    }

    // actualiza despues de revisar categoria, impuesto, cantidades y codigo unico
    [Authorize(Roles = "Administrador")]
    [HttpPut("Modificar")]
    public async Task<IActionResult> Modificar([FromBody] TProducto producto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _productoLN.ModificarAsync(producto, UsuarioIdActual());
        if (resultado.Error == Mensajes.ProductoNoEncontrado) return NotFound(resultado);
        if (resultado.Error == Mensajes.CodigoProductoDuplicado) return Conflict(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // cambia Activo sin borrar el producto ni sus imagenes o ventas anteriores
    // la ruta vieja de eliminar se conserva pero por dentro tambien hace una desactivacion logica
    [Authorize(Roles = "Administrador")]
    [HttpPut("CambiarEstado/{id:int}")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] TCambioEstadoProducto cambio)
    {
        var resultado = await _productoLN.CambiarEstadoAsync(id, cambio.Activo, UsuarioIdActual());
        if (resultado.Error == Mensajes.ProductoNoEncontrado) return NotFound(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("Eliminar/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await _productoLN.CambiarEstadoAsync(id, false, UsuarioIdActual());
        if (resultado.Error == Mensajes.ProductoNoEncontrado) return NotFound(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // el Administrador sale de los Claims del JWT y no de un ID que Angular pueda cambiar
    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
