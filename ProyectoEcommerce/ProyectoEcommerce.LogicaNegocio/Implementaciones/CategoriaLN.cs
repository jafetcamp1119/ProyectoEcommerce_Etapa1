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
    // aqui se revisan las reglas de categorias antes de hablar con el repositorio
    // cada categoria debe pertenecer a una familia real y su nombre no se repite dentro de ella
    public class CategoriaLN : ICategoriaLN
    {
        private IUnidadTrabajoEF _unidadDeTrabajo { get; set; }
        private ILogger<CategoriaLN> _logger { get; }
        private readonly IMapper _mapper;

        public CategoriaLN(IUnidadTrabajoEF unidadTrabajo, ILogger<CategoriaLN> logger, IMapper mapper)
        {
            _unidadDeTrabajo = unidadTrabajo;
            _logger = logger;
            _mapper = mapper;
        }

        // recibe la categoria del formulario, limpia los textos y revisa nombre y familia
        // devuelve la categoria guardada con su ID y con UrlImagen cuando venga informada
        public async Task<Respuesta<TCategoria>> InsertarAsync(TCategoria datos)
        {
            var resultado = new Respuesta<TCategoria>();
            try
            {
                Limpiar(datos);
                // ValidarAsync tambien consulta que la familia seleccionada exista
                var validacion = await ValidarAsync(datos);
                if (validacion != null) return Error<TCategoria>(validacion);
                // el mismo nombre si puede existir en otra familia, por eso compara los dos datos
                var existente = await _unidadDeTrabajo.TCategoria.ObtenerEntidadAsync(
                x => x.FamiliaId == datos.FamiliaId && x.Nombre == datos.Nombre);
                if (existente.Data != null)
                {
                    resultado.Data = _mapper.Map<TCategoria>(existente.Data);
                    resultado.Error = Mensajes.RegistroDuplicado;
                    resultado.Success = false;
                    return resultado;
                }
                // AutoMapper convierte el DTO en entidad y por nombre tambien copia UrlImagen
                var entidad = _mapper.Map<Categoria>(datos);
                var resp = await _unidadDeTrabajo.TCategoria.InsertarAsync(entidad);
                _unidadDeTrabajo.Completar();
                resultado.Data = _mapper.Map<TCategoria>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al insertar categoria {Nombre}", datos.Nombre);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // lista todas las categorias para administracion, incluidas las desactivadas
        public async Task<Respuesta<IEnumerable<TCategoria>>> ListarAsync()
        {
            var resultado = new Respuesta<IEnumerable<TCategoria>>();
            try
            {
                var resp = await _unidadDeTrabajo.TCategoria.ListarAsync();
                resultado.Data = _mapper.Map<IEnumerable<TCategoria>>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar categorias.");
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // actualiza una categoria despues de revisar que exista, que la familia sea valida y que no haya duplicado
        public async Task<Respuesta<TCategoria>> ModificarAsync(TCategoria datos)
        {
            var resultado = new Respuesta<TCategoria>();
            try
            {
                Limpiar(datos);
                var validacion = await ValidarAsync(datos);
                if (validacion != null) return Error<TCategoria>(validacion);
                // FirstOrDefaultAsync dentro del repositorio devuelve null si el ID ya no existe
                var actual = await _unidadDeTrabajo.TCategoria.ObtenerEntidadAsync(x => x.CategoriaId == datos.CategoriaId);
                if (actual.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoExisteModificar;
                    return resultado;
                }
                // excluye el mismo ID para que conservar el nombre no se marque como duplicado
                var duplicado = await _unidadDeTrabajo.TCategoria.ObtenerEntidadAsync(
                    x => x.FamiliaId == datos.FamiliaId && x.Nombre == datos.Nombre && x.CategoriaId != datos.CategoriaId);
                if (duplicado.Data != null) return Error<TCategoria>(Mensajes.RegistroDuplicado);
                // copia los cambios sobre la entidad encontrada, incluida una UrlImagen null si se quito
                _mapper.Map(datos, actual.Data);
                var resp = await _unidadDeTrabajo.TCategoria.ModificarAsync(actual.Data);
                _unidadDeTrabajo.Completar();
                resultado.Data = _mapper.Map<TCategoria>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al modificar CategoriaId {CategoriaId}", datos.CategoriaId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // desactiva la categoria sin borrar los productos que dependen de ella
        public async Task<Respuesta<bool>> EliminarAsync(TCategoria datos)
        {
            var resultado = new Respuesta<bool>();
            try
            {
                var entidad = await _unidadDeTrabajo.TCategoria.ObtenerEntidadAsync(x => x.CategoriaId == datos.CategoriaId);
                if (entidad.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoExisteEliminar;
                    return resultado;
                }
                entidad.Data.Activo = false;
                var resp = await _unidadDeTrabajo.TCategoria.ModificarAsync(entidad.Data);
                _unidadDeTrabajo.Completar();
                resultado.Data = resp.Data != null;
                resultado.Error = resp.Error;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar CategoriaId {CategoriaId}", datos.CategoriaId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // busca por una parte del nombre y devuelve todas las coincidencias
        public async Task<Respuesta<IEnumerable<TCategoria>>> BuscarAsync(TCategoria datos)
        {
            var resultado = new Respuesta<IEnumerable<TCategoria>>();
            try
            {
                var nombre = datos.Nombre ?? string.Empty;
                // Contains termina como una busqueda parcial en SQL Server
                var resp = await _unidadDeTrabajo.TCategoria.BuscarAsync(x => x.Nombre.Contains(nombre));
                resultado.Data = _mapper.Map<IEnumerable<TCategoria>>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar categorias.");
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // trae una categoria por ID, tambien se usa antes de asociarle una imagen
        public async Task<Respuesta<TCategoria>> ObtenerAsync(TCategoria datos)
        {
            var resultado = new Respuesta<TCategoria>();
            try
            {
                var resp = await _unidadDeTrabajo.TCategoria.ObtenerEntidadAsync(x => x.CategoriaId == datos.CategoriaId);
                if (resp.Data == null)
                {
                    resultado.Error = Mensajes.RegistroNoEncontrado;
                    return resultado;
                }
                resultado.Data = _mapper.Map<TCategoria>(resp.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener CategoriaId {CategoriaId}", datos.CategoriaId);
                resultado.Success = false;
                resultado.Error = Mensajes.ErrorOperacion;
            }
            return resultado;
        }

        // recibe una familia y trae todas sus categorias para la pantalla administrativa
        public async Task<Respuesta<IEnumerable<TCategoria>>> ListarPorFamiliaAsync(int familiaId)
        {
            if (familiaId <= 0) return Error<IEnumerable<TCategoria>>(Mensajes.FamiliaObligatoria);
            try
            {
                var familia = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.FamiliaId == familiaId);
                if (familia.Data == null) return Error<IEnumerable<TCategoria>>(Mensajes.FamiliaNoEncontrada);
                // este filtro se ejecuta en SQL y evita traer categorias de otras familias
                var respuesta = await _unidadDeTrabajo.TCategoria.BuscarAsync(x => x.FamiliaId == familiaId);
                if (!string.IsNullOrEmpty(respuesta.Error)) return Error<IEnumerable<TCategoria>>(Mensajes.ErrorOperacion);
                return new Respuesta<IEnumerable<TCategoria>>
                {
                    Data = _mapper.Map<IEnumerable<TCategoria>>(respuesta.Data)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar categorías de FamiliaId {FamiliaId}", familiaId);
                return Error<IEnumerable<TCategoria>>(Mensajes.ErrorOperacion);
            }
        }

        // para el Cliente primero revisa que la familia este activa y luego trae solo sus categorias activas
        public async Task<Respuesta<IEnumerable<TCategoria>>> ListarClientePorFamiliaAsync(int familiaId)
        {
            if (familiaId <= 0) return Error<IEnumerable<TCategoria>>(Mensajes.FamiliaObligatoria);
            try
            {
                var familia = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(
                    x => x.FamiliaId == familiaId && x.Activo);
                if (familia.Data == null) return Error<IEnumerable<TCategoria>>(Mensajes.FamiliaNoEncontrada);

                var respuesta = await _unidadDeTrabajo.TCategoria.BuscarAsync(
                    x => x.FamiliaId == familiaId && x.Activo);
                if (!string.IsNullOrEmpty(respuesta.Error))
                    return Error<IEnumerable<TCategoria>>(Mensajes.ErrorOperacion);

                return new Respuesta<IEnumerable<TCategoria>>
                {
                    // ?? [] usa una lista vacia si el repositorio no devolvio datos
                    Data = _mapper.Map<IEnumerable<TCategoria>>(respuesta.Data ?? [])
                        .OrderBy(x => x.Nombre)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar categorías activas de FamiliaId {FamiliaId} para el cliente.", familiaId);
                return Error<IEnumerable<TCategoria>>(Mensajes.ErrorOperacion);
            }
        }

        // revisa los campos obligatorios y devuelve el primer mensaje encontrado
        // si todo esta bien devuelve null para que el proceso pueda seguir
        private async Task<string?> ValidarAsync(TCategoria datos)
        {
            if (string.IsNullOrWhiteSpace(datos.Nombre)) return Mensajes.NombreObligatorio;
            if (datos.FamiliaId <= 0) return Mensajes.FamiliaObligatoria;
            var familia = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.FamiliaId == datos.FamiliaId);
            return familia.Data == null ? Mensajes.FamiliaNoEncontrada : null;
        }

        // quita espacios sobrantes antes de comparar o guardar los textos
        private static void Limpiar(TCategoria datos)
        {
            datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
            datos.Descripcion = datos.Descripcion?.Trim();
        }

        private static Respuesta<T> Error<T>(string mensaje) =>
            new() { Success = false, Error = mensaje };
    }
}
