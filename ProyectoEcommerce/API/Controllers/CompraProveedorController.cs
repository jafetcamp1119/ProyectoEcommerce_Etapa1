using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers;

[Authorize(Roles = "Administrador")]
[Route("api/[controller]")]
[ApiController]
public class CompraProveedorController : ControllerBase
{
    private readonly ICompraProveedorLN _compraProveedorLN;
    private readonly IFacturaLN _facturaLN;
    private readonly IWebHostEnvironment _environment;

    public CompraProveedorController(
        ICompraProveedorLN compraProveedorLN,
        IFacturaLN facturaLN,
        IWebHostEnvironment environment)
    {
        _compraProveedorLN = compraProveedorLN;
        _facturaLN = facturaLN;
        _environment = environment;
    }

    [HttpPost("Proforma")]
    public async Task<IActionResult> Proforma([FromBody] TSolicitudCompraProveedor datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _compraProveedorLN.PrepararProformaAsync(datos);
        if (resultado.Data == null || !string.IsNullOrEmpty(resultado.Error))
            return BadRequest(resultado);
        var pdf = _facturaLN.GenerarCompraProveedor(resultado.Data, true);
        return File(pdf, "application/pdf", $"Proforma-{resultado.Data.ProveedorId}-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    [HttpPost("EnviarProforma")]
    public async Task<IActionResult> EnviarProforma(
        [FromBody] TSolicitudCompraProveedor datos,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _compraProveedorLN.EnviarProformaAsync(datos, cancellationToken);
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPost("Confirmar")]
    public async Task<IActionResult> Confirmar(
        [FromBody] TSolicitudCompraProveedor datos,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _compraProveedorLN.ConfirmarAsync(
            datos,
            UsuarioIdActual(),
            cancellationToken);
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpGet("Historial")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Historial([FromQuery] TFiltroComprasProveedor filtro)
    {
        var resultado = await _compraProveedorLN.ListarAsync(filtro);
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpGet("Detalle/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Detalle(int id)
    {
        var resultado = await _compraProveedorLN.ObtenerDetalleAsync(id);
        return !string.IsNullOrEmpty(resultado.Error) ? NotFound(resultado) : Ok(resultado);
    }

    [HttpGet("Pdf/{id:int}")]
    public async Task<IActionResult> Pdf(int id)
    {
        var resultado = await _compraProveedorLN.ObtenerPdfAsync(id);
        if (resultado.Data == null || !string.IsNullOrEmpty(resultado.Error))
            return NotFound(resultado);

        var raiz = Path.GetFullPath(RaizWeb());
        var ruta = Path.GetFullPath(Path.Combine(raiz, resultado.Data.RutaRelativa));
        if (!ruta.StartsWith(raiz, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(ruta))
            return NotFound(new { success = false, error = "El archivo PDF no está disponible." });
        return PhysicalFile(ruta, "application/pdf", $"Compra-{resultado.Data.Numero}.pdf");
    }

    private string RaizWeb() => _environment.WebRootPath ??
        Path.Combine(_environment.ContentRootPath, "wwwroot");

    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
