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
/// <summary>
/// Expone las operaciones del carrito abierto del Cliente autenticado.
/// El UsuarioId siempre se obtiene del JWT y nunca del cuerpo enviado por Angular.
/// </summary>
public class CarritoController : ControllerBase
{
    private readonly ICarritoLN _carritoLN;

    public CarritoController(ICarritoLN carritoLN)
    {
        _carritoLN = carritoLN;
    }

    /// <summary>Agrega un producto al carrito actual después de validar disponibilidad y stock.</summary>
    /// <param name="datos">Producto y cantidad solicitada.</param>
    /// <returns>Resumen del carrito actualizado.</returns>
    [HttpPost("Agregar")]
    public async Task<IActionResult> Agregar([FromBody] TAgregarProductoCarrito datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _carritoLN.AgregarAsync(datos, UsuarioIdActual());
        if (resultado.Error == Mensajes.StockInsuficienteCarrito) return Conflict(resultado);
        if (resultado.Error == Mensajes.ProductoNoDisponibleCarrito) return BadRequest(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Obtiene el carrito activo perteneciente al Cliente identificado por el JWT.</summary>
    /// <returns>Carrito con artículos y totales calculados en el servidor.</returns>
    [HttpGet("Actual")]
    [HttpGet("Resumen")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Actual()
    {
        var resultado = await _carritoLN.ObtenerActualAsync(UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Actualiza la cantidad de un detalle sin permitir superar el stock disponible.</summary>
    /// <param name="datos">Detalle y nueva cantidad.</param>
    /// <returns>Carrito recalculado.</returns>
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

    /// <summary>Elimina un detalle únicamente si pertenece al carrito del Cliente autenticado.</summary>
    /// <param name="carritoDetalleId">Identificador del detalle que se desea retirar.</param>
    /// <returns>Carrito actualizado.</returns>
    [HttpDelete("Detalle/{carritoDetalleId:int}")]
    public async Task<IActionResult> EliminarDetalle(int carritoDetalleId)
    {
        var resultado = await _carritoLN.EliminarDetalleAsync(carritoDetalleId, UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // La identidad se toma del token para impedir que un Cliente opere sobre el carrito de otro usuario.
    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
