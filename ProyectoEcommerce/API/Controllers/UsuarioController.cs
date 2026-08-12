using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    /// <summary>
    /// Gestiona registro, autenticación JWT y administración de usuarios y roles.
    /// </summary>
    public class UsuarioController : ControllerBase
    {
        private IUsuarioLN _usuarioLN { get; }
        private IConfiguration _configuration { get; }

        public UsuarioController(IUsuarioLN usuarioLN, IConfiguration configuration)
        {
            _usuarioLN = usuarioLN;
            _configuration = configuration;
        }

        /// <summary>Registra una cuenta de Cliente aplicando las validaciones de la lógica de negocio.</summary>
        [AllowAnonymous]
        [HttpPost("Registrar")]
        public async Task<IActionResult> Registrar([FromBody] TRegistroUsuario registro)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var resultado = await _usuarioLN.RegistrarAsync(registro);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>Indica si LessPrice aún necesita crear su primer Administrador.</summary>
        [AllowAnonymous]
        [HttpGet("/api/auth/setup-status")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> EstadoConfiguracionInicial()
        {
            var resultado = await _usuarioLN.RequiereConfiguracionInicialAsync();
            if (!string.IsNullOrEmpty(resultado.Error))
                return StatusCode(StatusCodes.Status500InternalServerError, resultado);

            return Ok(new TEstadoConfiguracionInicial
            {
                RequiereConfiguracionInicial = resultado.Data,
                CorreoAdministradorInicial =
                    (_configuration["InitialAdmin:Email"] ?? string.Empty).Trim().ToLowerInvariant()
            });
        }

        /// <summary>Crea una sola vez el Administrador inicial autorizado.</summary>
        [AllowAnonymous]
        [HttpPost("/api/auth/setup-admin")]
        public async Task<IActionResult> CrearAdministradorInicial([FromBody] TRegistroUsuario registro)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var resultado = await _usuarioLN.CrearAdministradorInicialAsync(registro);
            if (resultado.Error == Mensajes.ConfiguracionInicialNoDisponible)
                return Conflict(resultado);
            if (!string.IsNullOrEmpty(resultado.Error))
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>Valida las credenciales y, si son correctas, entrega una sesión JWT firmada.</summary>
        [AllowAnonymous]
        [HttpPost("IniciarSesion")]
        public async Task<IActionResult> IniciarSesion([FromBody] TLoginUsuario login)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var resultado = await _usuarioLN.AutenticarAsync(login);
            if (!string.IsNullOrEmpty(resultado.Error) || resultado.Data == null)
            {
                if (resultado.Error == Mensajes.AccesoBloqueado)
                {
                    return StatusCode(StatusCodes.Status423Locked, resultado);
                }
                if (resultado.Error == Mensajes.CredencialesIncorrectas)
                {
                    return Unauthorized(resultado);
                }

                return StatusCode(StatusCodes.Status500InternalServerError, resultado);
            }

            var usuario = resultado.Data.Usuario;
            if (usuario == null) return StatusCode(StatusCodes.Status500InternalServerError, resultado);

            // La duración proviene de configuración; el token no modifica el estado del usuario en la base de datos.
            var expiracion = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int?>("Jwt:ExpiresMinutes") ?? 60);

            var respuesta = new Respuesta<TAutenticacionRespuesta>
            {
                Data = new TAutenticacionRespuesta
                {
                    Token = GenerarToken(usuario, expiracion),
                    Expira = expiracion,
                    Usuario = usuario
                }
            };

            return Ok(respuesta);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("Listar")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Listar()
        {
            var resultado = await _usuarioLN.ListarAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("ListarAdministracion")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarAdministracion([FromQuery] TFiltroUsuarios filtro)
        {
            var resultado = await _usuarioLN.ListarAdministracionAsync(filtro);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("ListarRoles")]
        public async Task<IActionResult> ListarRoles()
        {
            var resultado = await _usuarioLN.ListarRolesAsync();
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CambiarRol")]
        public async Task<IActionResult> CambiarRol([FromBody] TCambioRolUsuario datos)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var resultado = await _usuarioLN.CambiarRolAsync(datos, UsuarioIdActual());
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CambiarEstado")]
        public async Task<IActionResult> CambiarEstado([FromBody] TCambioEstadoUsuario datos)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var resultado = await _usuarioLN.CambiarEstadoAsync(datos, UsuarioIdActual());
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("Obtener/{id}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Obtener(int id)
        {
            var resultado = await _usuarioLN.ObtenerAsync(new TUsuario { UsuarioId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return NotFound(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("Buscar")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Buscar(string texto)
        {
            var resultado = await _usuarioLN.BuscarAsync(new TUsuario { Nombre = texto });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost("Insertar")]
        public async Task<IActionResult> Insertar([FromBody] TUsuario usuario)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _usuarioLN.InsertarAsync(usuario);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("Modificar")]
        public async Task<IActionResult> Modificar([FromBody] TUsuario usuario)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var resultado = await _usuarioLN.ModificarAsync(usuario);
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpDelete("Eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _usuarioLN.EliminarAsync(new TUsuario { UsuarioId = id });
            if (!string.IsNullOrEmpty(resultado.Error)) return BadRequest(resultado);
            return Ok(resultado);
        }

        // Las operaciones administrativas registran quién las realizó mediante el identificador incluido en el JWT.
        private int UsuarioIdActual() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        /// <summary>
        /// Genera un JWT con identificador, correo, nombre y rol del usuario autenticado.
        /// La clave de firma se obtiene de la configuración y no se expone en la respuesta.
        /// </summary>
        private string GenerarToken(TUsuario usuario, DateTime expiracion)
        {
            var clave = _configuration["Jwt:Key"]!;
            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellidos}".Trim()),
                new Claim(ClaimTypes.Role, usuario.RolNombre),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiracion,
                signingCredentials: credenciales);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
