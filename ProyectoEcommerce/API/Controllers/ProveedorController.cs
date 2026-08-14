using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.API.Controllers;

[Authorize(Roles = "Administrador")]
[Route("api/[controller]")]
[ApiController]
public class ProveedorController : ControllerBase
{
    private readonly IProveedorLN _proveedorLN;
    private readonly IWebHostEnvironment _environment;

    public ProveedorController(IProveedorLN proveedorLN, IWebHostEnvironment environment)
    {
        _proveedorLN = proveedorLN;
        _environment = environment;
    }

    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar([FromQuery] bool soloActivos = false)
    {
        var resultado = await _proveedorLN.ListarAsync(soloActivos);
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpGet("Obtener/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(int id)
    {
        var resultado = await _proveedorLN.ObtenerAsync(id);
        return !string.IsNullOrEmpty(resultado.Error) ? NotFound(resultado) : Ok(resultado);
    }

    [HttpPost("Insertar")]
    public async Task<IActionResult> Insertar([FromBody] TProveedor proveedor)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _proveedorLN.InsertarAsync(proveedor, UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPut("Modificar")]
    public async Task<IActionResult> Modificar([FromBody] TProveedor proveedor)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _proveedorLN.ModificarAsync(proveedor, UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPut("CambiarEstado/{id:int}")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] TCambioEstadoProveedor cambio)
    {
        var resultado = await _proveedorLN.CambiarEstadoAsync(id, cambio.Activo, UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpGet("{proveedorId:int}/Familias")]
    public async Task<IActionResult> Familias(int proveedorId, [FromQuery] bool soloDisponibles = false)
    {
        var resultado = await _proveedorLN.ListarFamiliasAsync(proveedorId, soloDisponibles);
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpGet("{proveedorId:int}/Categorias")]
    public async Task<IActionResult> Categorias(
        int proveedorId,
        [FromQuery] int? familiaId,
        [FromQuery] bool soloDisponibles = false)
    {
        var resultado = await _proveedorLN.ListarCategoriasAsync(
            proveedorId,
            familiaId,
            soloDisponibles);
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpGet("{proveedorId:int}/Productos")]
    public async Task<IActionResult> Productos(
        int proveedorId,
        [FromQuery] int? categoriaId,
        [FromQuery] bool soloDisponibles = false)
    {
        var resultado = await _proveedorLN.ListarProductosAsync(
            proveedorId,
            categoriaId,
            soloDisponibles);
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpGet("{proveedorId:int}/Categorias/{categoriaId:int}/ProductosExistentes")]
    public async Task<IActionResult> ProductosExistentes(int proveedorId, int categoriaId)
    {
        var resultado = await _proveedorLN.ListarProductosExistentesAsync(
            proveedorId,
            categoriaId);
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPost("{proveedorId:int}/Categorias/Existente/{categoriaId:int}")]
    public async Task<IActionResult> AsociarCategoriaExistente(int proveedorId, int categoriaId)
    {
        var resultado = await _proveedorLN.AsociarCategoriaExistenteAsync(
            proveedorId,
            categoriaId,
            UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPost("{proveedorId:int}/Categorias/Nueva")]
    public async Task<IActionResult> CrearCategoria(
        int proveedorId,
        [FromBody] TCategoriaNuevaProveedor datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _proveedorLN.CrearCategoriaAsync(
            proveedorId,
            datos,
            UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPost("{proveedorId:int}/Categorias/{categoriaId:int}/Productos/Existente")]
    public async Task<IActionResult> AgregarProductoExistente(
        int proveedorId,
        int categoriaId,
        [FromBody] TAgregarProductoExistenteProveedor datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _proveedorLN.AgregarProductoExistenteAsync(
            proveedorId,
            categoriaId,
            datos,
            UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPost("{proveedorId:int}/Categorias/{categoriaId:int}/Productos/Nuevo")]
    public async Task<IActionResult> CrearProducto(
        int proveedorId,
        int categoriaId,
        [FromBody] TCrearProductoProveedor datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _proveedorLN.CrearProductoAsync(
            proveedorId,
            categoriaId,
            datos,
            UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPut("Productos/{ofertaId:int}")]
    public async Task<IActionResult> ModificarProducto(
        int ofertaId,
        [FromBody] TModificarProductoProveedor datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _proveedorLN.ModificarProductoAsync(
            ofertaId,
            datos,
            UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPost("{proveedorId:int}/Familias/{familiaId:int}/Incorporar")]
    public async Task<IActionResult> IncorporarFamilia(int proveedorId, int familiaId)
    {
        var resultado = await _proveedorLN.IncorporarFamiliaAsync(
            proveedorId,
            familiaId,
            UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPost("{proveedorId:int}/Categorias/{categoriaId:int}/Incorporar")]
    public async Task<IActionResult> IncorporarCategoria(int proveedorId, int categoriaId)
    {
        var resultado = await _proveedorLN.IncorporarCategoriaAsync(
            proveedorId,
            categoriaId,
            UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    [HttpPost("Productos/{ofertaId:int}/Incorporar")]
    public async Task<IActionResult> IncorporarProducto(
        int ofertaId,
        [FromBody] TIncorporarProductoProveedor datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var resultado = await _proveedorLN.IncorporarProductoAsync(
            ofertaId,
            datos,
            UsuarioIdActual());
        return !string.IsNullOrEmpty(resultado.Error) ? BadRequest(resultado) : Ok(resultado);
    }

    // guarda la imagen en wwwroot/proveedores/{id}, igual que familias y categorías
    [HttpPost("SubirImagen/{proveedorId:int}")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> SubirImagen(int proveedorId, IFormFile archivo)
    {
        if (proveedorId <= 0 || archivo == null || archivo.Length == 0)
            return BadRequest("Debe seleccionar una imagen.");
        if (archivo.Length > 5 * 1024 * 1024)
            return BadRequest("La imagen supera los 5 MB.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
            return BadRequest("El archivo seleccionado no es una imagen permitida.");

        var actual = await _proveedorLN.ObtenerAsync(proveedorId);
        if (actual.Data == null) return NotFound("El proveedor no existe.");
        var urlAnterior = actual.Data.UrlImagen;
        var carpeta = Path.Combine(RaizWeb(), "proveedores", proveedorId.ToString());
        Directory.CreateDirectory(carpeta);
        var nombre = $"{Guid.NewGuid():N}{extension}";
        var ruta = Path.Combine(carpeta, nombre);

        await using (var stream = new FileStream(ruta, FileMode.Create))
            await archivo.CopyToAsync(stream);

        actual.Data.UrlImagen = $"{Request.Scheme}://{Request.Host}/proveedores/{proveedorId}/{nombre}";
        var resultado = await _proveedorLN.ModificarAsync(actual.Data, UsuarioIdActual());
        if (!string.IsNullOrEmpty(resultado.Error))
        {
            System.IO.File.Delete(ruta);
            return BadRequest(resultado);
        }

        EliminarImagenAnterior(proveedorId, urlAnterior);
        return Ok(resultado);
    }

    private void EliminarImagenAnterior(int proveedorId, string? urlAnterior)
    {
        if (string.IsNullOrWhiteSpace(urlAnterior) ||
            !urlAnterior.Replace('\\', '/').Contains($"/proveedores/{proveedorId}/"))
            return;

        var nombre = Uri.TryCreate(urlAnterior, UriKind.Absolute, out var uri)
            ? Path.GetFileName(uri.LocalPath)
            : Path.GetFileName(urlAnterior);
        var ruta = Path.Combine(RaizWeb(), "proveedores", proveedorId.ToString(), nombre);
        if (System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);
    }

    private string RaizWeb() => _environment.WebRootPath ??
        Path.Combine(_environment.ContentRootPath, "wwwroot");

    private int UsuarioIdActual() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
