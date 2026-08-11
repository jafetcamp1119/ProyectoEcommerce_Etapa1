using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;

namespace ProyectoEcommerce.API.Controllers;

/// <summary>
/// Permite consultar las imágenes ya asociadas a los productos del catálogo.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ProductoImagenController : ControllerBase
{
    private readonly IProductoImagenLN _productoImagenLN;

    public ProductoImagenController(IProductoImagenLN productoImagenLN)
    {
        _productoImagenLN = productoImagenLN;
    }

    /// <summary>Lista las imágenes de un producto respetando si el solicitante puede ver productos inactivos.</summary>
    [HttpGet("ListarPorProducto/{productoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ListarPorProducto(int productoId)
    {
        if (productoId <= 0) return NotFound();
        var resultado = await _productoImagenLN.ListarPorProductoAsync(productoId, User.IsInRole("Administrador"));
        if (resultado.Error == Mensajes.ProductoNoEncontrado) return NotFound(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Obtiene la imagen principal utilizada por las tarjetas y el detalle del producto.</summary>
    [HttpGet("ObtenerPrincipal/{productoId:int}")]
    [HttpGet("Principal/{productoId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ObtenerPrincipal(int productoId)
    {
        if (productoId <= 0) return NotFound();
        var resultado = await _productoImagenLN.ObtenerPrincipalAsync(productoId, User.IsInRole("Administrador"));
        if (resultado.Error == Mensajes.ProductoNoEncontrado) return NotFound(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }
}
