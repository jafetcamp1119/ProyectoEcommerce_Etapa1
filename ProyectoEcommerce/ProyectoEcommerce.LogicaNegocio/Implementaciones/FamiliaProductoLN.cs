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
    // aqui quedan las reglas para crear, editar y consultar familias
    // la lista del Cliente se separa para que nunca muestre familias desactivadas
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

        // recibe la familia del formulario, limpia sus textos y revisa que no haya otra con el mismo nombre
        // si todo esta bien la guarda y devuelve los datos con el ID que puso la BD
        public async Task<Respuesta<TFamiliaProducto>> InsertarAsync(TFamiliaProducto datos)
        {
            var resultado = new Respuesta<TFamiliaProducto>();
            try
            {
                // Trim quita espacios sobrantes al inicio y al final
                Limpiar(datos);
                if (string.IsNullOrWhiteSpace(datos.Nombre)) return Error<TFamiliaProducto>(Mensajes.NombreObligatorio);

                // ObtenerEntidadAsync trae la primera coincidencia o null cuando el nombre esta libre
                var existente = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.Nombre == datos.Nombre);
                if (existente.Data != null)
                {
                    resultado.Data = _mapper.Map<TFamiliaProducto>(existente.Data);
                    resultado.Error = Mensajes.RegistroDuplicado;
                    resultado.Success = false;
                    return resultado;
                }

                // convierte el DTO de la API en la entidad Database First que guarda EF
                // UrlImagen pasa por tener el mismo nombre en los dos tipos
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

        // trae todas las familias para administracion, incluidas las inactivas y su UrlImagen
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

        // trae solo familias activas y las acomoda por nombre para el primer nivel del catalogo
        public async Task<Respuesta<IEnumerable<TFamiliaProducto>>> ListarClienteAsync()
        {
            try
            {
                // BuscarAsync convierte esta condicion en un WHERE Activo = 1
                var respuesta = await _unidadDeTrabajo.TFamiliaProducto.BuscarAsync(x => x.Activo);
                if (!string.IsNullOrEmpty(respuesta.Error))
                    return Error<IEnumerable<TFamiliaProducto>>(Mensajes.ErrorOperacion);

                return new Respuesta<IEnumerable<TFamiliaProducto>>
                {
                    Data = _mapper.Map<IEnumerable<TFamiliaProducto>>(respuesta.Data ?? [])
                        // OrderBy acomoda la lista alfabeticamente antes de mandarla a Angular
                        .OrderBy(x => x.Nombre)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar las familias activas para el cliente.");
                return Error<IEnumerable<TFamiliaProducto>>(Mensajes.ErrorOperacion);
            }
        }

        // recibe una familia editada, revisa que exista y que el nuevo nombre no choque con otra
        // luego copia Nombre, Descripcion, UrlImagen y Activo sobre la entidad guardada
        public async Task<Respuesta<TFamiliaProducto>> ModificarAsync(TFamiliaProducto datos)
        {
            var resultado = new Respuesta<TFamiliaProducto>();
            try
            {
                Limpiar(datos);
                if (string.IsNullOrWhiteSpace(datos.Nombre)) return Error<TFamiliaProducto>(Mensajes.NombreObligatorio);
                // primero busca por ID porque nunca se debe modificar un registro que ya no existe
                var actual = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.FamiliaId == datos.FamiliaId);
                if (actual.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoExisteModificar;
                    return resultado;
                }

                // compara con todas menos con ella misma para permitir conservar el nombre actual
                var duplicado = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(
                    x => x.Nombre == datos.Nombre && x.FamiliaId != datos.FamiliaId);
                if (duplicado.Data != null) return Error<TFamiliaProducto>(Mensajes.RegistroDuplicado);

                // esta sobrecarga de Map copia los datos sobre la entidad que EF ya encontro
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

        // recibe el ID y cambia Activo a false
        // no borra la fila porque sus categorias y productos todavia la usan
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

                // esta es una eliminacion logica, por eso se llama ModificarAsync despues
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

        // busca familias cuyo nombre contiene el texto recibido y devuelve una lista
        public async Task<Respuesta<IEnumerable<TFamiliaProducto>>> BuscarAsync(TFamiliaProducto datos)
        {
            var resultado = new Respuesta<IEnumerable<TFamiliaProducto>>();
            try
            {
                var nombre = datos.Nombre ?? string.Empty;
                // Contains se convierte en una busqueda parcial de SQL
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

        // trae una sola familia por ID, por ejemplo antes de guardar una imagen
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

        // quita espacios que podrian causar duplicados que se ven iguales en pantalla
        private static void Limpiar(TFamiliaProducto datos)
        {
            datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
            datos.Descripcion = datos.Descripcion?.Trim();
        }

        private static Respuesta<T> Error<T>(string mensaje) =>
            new() { Success = false, Error = mensaje };
    }
}
