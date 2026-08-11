using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DescuentoController : ControllerBase
{
    private readonly IDescuentoLN _descuentoLN;

    public DescuentoController(IDescuentoLN descuentoLN)
    {
        _descuentoLN = descuentoLN;
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _descuentoLN.ListarAsync();
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("Obtener/{descuentoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(int descuentoId)
    {
        var resultado = await _descuentoLN.ObtenerAsync(descuentoId);
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : NotFound(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("Catalogos")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Catalogos()
    {
        var resultado = await _descuentoLN.ListarCatalogosAsync();
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("Insertar")]
    public async Task<IActionResult> Insertar([FromBody] TDescuento datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _descuentoLN.InsertarAsync(datos, UsuarioIdActual());
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("Modificar")]
    public async Task<IActionResult> Modificar([FromBody] TDescuento datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _descuentoLN.ModificarAsync(datos, UsuarioIdActual());
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("Estado/{descuentoId:int}")]
    public async Task<IActionResult> Estado(int descuentoId, [FromBody] TCambioEstadoDescuento datos)
    {
        var resultado = await _descuentoLN.CambiarEstadoAsync(descuentoId, datos.Activo, UsuarioIdActual());
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>Devuelve el precio efectivo ya resuelto; no delega la selección a Angular.</summary>
    [HttpGet("AplicadoProducto/{productoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> AplicadoProducto(int productoId)
    {
        var resultado = await _descuentoLN.ObtenerMejorDescuentoAsync(productoId);
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : NotFound(resultado);
    }

    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
