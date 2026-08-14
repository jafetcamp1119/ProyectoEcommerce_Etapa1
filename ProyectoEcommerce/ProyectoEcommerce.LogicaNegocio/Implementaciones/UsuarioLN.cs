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

// aqui se manejan registro, login, bloqueos y administracion de usuarios
// tambien protege la creacion del primer Administrador y nunca guarda contraseñas sin hash
public class UsuarioLN : IUsuarioLN
{
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;
    private readonly ILogger<UsuarioLN> _logger;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly int _maxIntentos;
    private readonly int _minutosBloqueo;

    public UsuarioLN(
        IUnidadTrabajoEF unidadTrabajo,
        ILogger<UsuarioLN> logger,
        IMapper mapper,
        IPasswordHasher<Usuario> passwordHasher,
        IConfiguration configuration)
    {
        _unidadDeTrabajo = unidadTrabajo;
        _logger = logger;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        // lee los limites de appsettings y Math.Max evita valores menores que uno
        _maxIntentos = Math.Max(1, configuration.GetValue<int?>("Seguridad:MaxIntentosFallidos") ?? 3);
        _minutosBloqueo = Math.Max(1, configuration.GetValue<int?>("Seguridad:BloqueoMinutos") ?? 15);
    }

    // recibe nombre, correo y contraseña del registro publico
    // crea una cuenta de Cliente si el correo esta libre y devuelve el usuario sin el PasswordHash
    public async Task<Respuesta<TUsuario>> RegistrarAsync(TRegistroUsuario datos)
    {
        try
        {
            // normaliza textos y deja el correo en minusculas antes de comparar
            LimpiarRegistro(datos);
            var correo = NormalizarCorreo(datos.Correo);
            // busca el correo exacto y devuelve null si todavia no esta registrado
            var existente = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.Correo == correo);
            if (!string.IsNullOrEmpty(existente.Error)) return Error<TUsuario>(Mensajes.ErrorRegistro);
            if (existente.Data != null) return Error<TUsuario>(Mensajes.CorreoDuplicado);

            // el rol se toma de la BD, no se acepta un RolId enviado por el registro publico
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
            // HashPassword convierte la contraseña en un hash que no se puede volver a leer
            // la contraseña original nunca se guarda en la entidad ni en la BD
            entidad.PasswordHash = _passwordHasher.HashPassword(entidad, datos.Contrasena);
            var insercion = await _unidadDeTrabajo.TUsuario.InsertarAsync(entidad);
            if (insercion.Data == null || !string.IsNullOrEmpty(insercion.Error))
            {
                // tambien revisa la restriccion unica de SQL por si dos registros llegaron al mismo tiempo
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

    // devuelve true solamente cuando todavia no existe ningun Administrador
    public async Task<Respuesta<bool>> RequiereConfiguracionInicialAsync()
    {
        try
        {
            var rolAdministrador = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(
                x => x.Nombre == "Administrador" && x.Activo);
            if (!string.IsNullOrEmpty(rolAdministrador.Error) || rolAdministrador.Data == null)
                return Error<bool>(Mensajes.ErrorConfiguracionInicial);

            // ContarAsync hace el COUNT en SQL y no trae todos los usuarios a memoria
            var administradores = await _unidadDeTrabajo.TUsuario.ContarAsync(
                x => x.RolId == rolAdministrador.Data.RolId);
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

    // recibe los datos del setup y permite que cada instalacion elija su propio correo
    // usa una transaccion Serializable para que dos solicitudes no creen dos Administradores iniciales
    public async Task<Respuesta<TUsuario>> CrearAdministradorInicialAsync(TRegistroUsuario datos)
    {
        LimpiarRegistro(datos);
        var correo = NormalizarCorreo(datos.Correo);

        try
        {
            // Serializable mantiene protegida la revision hasta terminar el Insert y el Commit
            _unidadDeTrabajo.EmpezarTransaccion(IsolationLevel.Serializable);

            var rolAdministrador = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(
                x => x.Nombre == "Administrador" && x.Activo);
            if (!string.IsNullOrEmpty(rolAdministrador.Error) || rolAdministrador.Data == null)
            {
                // Rollback devuelve cualquier cambio hecho dentro de esta transaccion
                _unidadDeTrabajo.Rollback();
                return Error<TUsuario>(Mensajes.ErrorConfiguracionInicial);
            }

            var administradores = await _unidadDeTrabajo.TUsuario.ContarAsync(
                x => x.RolId == rolAdministrador.Data.RolId);
            if (!string.IsNullOrEmpty(administradores.Error) || administradores.Data == null)
            {
                _unidadDeTrabajo.Rollback();
                return Error<TUsuario>(Mensajes.ErrorConfiguracionInicial);
            }
            // vuelve a contar dentro de la transaccion para evitar saltarse la regla por concurrencia
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
            // usa exactamente el mismo hash seguro que el registro normal
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

            // CompletarTran guarda y hace Commit, desde aqui el Administrador ya queda definitivo
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

    // recibe correo y contraseña del login, revisa el hash y controla los intentos fallidos
    // si todo esta bien devuelve el usuario con su rol y menu para que el controller cree el JWT
    public async Task<Respuesta<TEstadoAutenticacion>> AutenticarAsync(TLoginUsuario datos)
    {
        var correo = NormalizarCorreo(datos.Correo);
        try
        {
            // Include trae rol, permisos y opciones de menu en la misma consulta del usuario
            var respuesta = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                x => x.Correo == correo,
                ["Rol.RolMenuOpciones.MenuOpcion"]);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TEstadoAutenticacion>(Mensajes.ErrorAutenticacion);

            var usuario = respuesta.Data;
            // usa el mismo mensaje si no existe, esta inactivo o no tiene hash para no dar pistas
            if (usuario == null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.PasswordHash))
            {
                await RegistrarAccesoAsync(usuario?.UsuarioId, correo, false);
                return Error<TEstadoAutenticacion>(Mensajes.CredencialesIncorrectas);
            }

            var ahora = DateTime.UtcNow;
            // si la fecha de bloqueo sigue en el futuro no intenta revisar la contraseña
            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > ahora)
            {
                await RegistrarAccesoAsync(usuario.UsuarioId, correo, false);
                return Bloqueado(usuario.BloqueadoHasta.Value, ahora);
            }

            // si la fecha ya paso limpia el bloqueo anterior antes de probar el nuevo login
            if (usuario.BloqueadoHasta.HasValue)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
                usuario.UltimoIntentoFallido = null;
            }

            // VerifyHashedPassword compara la contraseña escrita contra el hash sin descifrarlo
            var verificacion = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, datos.Contrasena);

            // cada fallo suma un intento y al llegar al limite pone una fecha de desbloqueo
            if (verificacion == PasswordVerificationResult.Failed)
            {
                usuario.IntentosFallidos++;
                usuario.UltimoIntentoFallido = ahora;
                if (usuario.IntentosFallidos >= _maxIntentos)
                    usuario.BloqueadoHasta = ahora.AddMinutes(_minutosBloqueo);

                await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario);
                await RegistrarAccesoAsync(usuario.UsuarioId, correo, false);
                // el ternario devuelve el tiempo restante si se bloqueo o el mensaje normal si aun quedan intentos
                return usuario.BloqueadoHasta.HasValue
                    ? Bloqueado(usuario.BloqueadoHasta.Value, ahora)
                    : Error<TEstadoAutenticacion>(Mensajes.CredencialesIncorrectas);
            }

            // una contraseña correcta limpia todos los datos de intentos anteriores
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            usuario.UltimoIntentoFallido = null;
            // Identity puede pedir un hash nuevo si la configuracion de seguridad cambio
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

    // recibe filtros administrativos, trae las coincidencias y devuelve solo la pagina pedida
    public async Task<Respuesta<TPagina<TUsuario>>> ListarAdministracionAsync(TFiltroUsuarios filtro)
    {
        try
        {
            var texto = (filtro.Texto ?? string.Empty).Trim();
            // Contains deja solamente los tamaños permitidos y usa 25 si llega otro valor
            var tamano = new[] { 25, 50, 75, 100 }.Contains(filtro.TamanoPagina) ? filtro.TamanoPagina : 25;
            var pagina = Math.Max(1, filtro.Pagina);
            // combina texto, rol y estado en una sola condicion que el repositorio manda a SQL
            var respuesta = await _unidadDeTrabajo.TUsuario.BuscarAsync(x =>
                (texto == "" || x.Nombre.Contains(texto) || x.Apellidos.Contains(texto) || x.Correo.Contains(texto)) &&
                (!filtro.RolId.HasValue || x.RolId == filtro.RolId.Value) &&
                (!filtro.Activo.HasValue || x.Activo == filtro.Activo.Value), ["Rol"]);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TPagina<TUsuario>>(Mensajes.ErrorOperacion);
            // primero acomoda por nombre y apellido para que las paginas sean estables
            var consulta = (respuesta.Data ?? []).OrderBy(x => x.Nombre).ThenBy(x => x.Apellidos).ToList();
            return new Respuesta<TPagina<TUsuario>>
            {
                Data = new TPagina<TUsuario>
                {
                    // Skip salta paginas anteriores, Take toma esta pagina y Select convierte cada entidad
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

    // trae solamente Administrador y Cliente activos para el selector de roles
    public async Task<Respuesta<IEnumerable<TRol>>> ListarRolesAsync()
    {
        var respuesta = await _unidadDeTrabajo.TRol.BuscarAsync(x => x.Activo && (x.Nombre == "Administrador" || x.Nombre == "Cliente"));
        if (!string.IsNullOrEmpty(respuesta.Error)) return Error<IEnumerable<TRol>>(Mensajes.ErrorOperacion);
        return new Respuesta<IEnumerable<TRol>>
        {
            Data = (respuesta.Data ?? []).OrderBy(x => x.Nombre).Select(x => new TRol { RolId = x.RolId, Nombre = x.Nombre })
        };
    }

    // cambia el rol pedido y evita quitar al ultimo Administrador activo
    public async Task<Respuesta<TUsuario>> CambiarRolAsync(TCambioRolUsuario datos, int administradorId)
    {
        try
        {
            // trae el rol actual junto con el usuario porque la regla depende de los dos
            var usuario = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.UsuarioId == datos.UsuarioId, ["Rol"]);
            if (usuario.Data == null) return Error<TUsuario>(Mensajes.RegistroNoEncontrado);
            var rol = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(
            x => x.RolId == datos.RolId && x.Activo && (x.Nombre == "Administrador" || x.Nombre == "Cliente"));
            if (rol.Data == null) return Error<TUsuario>(Mensajes.RolNoEncontrado);

            // si se intenta quitar el rol al ultimo Admin se detiene para no dejar el sistema sin administracion
            if (usuario.Data.Activo && usuario.Data.Rol.Nombre ==
            "Administrador" && rol.Data.Nombre != "Administrador" && !await HayOtroAdministradorActivo(usuario.Data.UsuarioId))
                return Error<TUsuario>(Mensajes.UltimoAdministrador);

            var rolAnterior = usuario.Data.Rol.Nombre;
            usuario.Data.RolId = rol.Data.RolId;
            usuario.Data.Rol = rol.Data;
            var actualizacion = await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario.Data);
            if (actualizacion.Data == null || !string.IsNullOrEmpty(actualizacion.Error)) return Error<TUsuario>(Mensajes.ErrorOperacion);
            // despues de guardar deja el cambio anterior y nuevo en bitacora
            await RegistrarBitacora(administradorId, "CAMBIO_ROL", usuario.Data.UsuarioId, $"Rol: {rolAnterior} -> {rol.Data.Nombre}");
            return new Respuesta<TUsuario> { Data = MapearUsuario(usuario.Data, rol.Data.Nombre) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar el rol de UsuarioId {UsuarioId}", datos.UsuarioId);
            return Error<TUsuario>(Mensajes.ErrorOperacion);
        }
    }

    // activa o desactiva una cuenta y vuelve a proteger al ultimo Administrador
    public async Task<Respuesta<TUsuario>> CambiarEstadoAsync(TCambioEstadoUsuario datos, int administradorId)
    {
        try
        {
            var usuario = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.UsuarioId == datos.UsuarioId, ["Rol"]);
            if (usuario.Data == null) return Error<TUsuario>(Mensajes.RegistroNoEncontrado);
            if (!datos.Activo && usuario.Data.Activo && usuario.Data.Rol.Nombre ==
            "Administrador" && !await HayOtroAdministradorActivo(usuario.Data.UsuarioId))
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

    // no permite crear usuarios por el CRUD viejo porque registro y setup controlan contraseña y rol
    public Task<Respuesta<TUsuario>> InsertarAsync(TUsuario datos) => Task.FromResult(Error<TUsuario>(Mensajes.ErrorOperacion));

    // conserva la lista del contrato original usando la pagina administrativa mas grande permitida
    public async Task<Respuesta<IEnumerable<TUsuario>>> ListarAsync()
    {
        var pagina = await ListarAdministracionAsync(new TFiltroUsuarios { TamanoPagina = 100 });
        return string.IsNullOrEmpty(pagina.Error)
            ? new Respuesta<IEnumerable<TUsuario>> { Data = pagina.Data?.Items ?? [] }
            : Error<IEnumerable<TUsuario>>(pagina.Error);
    }

    // actualiza solamente datos personales, no toca correo, PasswordHash ni rol
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
            return respuesta.Data == null ? Error<TUsuario>(Mensajes.ErrorOperacion) : new Respuesta<TUsuario> {
            Data = MapearUsuario(actual.Data) };
        }
        catch { return Error<TUsuario>(Mensajes.ErrorOperacion); }
    }

    // eliminar en realidad llama la desactivacion logica para conservar historial y ordenes
    public async Task<Respuesta<bool>> EliminarAsync(TUsuario datos)
    {
        var cambio = await CambiarEstadoAsync(new TCambioEstadoUsuario { UsuarioId = datos.UsuarioId, Activo = false }, 0);
        return string.IsNullOrEmpty(cambio.Error) ? new Respuesta<bool> { Data = true } : Error<bool>(cambio.Error);
    }

    // reutiliza la lista paginada para buscar por nombre, apellido o correo
    public async Task<Respuesta<IEnumerable<TUsuario>>> BuscarAsync(TUsuario datos)
    {
        var pagina = await ListarAdministracionAsync(new TFiltroUsuarios { Texto = datos.Nombre, TamanoPagina = 100 });
        return string.IsNullOrEmpty(pagina.Error)
            ? new Respuesta<IEnumerable<TUsuario>> { Data = pagina.Data?.Items ?? [] }
            : Error<IEnumerable<TUsuario>>(pagina.Error);
    }

    // trae un usuario por ID con su rol y lo convierte sin exponer PasswordHash
    public async Task<Respuesta<TUsuario>> ObtenerAsync(TUsuario datos)
    {
        var respuesta = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.UsuarioId == datos.UsuarioId, ["Rol"]);
        return respuesta.Data == null ? Error<TUsuario>(Mensajes.RegistroNoEncontrado) : new Respuesta<TUsuario> {
        Data = MapearUsuario(respuesta.Data) };
    }

    // cuenta Administradores activos dejando por fuera al usuario que se quiere cambiar
    private async Task<bool> HayOtroAdministradorActivo(int usuarioId)
    {
        var rol = await _unidadDeTrabajo.TRol.ObtenerEntidadAsync(x => x.Nombre == "Administrador");
        if (rol.Data == null) return false;
        var cantidad = await _unidadDeTrabajo.TUsuario.ContarAsync(x => x.Activo && x.RolId == rol.Data.RolId && x.UsuarioId != usuarioId);
        return (cantidad.Data ?? 0) > 0;
    }

    // guarda quien hizo un cambio administrativo y sobre cual usuario
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

    // guarda cada intento de login con correo, fecha y si fue correcto o no
    private async Task RegistrarAccesoAsync(int? usuarioId, string correo, bool exitoso)
    {
        var respuesta = await _unidadDeTrabajo.THistorialAcceso.InsertarAsync(new HistorialAcceso
        {
            UsuarioId = usuarioId,
            CorreoIntentado = correo,
            Fecha = DateTime.UtcNow,
            Exitoso = exitoso
        });
        if (!string.IsNullOrEmpty(respuesta.Error)) _logger.LogWarning(
        "No fue posible registrar el historial de acceso: {Error}", respuesta.Error);
    }

    // arma la respuesta que Angular usa para mostrar cuanto falta para intentar de nuevo
    private static Respuesta<TEstadoAutenticacion> Bloqueado(DateTime hasta, DateTime ahora) => new()
    {
        Success = false,
        Error = Mensajes.AccesoBloqueado,
        Data = new TEstadoAutenticacion
        {
            Bloqueado = true,
            BloqueadoHasta = hasta,
            // Ceiling redondea hacia arriba para no mostrar cero mientras aun queda una fraccion de segundo
            SegundosRestantes = Math.Max(1, (int)Math.Ceiling((hasta - ahora).TotalSeconds))
        }
    };

    // convierte la entidad en el usuario seguro que se manda a Angular
    // si incluirMenu es true filtra opciones activas, las acomoda y crea un DTO por cada una
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
        // Where filtra, OrderBy acomoda y Select convierte cada opcion de menu
        MenuOpciones = incluirMenu
            ? (usuario.Rol?.RolMenuOpciones ?? []).Where(x => x.MenuOpcion.Activo).OrderBy(x => x.MenuOpcion.Orden)
                .Select(x => new TMenuOpcion { MenuOpcionId =
                x.MenuOpcionId, Nombre = x.MenuOpcion.Nombre, Ruta = x.MenuOpcion.Ruta,
                Icono = x.MenuOpcion.Icono, Orden = x.MenuOpcion.Orden }).ToList()
            : []
    };

    // deja el correo sin espacios y en minusculas para comparar siempre de la misma forma
    private static string NormalizarCorreo(string? correo) => (correo ?? string.Empty).Trim().ToLowerInvariant();

    // limpia los textos antes de guardar un registro
    private static void LimpiarRegistro(TRegistroUsuario datos)
    {
        datos.Nombre = datos.Nombre.Trim();
        datos.Apellidos = datos.Apellidos.Trim();
        datos.Correo = NormalizarCorreo(datos.Correo);
        datos.Telefono = datos.Telefono.Trim();
    }

    private static Respuesta<T> Error<T>(string mensaje) => new() { Success = false, Error = mensaje };
}
