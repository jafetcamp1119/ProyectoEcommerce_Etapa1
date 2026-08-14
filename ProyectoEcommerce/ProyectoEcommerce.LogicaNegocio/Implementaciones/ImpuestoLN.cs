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
    // aqui se validan y guardan los impuestos que despues se usan para separar precio base e impuesto
    public class ImpuestoLN : IImpuestoLN
    {
        private IUnidadTrabajoEF _unidadDeTrabajo { get; set; }
        private ILogger<ImpuestoLN> _logger { get; }
        private readonly IMapper _mapper;

        public ImpuestoLN(IUnidadTrabajoEF unidadTrabajo, ILogger<ImpuestoLN> logger, IMapper mapper)
        {
            _unidadDeTrabajo = unidadTrabajo;
            _logger = logger;
            _mapper = mapper;
        }

        // recibe un impuesto nuevo, revisa nombre, porcentaje y fechas y devuelve lo que se guardo
        public async Task<Respuesta<TImpuesto>> InsertarAsync(TImpuesto datos)
        {
            var resultado = new Respuesta<TImpuesto>();
            try
            {
                Limpiar(datos);
                // Validar devuelve un mensaje cuando algo esta mal o null cuando se puede seguir
                var validacion = Validar(datos);
                if (validacion != null) return Error<TImpuesto>(validacion);
                var existente = await _unidadDeTrabajo.TImpuesto.ObtenerEntidadAsync(x => x.Nombre == datos.Nombre);
                if (existente.Data != null)
                {
                    resultado.Data = _mapper.Map<TImpuesto>(existente.Data);
                    resultado.Error = Mensajes.RegistroDuplicado;
                    return resultado;
                }
                // si no vino una fecha usa hoy para que la vigencia tenga un inicio real
                if (datos.FechaInicio == default)
                    datos.FechaInicio = DateOnly.FromDateTime(DateTime.Today);
                var entidad = _mapper.Map<Impuesto>(datos);
                var resp = await _unidadDeTrabajo.TImpuesto.InsertarAsync(entidad);
                _unidadDeTrabajo.Completar();
                resultado.Data = _mapper.Map<TImpuesto>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al insertar impuesto {Nombre}", datos.Nombre);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // trae todos los impuestos para la pantalla administrativa
        public async Task<Respuesta<IEnumerable<TImpuesto>>> ListarAsync()
        {
            var resultado = new Respuesta<IEnumerable<TImpuesto>>();
            try
            {
                var resp = await _unidadDeTrabajo.TImpuesto.ListarAsync();
                resultado.Data = _mapper.Map<IEnumerable<TImpuesto>>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar impuestos.");
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // valida y guarda los cambios de un impuesto que ya existe
        public async Task<Respuesta<TImpuesto>> ModificarAsync(TImpuesto datos)
        {
            var resultado = new Respuesta<TImpuesto>();
            try
            {
                Limpiar(datos);
                var validacion = Validar(datos);
                if (validacion != null) return Error<TImpuesto>(validacion);
                var actual = await _unidadDeTrabajo.TImpuesto.ObtenerEntidadAsync(x => x.ImpuestoId == datos.ImpuestoId);
                if (actual.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoExisteModificar;
                    return resultado;
                }
                // busca el mismo nombre en otro ID para no crear dos impuestos iguales
                var duplicado = await _unidadDeTrabajo.TImpuesto.ObtenerEntidadAsync(
                    x => x.Nombre == datos.Nombre && x.ImpuestoId != datos.ImpuestoId);
                if (duplicado.Data != null) return Error<TImpuesto>(Mensajes.RegistroDuplicado);
                _mapper.Map(datos, actual.Data);
                var resp = await _unidadDeTrabajo.TImpuesto.ModificarAsync(actual.Data);
                _unidadDeTrabajo.Completar();
                resultado.Data = _mapper.Map<TImpuesto>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al modificar ImpuestoId {ImpuestoId}", datos.ImpuestoId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // cambia Activo a false sin borrar referencias de productos y ordenes anteriores
        public async Task<Respuesta<bool>> EliminarAsync(TImpuesto datos)
        {
            var resultado = new Respuesta<bool>();
            try
            {
                var entidad = await _unidadDeTrabajo.TImpuesto.ObtenerEntidadAsync(x => x.ImpuestoId == datos.ImpuestoId);
                if (entidad.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoExisteEliminar;
                    return resultado;
                }
                entidad.Data.Activo = false;
                var resp = await _unidadDeTrabajo.TImpuesto.ModificarAsync(entidad.Data);
                _unidadDeTrabajo.Completar();
                resultado.Data = resp.Data != null;
                resultado.Error = resp.Error;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar ImpuestoId {ImpuestoId}", datos.ImpuestoId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // busca impuestos que contienen el texto recibido en su nombre
        public async Task<Respuesta<IEnumerable<TImpuesto>>> BuscarAsync(TImpuesto datos)
        {
            var resultado = new Respuesta<IEnumerable<TImpuesto>>();
            try
            {
                var nombre = datos.Nombre ?? string.Empty;
                var resp = await _unidadDeTrabajo.TImpuesto.BuscarAsync(x => x.Nombre.Contains(nombre));
                resultado.Data = _mapper.Map<IEnumerable<TImpuesto>>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar impuestos.");
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // trae un solo impuesto por su ID o devuelve RegistroNoEncontrado
        public async Task<Respuesta<TImpuesto>> ObtenerAsync(TImpuesto datos)
        {
            var resultado = new Respuesta<TImpuesto>();
            try
            {
                var resp = await _unidadDeTrabajo.TImpuesto.ObtenerEntidadAsync(x => x.ImpuestoId == datos.ImpuestoId);
                if (resp.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoEncontrado;
                    return resultado;
                }
                resultado.Data = _mapper.Map<TImpuesto>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ImpuestoId {ImpuestoId}", datos.ImpuestoId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // hace las validaciones que comparten insertar y modificar
        private static string? Validar(TImpuesto datos)
        {
            if (string.IsNullOrWhiteSpace(datos.Nombre)) return Mensajes.NombreObligatorio;
            if (datos.Porcentaje < 0 || datos.Porcentaje > 100) return Mensajes.PorcentajeInvalido;
            if (datos.FechaFin.HasValue && datos.FechaFin.Value < datos.FechaInicio) return Mensajes.FechasInvalidas;
            return null;
        }

        // Trim evita guardar espacios que hagan parecer diferentes dos nombres iguales
        private static void Limpiar(TImpuesto datos) =>
            datos.Nombre = (datos.Nombre ?? string.Empty).Trim();

        private static Respuesta<T> Error<T>(string mensaje) =>
            new() { Success = false, Error = mensaje };
    }
}
