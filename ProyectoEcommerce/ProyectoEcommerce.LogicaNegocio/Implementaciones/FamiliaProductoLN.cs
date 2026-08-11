using AutoMapper;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones
{
    /// <summary>
    /// Gestiona familias de producto y separa la lista administrativa de la navegación activa del Cliente.
    /// </summary>
    public class FamiliaProductoLN : IFamiliaProductoLN
    {
        private IUnidadTrabajoEF _unidadDeTrabajo { get; set; }
        private ILogger<FamiliaProductoLN> _logger { get; }
        private readonly IMapper _mapper;

        public FamiliaProductoLN(IUnidadTrabajoEF unidadTrabajo, ILogger<FamiliaProductoLN> logger, IMapper mapper)
        {
            _unidadDeTrabajo = unidadTrabajo;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>Crea una familia después de normalizar y validar su nombre único.</summary>
        public async Task<Respuesta<TFamiliaProducto>> InsertarAsync(TFamiliaProducto datos)
        {
            var resultado = new Respuesta<TFamiliaProducto>();
            try
            {
                Limpiar(datos);
                if (string.IsNullOrWhiteSpace(datos.Nombre)) return Error<TFamiliaProducto>(Mensajes.NombreObligatorio);
                var existente = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.Nombre == datos.Nombre);
                if (existente.Data != null)
                {
                    resultado.Data = _mapper.Map<TFamiliaProducto>(existente.Data);
                    resultado.Error = Mensajes.RegistroDuplicado;
                    resultado.Success = false;
                    return resultado;
                }

                var entidad = _mapper.Map<FamiliaProducto>(datos);
                var respuestaRepositorio = await _unidadDeTrabajo.TFamiliaProducto.InsertarAsync(entidad);
                _unidadDeTrabajo.Completar();
                resultado.Data = _mapper.Map<TFamiliaProducto>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al insertar familia de producto {Nombre}", datos.Nombre);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TFamiliaProducto>>> ListarAsync()
        {
            var resultado = new Respuesta<IEnumerable<TFamiliaProducto>>();
            try
            {
                var resp = await _unidadDeTrabajo.TFamiliaProducto.ListarAsync();
                resultado.Data = _mapper.Map<IEnumerable<TFamiliaProducto>>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar familias de producto.");
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        /// <summary>Devuelve únicamente familias activas para el primer nivel del catálogo.</summary>
        public async Task<Respuesta<IEnumerable<TFamiliaProducto>>> ListarClienteAsync()
        {
            try
            {
                var respuesta = await _unidadDeTrabajo.TFamiliaProducto.BuscarAsync(x => x.Activo);
                if (!string.IsNullOrEmpty(respuesta.Error))
                    return Error<IEnumerable<TFamiliaProducto>>(Mensajes.ErrorOperacion);

                return new Respuesta<IEnumerable<TFamiliaProducto>>
                {
                    Data = _mapper.Map<IEnumerable<TFamiliaProducto>>(respuesta.Data ?? [])
                        .OrderBy(x => x.Nombre)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar las familias activas para el cliente.");
                return Error<IEnumerable<TFamiliaProducto>>(Mensajes.ErrorOperacion);
            }
        }

        public async Task<Respuesta<TFamiliaProducto>> ModificarAsync(TFamiliaProducto datos)
        {
            var resultado = new Respuesta<TFamiliaProducto>();
            try
            {
                Limpiar(datos);
                if (string.IsNullOrWhiteSpace(datos.Nombre)) return Error<TFamiliaProducto>(Mensajes.NombreObligatorio);
                var actual = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.FamiliaId == datos.FamiliaId);
                if (actual.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoExisteModificar;
                    return resultado;
                }

                var duplicado = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(
                    x => x.Nombre == datos.Nombre && x.FamiliaId != datos.FamiliaId);
                if (duplicado.Data != null) return Error<TFamiliaProducto>(Mensajes.RegistroDuplicado);

                _mapper.Map(datos, actual.Data);
                var resp = await _unidadDeTrabajo.TFamiliaProducto.ModificarAsync(actual.Data);
                _unidadDeTrabajo.Completar();
                resultado.Data = _mapper.Map<TFamiliaProducto>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al modificar FamiliaId {FamiliaId}", datos.FamiliaId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        /// <summary>Desactiva la familia sin eliminar sus categorías ni productos relacionados.</summary>
        public async Task<Respuesta<bool>> EliminarAsync(TFamiliaProducto datos)
        {
            var resultado = new Respuesta<bool>();
            try
            {
                var entidad = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.FamiliaId == datos.FamiliaId);
                if (entidad.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoExisteEliminar;
                    return resultado;
                }

                entidad.Data.Activo = false;
                var resp = await _unidadDeTrabajo.TFamiliaProducto.ModificarAsync(entidad.Data);
                _unidadDeTrabajo.Completar();
                resultado.Data = resp.Data != null;
                resultado.Error = resp.Error;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar FamiliaId {FamiliaId}", datos.FamiliaId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TFamiliaProducto>>> BuscarAsync(TFamiliaProducto datos)
        {
            var resultado = new Respuesta<IEnumerable<TFamiliaProducto>>();
            try
            {
                var nombre = datos.Nombre ?? string.Empty;
                var resp = await _unidadDeTrabajo.TFamiliaProducto.BuscarAsync(x => x.Nombre.Contains(nombre));
                resultado.Data = _mapper.Map<IEnumerable<TFamiliaProducto>>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar familias de producto.");
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        public async Task<Respuesta<TFamiliaProducto>> ObtenerAsync(TFamiliaProducto datos)
        {
            var resultado = new Respuesta<TFamiliaProducto>();
            try
            {
                var resp = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.FamiliaId == datos.FamiliaId);
                if (resp.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoEncontrado;
                    return resultado;
                }
                resultado.Data = _mapper.Map<TFamiliaProducto>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener FamiliaId {FamiliaId}", datos.FamiliaId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        private static void Limpiar(TFamiliaProducto datos)
        {
            datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
            datos.Descripcion = datos.Descripcion?.Trim();
        }

        private static Respuesta<T> Error<T>(string mensaje) =>
            new() { Success = false, Error = mensaje };
    }
}
