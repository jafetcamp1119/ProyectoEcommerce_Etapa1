using AutoMapper;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

/// <summary>Consulta imágenes activas y determina la imagen principal de cada producto.</summary>
public class ProductoImagenLN : IProductoImagenLN
{
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductoImagenLN> _logger;

    public ProductoImagenLN(IUnidadTrabajoEF unidadDeTrabajo, IMapper mapper, ILogger<ProductoImagenLN> logger)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Lista imágenes ordenadas, ocultando productos inactivos al Cliente.</summary>
    public async Task<Respuesta<IEnumerable<TProductoImagen>>> ListarPorProductoAsync(int productoId, bool incluirProductoInactivo)
    {
        try
        {
            var producto = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(
                x => x.ProductoId == productoId && (incluirProductoInactivo || x.Activo));
            if (producto.Data == null)
                return Error<IEnumerable<TProductoImagen>>(Mensajes.ProductoNoEncontrado);

            var respuesta = await _unidadDeTrabajo.TProductoImagen
                .BuscarAsync(x => x.ProductoId == productoId && x.Activo);
            if (!string.IsNullOrEmpty(respuesta.Error))
                return Error<IEnumerable<TProductoImagen>>("No fue posible cargar las imágenes del producto.");

            var imagenes = (respuesta.Data ?? [])
                .OrderByDescending(x => x.EsPrincipal)
                .ThenBy(x => x.Orden)
                .ThenBy(x => x.ImagenId);

            return new Respuesta<IEnumerable<TProductoImagen>>
            {
                Data = _mapper.Map<IEnumerable<TProductoImagen>>(imagenes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar imágenes para ProductoId {ProductoId}", productoId);
            return Error<IEnumerable<TProductoImagen>>("No fue posible cargar las imágenes del producto.");
        }
    }

    /// <summary>Devuelve primero la marcada como principal y luego respeta el orden configurado.</summary>
    public async Task<Respuesta<TProductoImagen?>> ObtenerPrincipalAsync(int productoId, bool incluirProductoInactivo)
    {
        var imagenes = await ListarPorProductoAsync(productoId, incluirProductoInactivo);
        return !string.IsNullOrEmpty(imagenes.Error)
            ? Error<TProductoImagen?>(imagenes.Error)
            : new Respuesta<TProductoImagen?> { Data = imagenes.Data?.FirstOrDefault() };
    }

    private static Respuesta<T> Error<T>(string mensaje) => new() { Success = false, Error = mensaje };
}
