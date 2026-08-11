using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;

namespace ProyectoEcommerce.API.Controllers;

/// <summary>
/// Expone el catálogo para Clientes y las operaciones de mantenimiento reservadas al Administrador.
/// </summary>
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

    /// <summary>Obtiene productos activos aplicando el alcance global, de familia o de categoría solicitado.</summary>
    [HttpGet("Catalogo")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Catalogo([FromQuery] TFiltroProductos filtro)
    {
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

    /// <summary>Obtiene el catálogo administrativo, incluidos filtros y estados de mantenimiento.</summary>
    [Authorize(Roles = "Administrador")]
    [HttpGet("Administracion")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Administracion([FromQuery] TFiltroProductos filtro)
    {
        var resultado = await _productoLN.ListarAdministracionAsync(filtro);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Obtiene el detalle visible de un producto activo.</summary>
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

    /// <summary>Devuelve familias, categorías e impuestos activos usados por las pantallas de producto.</summary>
    [HttpGet("Catalogos")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Catalogos()
    {
        var resultado = await _productoLN.ListarCatalogosAsync();
        if (!string.IsNullOrEmpty(resultado.Error)) return StatusCode(StatusCodes.Status500InternalServerError, resultado);
        return Ok(resultado);
    }

    /// <summary>Crea un producto y registra al Administrador responsable en la bitácora.</summary>
    [Authorize(Roles = "Administrador")]
    [HttpPost("Insertar")]
    [HttpPost("Crear")]
    public async Task<IActionResult> Insertar([FromBody] TProducto producto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _productoLN.InsertarAsync(producto, UsuarioIdActual());
        if (resultado.Error == Mensajes.CodigoProductoDuplicado) return Conflict(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Modifica un producto existente después de validar categoría, impuesto y código.</summary>
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

    /// <summary>Activa o desactiva lógicamente un producto; no elimina su información histórica.</summary>
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

    // El Administrador se identifica por el JWT para registrar cambios sin confiar en datos enviados por Angular.
    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
