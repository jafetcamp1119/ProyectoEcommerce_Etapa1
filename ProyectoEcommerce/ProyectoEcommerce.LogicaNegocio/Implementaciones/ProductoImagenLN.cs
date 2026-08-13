
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;


namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;


public class ProductoImagenLN : IProductoImagenLN
{
    // Permite trabajar con los datos de la base de datos
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;

    // Permite convertir las entidades a entidades tipadas
    private readonly IMapper _mapper;

    // Permite registrar errores que ocurran en esta clase
    private readonly ILogger<ProductoImagenLN> _logger;


    // Constructor de la lógica de negocio de imágenes
    public ProductoImagenLN(
        IUnidadTrabajoEF unidadDeTrabajo,
        IMapper mapper,
        ILogger<ProductoImagenLN> logger)
    {
        // Guarda la unidad de trabajo recibida
        _unidadDeTrabajo = unidadDeTrabajo;

        // Guarda el mapper recibido
        _mapper = mapper;

        // Guarda el logger recibido
        _logger = logger;
    }


    // Lista todas las imágenes activas que pertenecen a un producto
    public async Task<Respuesta<IEnumerable<TProductoImagen>>> ListarPorProductoAsync(
        int productoId,
        bool incluirProductoInactivo)
    {
        try
        {
            // Busca el producto utilizando su ID
            var producto = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(
                x => x.ProductoId == productoId &&
                     (incluirProductoInactivo || x.Activo));

            // Verifica que el producto exista
            if (producto.Data == null)
                return Error<IEnumerable<TProductoImagen>>(
                    Mensajes.ProductoNoEncontrado);


            // Busca las imágenes activas relacionadas con el producto
            var respuesta = await _unidadDeTrabajo.TProductoImagen
                .BuscarAsync(
                    x => x.ProductoId == productoId && x.Activo);

            // Verifica si ocurrió un error al buscar las imágenes
            if (!string.IsNullOrEmpty(respuesta.Error))
                return Error<IEnumerable<TProductoImagen>>(
                    "No fue posible cargar las imágenes del producto.");


            // Ordena primero la imagen principal y luego las demás
            var imagenes = (respuesta.Data ?? [])
                .OrderByDescending(x => x.EsPrincipal)
                .ThenBy(x => x.Orden)
                .ThenBy(x => x.ImagenId);


            // Devuelve las imágenes encontradas
            return new Respuesta<IEnumerable<TProductoImagen>>
            {
                Data = _mapper.Map<IEnumerable<TProductoImagen>>(imagenes)
            };
        }
        catch (Exception ex)
        {
            // Registra el error ocurrido
            _logger.LogError(
                ex,
                "Error al listar imágenes para ProductoId {ProductoId}",
                productoId);

            // Devuelve el mensaje de error
            return Error<IEnumerable<TProductoImagen>>(
                "No fue posible cargar las imágenes del producto.");
        }
    }


    // Obtiene la imagen principal de un producto
    public async Task<Respuesta<TProductoImagen?>> ObtenerPrincipalAsync(
        int productoId,
        bool incluirProductoInactivo)
    {
        // Obtiene todas las imágenes ordenadas del producto
        var imagenes = await ListarPorProductoAsync(
            productoId,
            incluirProductoInactivo);

        // Devuelve la primera imagen, que corresponde a la principal
        return !string.IsNullOrEmpty(imagenes.Error)
            ? Error<TProductoImagen?>(imagenes.Error)
            : new Respuesta<TProductoImagen?>
            {
                Data = imagenes.Data?.FirstOrDefault()
            };
    }


    // Agrega una nueva imagen a un producto
    public async Task<Respuesta<TProductoImagen>> AgregarAsync(
        int productoId,
        string urlImagen,
        string? textoAlternativo,
        bool esPrincipal)
    {
        try
        {
            // Busca el producto al que se le agregará la imagen
            var productoRespuesta =
                await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(
                    x => x.ProductoId == productoId);

            // Verifica que el producto exista
            if (productoRespuesta.Data == null)
                return Error<TProductoImagen>(
                    Mensajes.ProductoNoEncontrado);


            // Busca las imágenes que ya tiene el producto
            var imagenesRespuesta =
                await _unidadDeTrabajo.TProductoImagen.BuscarAsync(
                    x => x.ProductoId == productoId && x.Activo);

            // Convierte las imágenes encontradas en una lista
            var imagenes =
                imagenesRespuesta.Data?.ToList() ?? [];


            // Si es la primera imagen, automáticamente será la principal
            if (!imagenes.Any())
                esPrincipal = true;


            // Verifica si la nueva imagen será la principal
            if (esPrincipal)
            {
                // Busca las imágenes que actualmente estén como principal
                foreach (var imagenActual in
                         imagenes.Where(x => x.EsPrincipal))
                {
                    // Quita la imagen como principal
                    imagenActual.EsPrincipal = false;

                    // Guarda el cambio en la base de datos
                    await _unidadDeTrabajo.TProductoImagen
                        .ModificarAsync(imagenActual);
                }
            }


            // Calcula el orden que tendrá la nueva imagen
            var nuevoOrden = imagenes.Any()
                ? imagenes.Max(x => x.Orden) + 1
                : 1;


            // Crea la nueva imagen del producto
            var imagen = new ProductoImagen
            {
                // Relaciona la imagen con el producto
                ProductoId = productoId,

                // Guarda la dirección donde se encuentra la imagen
                UrlImagen = urlImagen,

                // Guarda una descripción opcional
                TextoAlternativo = textoAlternativo,

                // Indica si será la imagen principal
                EsPrincipal = esPrincipal,

                // Guarda el orden de la imagen
                Orden = nuevoOrden,

                // La imagen se crea activa
                Activo = true
            };


            // Guarda la nueva imagen en la base de datos
            var resultado =
                await _unidadDeTrabajo.TProductoImagen
                    .InsertarAsync(imagen);


            // Verifica si ocurrió un error al guardar
            if (!string.IsNullOrEmpty(resultado.Error) ||
                resultado.Data == null)
            {
                return Error<TProductoImagen>(
                    "No fue posible registrar la imagen.");
            }


            // Devuelve la imagen que fue registrada
            return new Respuesta<TProductoImagen>
            {
                Data = _mapper.Map<TProductoImagen>(resultado.Data)
            };
        }
        catch (Exception ex)
        {
            // Registra el error ocurrido
            _logger.LogError(
                ex,
                "Error agregando imagen al producto {ProductoId}",
                productoId);

            // Devuelve un mensaje de error
            return Error<TProductoImagen>(
                "No fue posible registrar la imagen.");
        }
    }


    // Cambia la imagen principal de un producto
    public async Task<Respuesta<bool>> EstablecerPrincipalAsync(
        int imagenId)
    {
        try
        {
            // Busca la imagen que se quiere colocar como principal
            var respuestaImagen =
                await _unidadDeTrabajo.TProductoImagen.ObtenerEntidadAsync(
                    x => x.ImagenId == imagenId && x.Activo);

            // Obtiene la imagen encontrada
            var imagen = respuestaImagen.Data;

            // Verifica que la imagen exista
            if (imagen == null)
                return Error<bool>(
                    "La imagen no existe.");


            // Busca todas las imágenes del mismo producto
            var imagenesRespuesta =
                await _unidadDeTrabajo.TProductoImagen.BuscarAsync(
                    x => x.ProductoId == imagen.ProductoId &&
                         x.Activo);

            // Convierte el resultado en una lista
            var imagenes =
                imagenesRespuesta.Data?.ToList() ?? [];


            // Busca si alguna imagen estaba marcada como principal
            foreach (var item in
                     imagenes.Where(x => x.EsPrincipal))
            {
                // Quita la imagen principal anterior
                item.EsPrincipal = false;

                // Guarda el cambio
                await _unidadDeTrabajo.TProductoImagen
                    .ModificarAsync(item);
            }


            // Coloca la imagen seleccionada como principal
            imagen.EsPrincipal = true;


            // Guarda el cambio en la base de datos
            var resultado =
                await _unidadDeTrabajo.TProductoImagen
                    .ModificarAsync(imagen);


            // Verifica si ocurrió un error
            if (!string.IsNullOrEmpty(resultado.Error))
                return Error<bool>(
                    "No fue posible cambiar la imagen principal.");


            // Indica que el cambio se realizó correctamente
            return new Respuesta<bool>
            {
                Data = true
            };
        }
        catch (Exception ex)
        {
            // Registra el error ocurrido
            _logger.LogError(
                ex,
                "Error cambiando imagen principal {ImagenId}",
                imagenId);

            // Devuelve un mensaje de error
            return Error<bool>(
                "No fue posible cambiar la imagen principal.");
        }
    }


    // Elimina una imagen de un producto
    public async Task<Respuesta<TProductoImagen?>> EliminarAsync(
        int imagenId)
    {
        try
        {
            // Busca la imagen que se quiere eliminar
            var respuestaImagen =
                await _unidadDeTrabajo.TProductoImagen.ObtenerEntidadAsync(
                    x => x.ImagenId == imagenId);

            // Obtiene la imagen encontrada
            var imagen = respuestaImagen.Data;

            // Verifica que la imagen exista
            if (imagen == null)
                return Error<TProductoImagen?>(
                    "La imagen no existe.");


            // Guarda si la imagen eliminada era la principal
            bool eraPrincipal = imagen.EsPrincipal;

            // Guarda el ID del producto
            int productoId = imagen.ProductoId;

            // Guarda una copia antes de eliminarla
            var copia =
                _mapper.Map<TProductoImagen>(imagen);


            // Elimina la imagen de la base de datos
            var resultado =
                await _unidadDeTrabajo.TProductoImagen
                    .EliminarAsync(imagen);


            // Verifica si ocurrió un error al eliminar
            if (!string.IsNullOrEmpty(resultado.Error))
                return Error<TProductoImagen?>(
                    "No fue posible eliminar la imagen.");


            // Verifica si la imagen eliminada era la principal
            if (eraPrincipal)
            {
                // Busca las imágenes restantes del producto
                var restantes =
                    await _unidadDeTrabajo.TProductoImagen.BuscarAsync(
                        x => x.ProductoId == productoId &&
                             x.Activo);

                // Selecciona la siguiente imagen disponible
                var nuevaPrincipal = restantes.Data?
                    .OrderBy(x => x.Orden)
                    .FirstOrDefault();


                // Verifica si todavía existen imágenes
                if (nuevaPrincipal != null)
                {
                    // Coloca otra imagen como principal
                    nuevaPrincipal.EsPrincipal = true;

                    // Guarda el cambio
                    await _unidadDeTrabajo.TProductoImagen
                        .ModificarAsync(nuevaPrincipal);
                }
            }


            // Devuelve la información de la imagen eliminada
            return new Respuesta<TProductoImagen?>
            {
                Data = copia
            };
        }
        catch (Exception ex)
        {
            // Registra el error ocurrido
            _logger.LogError(
                ex,
                "Error eliminando imagen {ImagenId}",
                imagenId);

            // Devuelve un mensaje de error
            return Error<TProductoImagen?>(
                "No fue posible eliminar la imagen.");
        }
    }


    // Método utilizado para devolver los errores de esta clase
    private static Respuesta<T> Error<T>(string mensaje) =>
        new()
        {
            Success = false,
            Error = mensaje
        };
}