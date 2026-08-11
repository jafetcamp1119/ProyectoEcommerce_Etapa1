using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
/// <summary>
/// Coordina checkout, confirmación de ventas, consulta de órdenes y descarga protegida de facturas.
/// </summary>
public class OrdenController : ControllerBase
{
    private readonly IOrdenLN _ordenLN;
    private readonly IWebHostEnvironment _entorno;

    public OrdenController(IOrdenLN ordenLN, IWebHostEnvironment entorno)
    {
        _ordenLN = ordenLN;
        _entorno = entorno;
    }

    /// <summary>Prepara los datos del carrito y del Cliente necesarios para mostrar el checkout.</summary>
    /// <returns>Cliente, carrito vigente y totales recalculados.</returns>
    [Authorize(Roles = "Cliente")]
    [HttpGet("Checkout")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Checkout()
    {
        var resultado = await _ordenLN.PrepararCheckoutAsync(UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Confirma una compra del Cliente mediante la transacción definida en la lógica de negocio.</summary>
    /// <param name="datos">Correo, dirección y método de pago seleccionados.</param>
    /// <param name="cancellationToken">Permite cancelar la solicitud HTTP.</param>
    /// <returns>Resultado de la venta, factura y envío de correo.</returns>
    [Authorize(Roles = "Cliente")]
    [HttpPost("ConfirmarCompra")]
    public async Task<IActionResult> ConfirmarCompra([FromBody] TConfirmarCompra datos, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _ordenLN.ConfirmarCompraAsync(datos, UsuarioIdActual(), cancellationToken);
        if (resultado.Error.Contains("stock suficiente", StringComparison.OrdinalIgnoreCase)) return Conflict(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Lista únicamente las órdenes que pertenecen al Cliente autenticado.</summary>
    [Authorize(Roles = "Cliente")]
    [HttpGet("MisOrdenes")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> MisOrdenes([FromQuery] TFiltroOrdenes filtro)
    {
        var resultado = await _ordenLN.ListarClienteAsync(filtro, UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Lista órdenes de todos los Clientes para la vista administrativa.</summary>
    [Authorize(Roles = "Administrador")]
    [HttpGet("Administracion")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Administracion([FromQuery] TFiltroOrdenes filtro)
    {
        var resultado = await _ordenLN.ListarAdministracionAsync(filtro);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    /// <summary>Obtiene el detalle si la orden pertenece al Cliente o el solicitante es Administrador.</summary>
    [HttpGet("Detalle/{ordenId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Detalle(int ordenId)
    {
        var resultado = await _ordenLN.ObtenerDetalleAsync(ordenId, UsuarioIdActual(), User.IsInRole("Administrador"));
        if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
        return Ok(resultado);
    }

    /// <summary>Descarga el PDF validando propiedad de la orden y que la ruta permanezca dentro de wwwroot.</summary>
    [HttpGet("Factura/{ordenId:int}")]
    public async Task<IActionResult> Factura(int ordenId)
    {
        var resultado = await _ordenLN.ObtenerFacturaAsync(ordenId, UsuarioIdActual(), User.IsInRole("Administrador"));
        if (resultado.Data == null || !string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);

        // Se normaliza la ruta antes de leer el archivo para impedir accesos fuera de la carpeta pública esperada.
        var raizWeb = Path.GetFullPath(_entorno.WebRootPath ?? Path.Combine(_entorno.ContentRootPath, "wwwroot"));
        var ruta = Path.GetFullPath(Path.Combine(raizWeb, resultado.Data.RutaRelativa.Replace('/', Path.DirectorySeparatorChar)));
        var prefijoSeguro = raizWeb.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!ruta.StartsWith(prefijoSeguro, StringComparison.OrdinalIgnoreCase)) return Forbid();
        if (!System.IO.File.Exists(ruta)) return NotFound();
        return PhysicalFile(ruta, "application/pdf", $"Factura-{resultado.Data.NumeroFactura}.pdf");
    }

    /// <summary>Cancela una orden pendiente que pertenece al Cliente autenticado.</summary>
    [Authorize(Roles = "Cliente")]
    [HttpPut("Cancelar/{ordenId:int}")]
    public async Task<IActionResult> Cancelar(int ordenId)
    {
        var resultado = await _ordenLN.CancelarPendienteAsync(ordenId, UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _ordenLN.ListarAsync();
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("Obtener/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(int id)
    {
        var resultado = await _ordenLN.ObtenerAsync(new TOrden { OrdenId = id });
        if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
        return Ok(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("Buscar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Buscar(string estado)
    {
        var resultado = await _ordenLN.BuscarAsync(new TOrden { Estado = estado });
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("Insertar")]
    public async Task<IActionResult> Insertar([FromBody] TOrden orden)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _ordenLN.InsertarAsync(orden);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("Modificar")]
    public async Task<IActionResult> Modificar([FromBody] TOrden orden)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _ordenLN.ModificarAsync(orden);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("Eliminar/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await _ordenLN.EliminarAsync(new TOrden { OrdenId = id });
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // El UsuarioId del JWT delimita todas las consultas de propiedad realizadas por la LN.
    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
