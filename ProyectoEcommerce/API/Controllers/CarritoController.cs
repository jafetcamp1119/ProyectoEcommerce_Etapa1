using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;

namespace ProyectoEcommerce.API.Controllers;

[Authorize(Roles = "Cliente")]
[Route("api/[controller]")]
[ApiController]
// recibe las solicitudes del carrito del Cliente autenticado
// el UsuarioId siempre sale del JWT para que nadie pueda mandar el ID de otro Cliente
public class CarritoController : ControllerBase
{
    private readonly ICarritoLN _carritoLN;

    public CarritoController(ICarritoLN carritoLN)
    {
        _carritoLN = carritoLN;
    }

    // recibe producto y cantidad, la LN revisa stock y devuelve el carrito actualizado
    [HttpPost("Agregar")]
    public async Task<IActionResult> Agregar([FromBody] TAgregarProductoCarrito datos)
    {
        // ModelState revisa las reglas que tienen las propiedades del DTO
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _carritoLN.AgregarAsync(datos, UsuarioIdActual());
        // Conflict usa el codigo HTTP 409 porque el stock actual choca con la cantidad pedida
        if (resultado.Error == Mensajes.StockInsuficienteCarrito) return Conflict(resultado);
        if (resultado.Error == Mensajes.ProductoNoDisponibleCarrito) return BadRequest(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // trae el carrito del Cliente con sus lineas y totales calculados en el servidor
    [HttpGet("Actual")]
    [HttpGet("Resumen")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Actual()
    {
        var resultado = await _carritoLN.ObtenerActualAsync(UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // recibe el ID del detalle y su nueva cantidad, luego devuelve todo el carrito recalculado
    [HttpPut("Cantidad")]
    public async Task<IActionResult> ActualizarCantidad([FromBody] TActualizarCantidadCarrito datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _carritoLN.ActualizarCantidadAsync(datos, UsuarioIdActual());
        if (resultado.Error == Mensajes.StockInsuficienteCarrito) return Conflict(resultado);
        if (resultado.Error == Mensajes.ProductoNoDisponibleCarrito) return BadRequest(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // quita una linea solo si pertenece al carrito del usuario que viene en el token
    [HttpDelete("Detalle/{carritoDetalleId:int}")]
    public async Task<IActionResult> EliminarDetalle(int carritoDetalleId)
    {
        var resultado = await _carritoLN.EliminarDetalleAsync(carritoDetalleId, UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // FindFirstValue busca el Claim del ID dentro del JWT
    // si falta o no es un numero devuelve cero y la LN lo rechaza
    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
