using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
// coordina checkout, compra, consultas de ordenes y descarga de facturas
// la LN mantiene la logica pesada y aqui se acomodan las respuestas HTTP
public class OrdenController : ControllerBase
{
    private readonly IOrdenLN _ordenLN;
    private readonly IWebHostEnvironment _entorno;

    public OrdenController(IOrdenLN ordenLN, IWebHostEnvironment entorno)
    {
        _ordenLN = ordenLN;
        _entorno = entorno;
    }

    // pide a la LN el Cliente, carrito y totales que se muestran antes de confirmar
    [Authorize(Roles = "Cliente")]
    [HttpGet("Checkout")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Checkout()
    {
        var resultado = await _ordenLN.PrepararCheckoutAsync(UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // recibe correo, direccion y metodo de pago y manda todo a la transaccion de compra
    // CancellationToken deja cancelar el trabajo si la solicitud HTTP se corta
    [Authorize(Roles = "Cliente")]
    [HttpPost("ConfirmarCompra")]
    public async Task<IActionResult> ConfirmarCompra([FromBody] TConfirmarCompra datos, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _ordenLN.ConfirmarCompraAsync(datos, UsuarioIdActual(), cancellationToken);
        // si el stock cambio durante el checkout devuelve 409 para que Angular avise el conflicto
        if (resultado.Error.Contains("stock suficiente", StringComparison.OrdinalIgnoreCase)) return Conflict(resultado);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // lista solamente las ordenes del Cliente que viene en el JWT
    [Authorize(Roles = "Cliente")]
    [HttpGet("MisOrdenes")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> MisOrdenes([FromQuery] TFiltroOrdenes filtro)
    {
        var resultado = await _ordenLN.ListarClienteAsync(filtro, UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // para el Administrador lista ordenes de todos los clientes
    [Authorize(Roles = "Administrador")]
    [HttpGet("Administracion")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Administracion([FromQuery] TFiltroOrdenes filtro)
    {
        var resultado = await _ordenLN.ListarAdministracionAsync(filtro);
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // manda a la LN el ID, el usuario actual y si tiene rol Administrador para revisar el permiso
    [HttpGet("Detalle/{ordenId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Detalle(int ordenId)
    {
        var resultado = await _ordenLN.ObtenerDetalleAsync(ordenId, UsuarioIdActual(), User.IsInRole("Administrador"));
        if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
        return Ok(resultado);
    }

    // busca la factura autorizada y devuelve el PDF como archivo descargable
    [HttpGet("Factura/{ordenId:int}")]
    public async Task<IActionResult> Factura(int ordenId)
    {
        var resultado = await _ordenLN.ObtenerFacturaAsync(ordenId, UsuarioIdActual(), User.IsInRole("Administrador"));
        if (resultado.Data == null || !string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);

        // GetFullPath acomoda la ruta completa para comprobar que siga dentro de wwwroot
        var raizWeb = Path.GetFullPath(_entorno.WebRootPath ?? Path.Combine(_entorno.ContentRootPath, "wwwroot"));
        var ruta = Path.GetFullPath(Path.Combine(raizWeb, resultado.Data.RutaRelativa.Replace('/', Path.DirectorySeparatorChar)));
        var prefijoSeguro = raizWeb.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        // si la ruta intenta salir de la carpeta permitida devuelve 403 y no lee el archivo
        if (!ruta.StartsWith(prefijoSeguro, StringComparison.OrdinalIgnoreCase)) return Forbid();
        if (!System.IO.File.Exists(ruta)) return NotFound();
        return PhysicalFile(ruta, "application/pdf", $"Factura-{resultado.Data.NumeroFactura}.pdf");
    }

    // cancela una orden propia solo cuando todavia esta pendiente
    [Authorize(Roles = "Cliente")]
    [HttpPut("Cancelar/{ordenId:int}")]
    public async Task<IActionResult> Cancelar(int ordenId)
    {
        var resultado = await _ordenLN.CancelarPendienteAsync(ordenId, UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
        return Ok(resultado);
    }

    // los siguientes endpoints conservan las operaciones administrativas de la arquitectura original
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

    // agarra el UsuarioId del Claim para que la LN limite las consultas al dueño real
    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
