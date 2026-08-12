using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

/// <summary>
/// Aplica las reglas de registro, autenticación, bloqueo y administración de usuarios.
/// </summary>
public class UsuarioLN : IUsuarioLN
{
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;
    private readonly ILogger<UsuarioLN> _logger;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly int _maxIntentos;
    private readonly int _minutosBloqueo;
    private readonly string _correoAdministradorInicial;

    public UsuarioLN(IUnidadTrabajoEF unidadTrabajo, ILogger<UsuarioLN> logger, IMapper mapper,
        IPasswordHasher<Usuario> passwordHasher, IConfiguration configuration)
    {
        _unidadDeTrabajo = unidadTrabajo;
        _logger = logger;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _maxIntentos = Math.Max(1, configuration.GetValue<int?>("Seguridad:MaxIntentosFallidos") ?? 3);
        _minutosBloqueo = Math.Max(1, configuration.GetValue<int?>("Seguridad:BloqueoMinutos") ?? 15);
        _correoAdministradorInicial = NormalizarCorreo(configuration["InitialAdmin:Email"]);
    }

    /// <summary>Registra un Cliente con correo único y contraseña almacenada como hash.</summary>
    public async Task<Respuesta<TUsuario>> RegistrarAsync(TRegistroUsuario datos)
    {
        try
        {
            LimpiarRegistro(datos);
            var correo = NormalizarCorreo(datos.Correo);
            // El correo principal no puede ser ocupado como Cliente antes de completar el setup.
            if (string.Equals(correo, _correoAdministradorInicial, StringComparison.Ordinal))
                return Error<TUsuario>(Mensajes.CorreoReservadoConfiguracionInicial);

            var existente = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.Correo == correo);
            if (!string.IsNullOrEmpty(existente.Error)) return Error<TUsuario>(Mensajes.ErrorRegistro);
            if (existente.Data != null) return Error<TUsuario>(Mensajes.CorreoDuplicado);

            var rolCliente = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(x => x.Nombre == "Cliente" && x.Activo);
            if (rolCliente.Data == null || !string.IsNullOrEmpty(rolCliente.Error)) return Error<TUsuario>(Mensajes.ErrorRegistro);

            var entidad = new Usuario
            {
                RolId = rolCliente.Data.RolId,
                Nombre = datos.Nombre,
                Apellidos = datos.Apellidos,
                Correo = correo,
                Telefono = datos.Telefono,
                Direccion = null,
                Activo = true,
                FechaRegistro = DateTime.UtcNow,
                IntentosFallidos = 0
            };
            // La contraseña original nunca se persiste; únicamente se guarda el hash producido por Identity.
            entidad.PasswordHash = _passwordHasher.HashPassword(entidad, datos.Contrasena);
            var insercion = await _unidadDeTrabajo.TUsuario.InsertarAsync(entidad);
            if (insercion.Data == null || !string.IsNullOrEmpty(insercion.Error))
            {
                if (insercion.Error.Contains("UQ_Usuarios_Correo", StringComparison.OrdinalIgnoreCase))
                    return Error<TUsuario>(Mensajes.CorreoDuplicado);
                return Error<TUsuario>(Mensajes.ErrorRegistro);
            }
            return new Respuesta<TUsuario> { Data = MapearUsuario(insercion.Data, "Cliente") };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar la cuenta para {Correo}", NormalizarCorreo(datos.Correo));
            return Error<TUsuario>(Mensajes.ErrorRegistro);
        }
    }

    /// <summary>Consulta en la base si existe al menos un Administrador activo.</summary>
    public async Task<Respuesta<bool>> RequiereConfiguracionInicialAsync()
    {
        try
        {
            var rolAdministrador = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(
                x => x.Nombre == "Administrador" && x.Activo);
            if (!string.IsNullOrEmpty(rolAdministrador.Error) || rolAdministrador.Data == null)
                return Error<bool>(Mensajes.ErrorConfiguracionInicial);

            var administradores = await _unidadDeTrabajo.TUsuario.ContarAsync(
                x => x.Activo && x.RolId == rolAdministrador.Data.RolId);
            if (!string.IsNullOrEmpty(administradores.Error) || administradores.Data == null)
                return Error<bool>(Mensajes.ErrorConfiguracionInicial);

            return new Respuesta<bool> { Data = administradores.Data.Value == 0 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar el estado de configuración inicial.");
            return Error<bool>(Mensajes.ErrorConfiguracionInicial);
        }
    }

    /// <summary>
    /// Crea el primer Administrador con el mismo hasher del registro público.
    /// Serializable mantiene bloqueado el rango consultado hasta confirmar la inserción.
    /// </summary>
    public async Task<Respuesta<TUsuario>> CrearAdministradorInicialAsync(TRegistroUsuario datos)
    {
        LimpiarRegistro(datos);
        var correo = NormalizarCorreo(datos.Correo);

        if (string.IsNullOrWhiteSpace(_correoAdministradorInicial) ||
            !string.Equals(correo, _correoAdministradorInicial, StringComparison.Ordinal))
            return Error<TUsuario>(Mensajes.CorreoAdministradorInicialNoAutorizado);

        try
        {
            _unidadDeTrabajo.EmpezarTransaccion(IsolationLevel.Serializable);

            var rolAdministrador = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(
                x => x.Nombre == "Administrador" && x.Activo);
            if (!string.IsNullOrEmpty(rolAdministrador.Error) || rolAdministrador.Data == null)
            {
                _unidadDeTrabajo.Rollback();
                return Error<TUsuario>(Mensajes.ErrorConfiguracionInicial);
            }

            var administradores = await _unidadDeTrabajo.TUsuario.ContarAsync(
                x => x.Activo && x.RolId == rolAdministrador.Data.RolId);
            if (!string.IsNullOrEmpty(administradores.Error) || administradores.Data == null)
            {
                _unidadDeTrabajo.Rollback();
                return Error<TUsuario>(Mensajes.ErrorConfiguracionInicial);
            }
            if (administradores.Data.Value > 0)
            {
                _unidadDeTrabajo.Rollback();
                return Error<TUsuario>(Mensajes.ConfiguracionInicialNoDisponible);
            }

            var existente = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.Correo == correo);
            if (!string.IsNullOrEmpty(existente.Error))
            {
                _unidadDeTrabajo.Rollback();
                return Error<TUsuario>(Mensajes.ErrorConfiguracionInicial);
            }
            if (existente.Data != null)
            {
                _unidadDeTrabajo.Rollback();
                return Error<TUsuario>(Mensajes.CorreoDuplicado);
            }

            var entidad = new Usuario
            {
                RolId = rolAdministrador.Data.RolId,
                Nombre = datos.Nombre,
                Apellidos = datos.Apellidos,
                Correo = correo,
                Telefono = datos.Telefono,
                Direccion = null,
                Activo = true,
                FechaRegistro = DateTime.UtcNow,
                IntentosFallidos = 0
            };
            entidad.PasswordHash = _passwordHasher.HashPassword(entidad, datos.Contrasena);

            var insercion = await _unidadDeTrabajo.TUsuario.InsertarAsync(entidad);
            if (insercion.Data == null || !string.IsNullOrEmpty(insercion.Error))
            {
                _unidadDeTrabajo.Rollback();
                return Error<TUsuario>(
                    insercion.Error.Contains("UQ_Usuarios_Correo", StringComparison.OrdinalIgnoreCase)
                        ? Mensajes.CorreoDuplicado
                        : Mensajes.ErrorConfiguracionInicial);
            }

            _unidadDeTrabajo.CompletarTran();
            return new Respuesta<TUsuario>
            {
                Data = MapearUsuario(insercion.Data, rolAdministrador.Data.Nombre)
            };
        }
        catch (Exception ex)
        {
            _unidadDeTrabajo.Rollback();
            _logger.LogError(ex, "Error al crear el Administrador inicial para {Correo}", correo);
            return Error<TUsuario>(Mensajes.ErrorConfiguracionInicial);
        }
    }

    /// <summary>Valida credenciales y controla intentos fallidos antes de permitir generar el JWT.</summary>
    public async Task<Respuesta<TEstadoAutenticacion>> AutenticarAsync(TLoginUsuario datos)
    {
        var correo = NormalizarCorreo(datos.Correo);
        try
        {
            var respuesta = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                x => x.Correo == correo,
                ["Rol.RolMenuOpciones.MenuOpcion"]);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TEstadoAutenticacion>(Mensajes.ErrorAutenticacion);

            var usuario = respuesta.Data;
            if (usuario == null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.PasswordHash))
            {
                await RegistrarAccesoAsync(usuario?.UsuarioId, correo, false);
                return Error<TEstadoAutenticacion>(Mensajes.CredencialesIncorrectas);
            }

            var ahora = DateTime.UtcNow;
            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > ahora)
            {
                await RegistrarAccesoAsync(usuario.UsuarioId, correo, false);
                return Bloqueado(usuario.BloqueadoHasta.Value, ahora);
            }

            if (usuario.BloqueadoHasta.HasValue)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
                usuario.UltimoIntentoFallido = null;
            }

            var verificacion = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, datos.Contrasena);
            // Los intentos fallidos consecutivos bloquean temporalmente la cuenta según configuración.
            if (verificacion == PasswordVerificationResult.Failed)
            {
                usuario.IntentosFallidos++;
                usuario.UltimoIntentoFallido = ahora;
                if (usuario.IntentosFallidos >= _maxIntentos)
                    usuario.BloqueadoHasta = ahora.AddMinutes(_minutosBloqueo);

                await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario);
                await RegistrarAccesoAsync(usuario.UsuarioId, correo, false);
                return usuario.BloqueadoHasta.HasValue
                    ? Bloqueado(usuario.BloqueadoHasta.Value, ahora)
                    : Error<TEstadoAutenticacion>(Mensajes.CredencialesIncorrectas);
            }

            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            usuario.UltimoIntentoFallido = null;
            if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
                usuario.PasswordHash = _passwordHasher.HashPassword(usuario, datos.Contrasena);
            await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario);
            await RegistrarAccesoAsync(usuario.UsuarioId, correo, true);

            return new Respuesta<TEstadoAutenticacion>
            {
                Data = new TEstadoAutenticacion { Usuario = MapearUsuario(usuario, incluirMenu: true) }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar sesión para {Correo}", correo);
            return Error<TEstadoAutenticacion>(Mensajes.ErrorAutenticacion);
        }
    }

    /// <summary>Lista usuarios con filtros de texto, rol, estado y paginación.</summary>
    public async Task<Respuesta<TPagina<TUsuario>>> ListarAdministracionAsync(TFiltroUsuarios filtro)
    {
        try
        {
            var texto = (filtro.Texto ?? string.Empty).Trim();
            var tamano = new[] { 25, 50, 75, 100 }.Contains(filtro.TamanoPagina) ? filtro.TamanoPagina : 25;
            var pagina = Math.Max(1, filtro.Pagina);
            var respuesta = await _unidadDeTrabajo.TUsuario.BuscarAsync(x =>
                (texto == "" || x.Nombre.Contains(texto) || x.Apellidos.Contains(texto) || x.Correo.Contains(texto)) &&
                (!filtro.RolId.HasValue || x.RolId == filtro.RolId.Value) &&
                (!filtro.Activo.HasValue || x.Activo == filtro.Activo.Value), ["Rol"]);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TPagina<TUsuario>>(Mensajes.ErrorOperacion);
            var consulta = (respuesta.Data ?? []).OrderBy(x => x.Nombre).ThenBy(x => x.Apellidos).ToList();
            return new Respuesta<TPagina<TUsuario>>
            {
                Data = new TPagina<TUsuario>
                {
                    Items = consulta.Skip((pagina - 1) * tamano).Take(tamano).Select(x => MapearUsuario(x)),
                    Pagina = pagina,
                    TamanoPagina = tamano,
                    Total = consulta.Count
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar usuarios para administración.");
            return Error<TPagina<TUsuario>>(Mensajes.ErrorOperacion);
        }
    }

    public async Task<Respuesta<IEnumerable<TRol>>> ListarRolesAsync()
    {
        var respuesta = await _unidadDeTrabajo.TRol.BuscarAsync(x => x.Activo && (x.Nombre == "Administrador" || x.Nombre == "Cliente"));
        if (!string.IsNullOrEmpty(respuesta.Error)) return Error<IEnumerable<TRol>>(Mensajes.ErrorOperacion);
        return new Respuesta<IEnumerable<TRol>>
        {
            Data = (respuesta.Data ?? []).OrderBy(x => x.Nombre).Select(x => new TRol { RolId = x.RolId, Nombre = x.Nombre })
        };
    }

    /// <summary>Cambia un rol sin permitir que el sistema quede sin Administradores activos.</summary>
    public async Task<Respuesta<TUsuario>> CambiarRolAsync(TCambioRolUsuario datos, int administradorId)
    {
        try
        {
            var usuario = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.UsuarioId == datos.UsuarioId, ["Rol"]);
            if (usuario.Data == null) return Error<TUsuario>(Mensajes.RegistroNoEncontrado);
            var rol = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(x => x.RolId == datos.RolId && x.Activo && (x.Nombre == "Administrador" || x.Nombre == "Cliente"));
            if (rol.Data == null) return Error<TUsuario>(Mensajes.RolNoEncontrado);

            if (usuario.Data.Activo && usuario.Data.Rol.Nombre == "Administrador" && rol.Data.Nombre != "Administrador" && !await HayOtroAdministradorActivo(usuario.Data.UsuarioId))
                return Error<TUsuario>(Mensajes.UltimoAdministrador);

            var rolAnterior = usuario.Data.Rol.Nombre;
            usuario.Data.RolId = rol.Data.RolId;
            usuario.Data.Rol = rol.Data;
            var actualizacion = await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario.Data);
            if (actualizacion.Data == null || !string.IsNullOrEmpty(actualizacion.Error)) return Error<TUsuario>(Mensajes.ErrorOperacion);
            await RegistrarBitacora(administradorId, "CAMBIO_ROL", usuario.Data.UsuarioId, $"Rol: {rolAnterior} -> {rol.Data.Nombre}");
            return new Respuesta<TUsuario> { Data = MapearUsuario(usuario.Data, rol.Data.Nombre) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar el rol de UsuarioId {UsuarioId}", datos.UsuarioId);
            return Error<TUsuario>(Mensajes.ErrorOperacion);
        }
    }

    /// <summary>Activa o desactiva una cuenta conservando la protección del último Administrador.</summary>
    public async Task<Respuesta<TUsuario>> CambiarEstadoAsync(TCambioEstadoUsuario datos, int administradorId)
    {
        try
        {
            var usuario = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.UsuarioId == datos.UsuarioId, ["Rol"]);
            if (usuario.Data == null) return Error<TUsuario>(Mensajes.RegistroNoEncontrado);
            if (!datos.Activo && usuario.Data.Activo && usuario.Data.Rol.Nombre == "Administrador" && !await HayOtroAdministradorActivo(usuario.Data.UsuarioId))
                return Error<TUsuario>(Mensajes.UltimoAdministrador);

            usuario.Data.Activo = datos.Activo;
            var actualizacion = await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario.Data);
            if (actualizacion.Data == null || !string.IsNullOrEmpty(actualizacion.Error)) return Error<TUsuario>(Mensajes.ErrorOperacion);
            await RegistrarBitacora(administradorId, datos.Activo ? "ACTIVAR_USUARIO" : "DESACTIVAR_USUARIO", usuario.Data.UsuarioId, null);
            return new Respuesta<TUsuario> { Data = MapearUsuario(usuario.Data) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar el estado de UsuarioId {UsuarioId}", datos.UsuarioId);
            return Error<TUsuario>(Mensajes.ErrorOperacion);
        }
    }

    public Task<Respuesta<TUsuario>> InsertarAsync(TUsuario datos) => Task.FromResult(Error<TUsuario>(Mensajes.ErrorOperacion));

    public async Task<Respuesta<IEnumerable<TUsuario>>> ListarAsync()
    {
        var pagina = await ListarAdministracionAsync(new TFiltroUsuarios { TamanoPagina = 100 });
        return string.IsNullOrEmpty(pagina.Error)
            ? new Respuesta<IEnumerable<TUsuario>> { Data = pagina.Data?.Items ?? [] }
            : Error<IEnumerable<TUsuario>>(pagina.Error);
    }

    public async Task<Respuesta<TUsuario>> ModificarAsync(TUsuario datos)
    {
        try
        {
            var actual = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.UsuarioId == datos.UsuarioId, ["Rol"]);
            if (actual.Data == null) return Error<TUsuario>(Mensajes.RegistroNoEncontrado);
            actual.Data.Nombre = datos.Nombre.Trim();
            actual.Data.Apellidos = datos.Apellidos.Trim();
            actual.Data.Telefono = datos.Telefono.Trim();
            actual.Data.Direccion = datos.Direccion?.Trim();
            var respuesta = await _unidadDeTrabajo.TUsuario.ModificarAsync(actual.Data);
            return respuesta.Data == null ? Error<TUsuario>(Mensajes.ErrorOperacion) : new Respuesta<TUsuario> { Data = MapearUsuario(actual.Data) };
        }
        catch { return Error<TUsuario>(Mensajes.ErrorOperacion); }
    }

    public async Task<Respuesta<bool>> EliminarAsync(TUsuario datos)
    {
        var cambio = await CambiarEstadoAsync(new TCambioEstadoUsuario { UsuarioId = datos.UsuarioId, Activo = false }, 0);
        return string.IsNullOrEmpty(cambio.Error) ? new Respuesta<bool> { Data = true } : Error<bool>(cambio.Error);
    }

    public async Task<Respuesta<IEnumerable<TUsuario>>> BuscarAsync(TUsuario datos)
    {
        var pagina = await ListarAdministracionAsync(new TFiltroUsuarios { Texto = datos.Nombre, TamanoPagina = 100 });
        return string.IsNullOrEmpty(pagina.Error)
            ? new Respuesta<IEnumerable<TUsuario>> { Data = pagina.Data?.Items ?? [] }
            : Error<IEnumerable<TUsuario>>(pagina.Error);
    }

    public async Task<Respuesta<TUsuario>> ObtenerAsync(TUsuario datos)
    {
        var respuesta = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.UsuarioId == datos.UsuarioId, ["Rol"]);
        return respuesta.Data == null ? Error<TUsuario>(Mensajes.RegistroNoEncontrado) : new Respuesta<TUsuario> { Data = MapearUsuario(respuesta.Data) };
    }

    private async Task<bool> HayOtroAdministradorActivo(int usuarioId)
    {
        var rol = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(x => x.Nombre == "Administrador");
        if (rol.Data == null) return false;
        var cantidad = await _unidadDeTrabajo.TUsuario.ContarAsync(x => x.Activo && x.RolId == rol.Data.RolId && x.UsuarioId != usuarioId);
        return (cantidad.Data ?? 0) > 0;
    }

    private async Task RegistrarBitacora(int usuarioId, string accion, int entidadId, string? detalle)
    {
        var respuesta = await _unidadDeTrabajo.TBitacoraSistema.InsertarAsync(new BitacoraSistema
        {
            UsuarioId = usuarioId > 0 ? usuarioId : null,
            Fecha = DateTime.UtcNow,
            Accion = accion,
            Entidad = "Usuario",
            EntidadId = entidadId.ToString(),
            Detalle = detalle
        });
        if (!string.IsNullOrEmpty(respuesta.Error)) _logger.LogWarning("No fue posible registrar la bitácora: {Error}", respuesta.Error);
    }

    /// <summary>Registra cada intento para conservar trazabilidad de acceso exitoso o fallido.</summary>
    private async Task RegistrarAccesoAsync(int? usuarioId, string correo, bool exitoso)
    {
        var respuesta = await _unidadDeTrabajo.THistorialAcceso.InsertarAsync(new HistorialAcceso
        {
            UsuarioId = usuarioId,
            CorreoIntentado = correo,
            Fecha = DateTime.UtcNow,
            Exitoso = exitoso
        });
        if (!string.IsNullOrEmpty(respuesta.Error)) _logger.LogWarning("No fue posible registrar el historial de acceso: {Error}", respuesta.Error);
    }

    private static Respuesta<TEstadoAutenticacion> Bloqueado(DateTime hasta, DateTime ahora) => new()
    {
        Success = false,
        Error = Mensajes.AccesoBloqueado,
        Data = new TEstadoAutenticacion
        {
            Bloqueado = true,
            BloqueadoHasta = hasta,
            SegundosRestantes = Math.Max(1, (int)Math.Ceiling((hasta - ahora).TotalSeconds))
        }
    };

    // La respuesta de sesión incluye solo las opciones de menú activas asignadas al rol.
    private static TUsuario MapearUsuario(Usuario usuario, string? rolNombre = null, bool incluirMenu = false) => new()
    {
        UsuarioId = usuario.UsuarioId,
        RolId = usuario.RolId,
        RolNombre = rolNombre ?? usuario.Rol?.Nombre ?? string.Empty,
        Nombre = usuario.Nombre,
        Apellidos = usuario.Apellidos,
        Correo = usuario.Correo,
        Telefono = usuario.Telefono,
        Direccion = usuario.Direccion,
        Activo = usuario.Activo,
        FechaRegistro = usuario.FechaRegistro,
        MenuOpciones = incluirMenu
            ? (usuario.Rol?.RolMenuOpciones ?? []).Where(x => x.MenuOpcion.Activo).OrderBy(x => x.MenuOpcion.Orden)
                .Select(x => new TMenuOpcion { MenuOpcionId = x.MenuOpcionId, Nombre = x.MenuOpcion.Nombre, Ruta = x.MenuOpcion.Ruta, Icono = x.MenuOpcion.Icono, Orden = x.MenuOpcion.Orden }).ToList()
            : []
    };

    private static string NormalizarCorreo(string? correo) => (correo ?? string.Empty).Trim().ToLowerInvariant();
    private static void LimpiarRegistro(TRegistroUsuario datos)
    {
        datos.Nombre = datos.Nombre.Trim(); datos.Apellidos = datos.Apellidos.Trim(); datos.Correo = NormalizarCorreo(datos.Correo);
        datos.Telefono = datos.Telefono.Trim();
    }
    private static Respuesta<T> Error<T>(string mensaje) => new() { Success = false, Error = mensaje };
}
