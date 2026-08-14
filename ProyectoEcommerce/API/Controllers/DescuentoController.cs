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

    // lista descuentos activos e inactivos para la pantalla administrativa
    [Authorize(Roles = "Administrador")]
    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _descuentoLN.ListarAsync();
        // el ternario devuelve 200 si no hay error y 400 cuando la LN mando un mensaje
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    // trae un descuento por ID o responde 404 si no existe
    [Authorize(Roles = "Administrador")]
    [HttpGet("Obtener/{descuentoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(int descuentoId)
    {
        var resultado = await _descuentoLN.ObtenerAsync(descuentoId);
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : NotFound(resultado);
    }

    // junta familias, categorias y productos para llenar los select del formulario
    [Authorize(Roles = "Administrador")]
    [HttpGet("Catalogos")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Catalogos()
    {
        var resultado = await _descuentoLN.ListarCatalogosAsync();
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    // recibe un descuento nuevo y pasa tambien el ID del Administrador para la bitacora
    [Authorize(Roles = "Administrador")]
    [HttpPost("Insertar")]
    public async Task<IActionResult> Insertar([FromBody] TDescuento datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _descuentoLN.InsertarAsync(datos, UsuarioIdActual());
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    // recibe los cambios de un descuento existente y deja que la LN haga las validaciones
    [Authorize(Roles = "Administrador")]
    [HttpPut("Modificar")]
    public async Task<IActionResult> Modificar([FromBody] TDescuento datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _descuentoLN.ModificarAsync(datos, UsuarioIdActual());
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    // cambia solamente el estado logico para no borrar el historial del descuento
    [Authorize(Roles = "Administrador")]
    [HttpPut("Estado/{descuentoId:int}")]
    public async Task<IActionResult> Estado(int descuentoId, [FromBody] TCambioEstadoDescuento datos)
    {
        var resultado = await _descuentoLN.CambiarEstadoAsync(descuentoId, datos.Activo, UsuarioIdActual());
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : BadRequest(resultado);
    }

    // devuelve el mejor descuento ya calculado para que Angular no tenga que decidir cual gana
    [HttpGet("AplicadoProducto/{productoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> AplicadoProducto(int productoId)
    {
        var resultado = await _descuentoLN.ObtenerMejorDescuentoAsync(productoId);
        return string.IsNullOrEmpty(resultado.Error) ? Ok(resultado) : NotFound(resultado);
    }

    // agarra del JWT el ID que se usa para registrar quien hizo el cambio
    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
