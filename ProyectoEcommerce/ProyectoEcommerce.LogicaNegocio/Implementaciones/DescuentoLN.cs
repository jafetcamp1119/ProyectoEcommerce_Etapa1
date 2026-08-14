using AutoMapper;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// aqui se guardan descuentos y se resuelve cual es el mejor para cada producto
// un descuento puede apuntar a producto, categoria, familia o una promocion de producto
public class DescuentoLN : IDescuentoLN
{
    private static readonly string[] TiposPermitidos = ["PRODUCTO", "CATEGORIA", "FAMILIA", "PROMOCIONAL"];
    private static readonly List<string> Relaciones = ["Producto", "Categoria", "Familia"];
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<DescuentoLN> _logger;

    public DescuentoLN(IUnidadTrabajoEF unidadDeTrabajo, IMapper mapper, ILogger<DescuentoLN> logger)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // lista todos los descuentos con su destino y una etiqueta de vigencia calculada al momento
    public async Task<Respuesta<IEnumerable<TDescuento>>> ListarAsync()
    {
        try
        {
            // estas relaciones permiten mostrar el nombre del producto, categoria o familia elegida
            var respuesta = await _unidadDeTrabajo.TDescuento.ListarAsync(Relaciones);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<IEnumerable<TDescuento>>("No fue posible consultar los descuentos.");
            var ahora = DateTime.Now;
            // acomoda los mas recientes primero y Select convierte cada entidad en DTO
            var datos = (respuesta.Data ?? [])
                .OrderByDescending(x => x.FechaInicio)
                .ThenByDescending(x => x.DescuentoId)
                .Select(x => Mapear(x, ahora))
                .ToList();
            return new Respuesta<IEnumerable<TDescuento>> { Data = datos };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar descuentos.");
            return Error<IEnumerable<TDescuento>>("No fue posible consultar los descuentos.");
        }
    }

    // trae un descuento por ID junto con el nombre de su destino
    public async Task<Respuesta<TDescuento>> ObtenerAsync(int descuentoId)
    {
        try
        {
            var respuesta = await _unidadDeTrabajo.TDescuento.ObtenerEntidadAsync(
                x => x.DescuentoId == descuentoId, Relaciones);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TDescuento>(Mensajes.ErrorOperacion);
            // el ternario devuelve error si no existe o mapea la entidad si la encontro
            return respuesta.Data == null
                ? Error<TDescuento>(Mensajes.RegistroNoEncontrado)
                : new Respuesta<TDescuento> { Data = Mapear(respuesta.Data, DateTime.Now) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener DescuentoId {DescuentoId}.", descuentoId);
            return Error<TDescuento>(Mensajes.ErrorOperacion);
        }
    }

    // junta familias, categorias y productos para llenar el formulario de descuentos
    public async Task<Respuesta<TCatalogosDescuento>> ListarCatalogosAsync()
    {
        try
        {
            // incluye destinos inactivos para que un descuento historico siga siendo legible y editable
            var familias = await _unidadDeTrabajo.TFamiliaProducto.ListarAsync();
            var categorias = await _unidadDeTrabajo.TCategoria.ListarAsync(["Familia"]);
            var productos = await _unidadDeTrabajo.TProducto.ListarAsync(["Categoria.Familia"]);
            if (!string.IsNullOrEmpty(familias.Error) || !string.IsNullOrEmpty(categorias.Error) || !string.IsNullOrEmpty(productos.Error))
                return Error<TCatalogosDescuento>(Mensajes.ErrorOperacion);

            return new Respuesta<TCatalogosDescuento>
            {
                Data = new TCatalogosDescuento
                {
                    Familias = _mapper.Map<IEnumerable<TFamiliaProducto>>(familias.Data ?? []).OrderBy(x => x.Nombre),
                    Categorias = _mapper.Map<IEnumerable<TCategoria>>(categorias.Data ?? []).OrderBy(x => x.Nombre),
                    // Select acomoda cada producto con los IDs y nombres que necesitan los filtros dependientes
                    Productos = (productos.Data ?? []).Select(x => new TProductoSelector
                    {
                        ProductoId = x.ProductoId,
                        CategoriaId = x.CategoriaId,
                        FamiliaId = x.Categoria.FamiliaId,
                        Nombre = x.Nombre,
                        CategoriaNombre = x.Categoria.Nombre,
                        FamiliaNombre = x.Categoria.Familia.Nombre
                    }).OrderBy(x => x.Nombre)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar catálogos para descuentos.");
            return Error<TCatalogosDescuento>(Mensajes.ErrorOperacion);
        }
    }

    // normaliza y valida el formulario, guarda el descuento y deja la accion en bitacora
    public async Task<Respuesta<TDescuento>> InsertarAsync(TDescuento datos, int administradorId)
    {
        try
        {
            // deja el tipo en mayusculas y limpia los IDs que no corresponden a ese tipo
            Normalizar(datos);
            var validacion = await ValidarAsync(datos);
            if (validacion != null) return Error<TDescuento>(validacion);

            // crea la entidad dejando solamente una llave de destino con valor
            var entidad = CrearEntidad(datos);
            var respuesta = await _unidadDeTrabajo.TDescuento.InsertarAsync(entidad);
            if (respuesta.Data == null || !string.IsNullOrEmpty(respuesta.Error)) return Error<TDescuento>(Mensajes.ErrorOperacion);
            await RegistrarBitacora(administradorId, "CREAR_DESCUENTO", entidad.DescuentoId, entidad.Nombre);
            return await ObtenerAsync(entidad.DescuentoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el descuento {Nombre}.", datos.Nombre);
            return Error<TDescuento>(Mensajes.ErrorOperacion);
        }
    }

    // busca el descuento, valida los datos nuevos y los copia sobre la entidad actual
    public async Task<Respuesta<TDescuento>> ModificarAsync(TDescuento datos, int administradorId)
    {
        try
        {
            Normalizar(datos);
            var actual = await _unidadDeTrabajo.TDescuento.ObtenerEntidadAsync(x => x.DescuentoId == datos.DescuentoId);
            if (actual.Data == null) return Error<TDescuento>(Mensajes.RegistroNoEncontrado);
            var validacion = await ValidarAsync(datos);
            if (validacion != null) return Error<TDescuento>(validacion);

            // Aplicar se comparte con insertar para que los dos guarden las mismas propiedades
            Aplicar(datos, actual.Data);
            var respuesta = await _unidadDeTrabajo.TDescuento.ModificarAsync(actual.Data);
            if (respuesta.Data == null || !string.IsNullOrEmpty(respuesta.Error)) return Error<TDescuento>(Mensajes.ErrorOperacion);
            await RegistrarBitacora(administradorId, "MODIFICAR_DESCUENTO", datos.DescuentoId, datos.Nombre);
            return await ObtenerAsync(datos.DescuentoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al modificar DescuentoId {DescuentoId}.", datos.DescuentoId);
            return Error<TDescuento>(Mensajes.ErrorOperacion);
        }
    }

    // cambia Activo sin borrar el descuento y devuelve el registro ya actualizado
    public async Task<Respuesta<TDescuento>> CambiarEstadoAsync(int descuentoId, bool activo, int administradorId)
    {
        try
        {
            var actual = await _unidadDeTrabajo.TDescuento.ObtenerEntidadAsync(x => x.DescuentoId == descuentoId);
            if (actual.Data == null) return Error<TDescuento>(Mensajes.RegistroNoEncontrado);
            actual.Data.Activo = activo;
            var respuesta = await _unidadDeTrabajo.TDescuento.ModificarAsync(actual.Data);
            if (respuesta.Data == null || !string.IsNullOrEmpty(respuesta.Error)) return Error<TDescuento>(Mensajes.ErrorOperacion);
            await RegistrarBitacora(administradorId, activo ? "ACTIVAR_DESCUENTO" : "DESACTIVAR_DESCUENTO", descuentoId, actual.Data.Nombre);
            return await ObtenerAsync(descuentoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar el estado de DescuentoId {DescuentoId}.", descuentoId);
            return Error<TDescuento>(Mensajes.ErrorOperacion);
        }
    }

    // reutiliza el calculo de varios productos aunque aqui solo recibe uno
    public async Task<Respuesta<TDescuentoAplicado>> ObtenerMejorDescuentoAsync(int productoId)
    {
        var respuesta = await ObtenerMejoresDescuentosAsync([productoId]);
        if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TDescuentoAplicado>(respuesta.Error);
        return respuesta.Data != null && respuesta.Data.TryGetValue(productoId, out var descuento)
            ? new Respuesta<TDescuentoAplicado> { Data = descuento }
            : Error<TDescuentoAplicado>(Mensajes.ProductoNoEncontrado);
    }

    // recibe varios IDs y devuelve un diccionario para encontrar rapido el descuento de cada producto
    public async Task<Respuesta<IReadOnlyDictionary<int, TDescuentoAplicado>>> ObtenerMejoresDescuentosAsync(IEnumerable<int> productoIds)
    {
        // quita IDs invalidos y repetidos antes de consultar la BD
        var ids = productoIds.Where(x => x > 0).Distinct().ToArray();
        if (ids.Length == 0)
            return new Respuesta<IReadOnlyDictionary<int, TDescuentoAplicado>> { Data = new Dictionary<int, TDescuentoAplicado>() };

        try
        {
            // Contains se convierte en un IN de SQL y trae la categoria de cada producto
            var productosRespuesta = await _unidadDeTrabajo.TProducto.BuscarAsync(
                x => ids.Contains(x.ProductoId), ["Categoria"]);
            if (!string.IsNullOrEmpty(productosRespuesta.Error)) 
            return Error<IReadOnlyDictionary<int, TDescuentoAplicado>>(Mensajes.ErrorProductos);
            var productos = (productosRespuesta.Data ?? []).ToList();
            // saca los IDs relacionados para buscar descuentos de todos los niveles en una consulta
            var categorias = productos.Select(x => x.CategoriaId).Distinct().ToArray();
            var familias = productos.Select(x => x.Categoria.FamiliaId).Distinct().ToArray();
            var ahora = DateTime.Now;
            // deja solo descuentos activos, vigentes y que alcancen alguno de los destinos pedidos
            var descuentosRespuesta = await _unidadDeTrabajo.TDescuento.BuscarAsync(x =>
                x.Activo && x.FechaInicio <= ahora && x.FechaFin >= ahora &&
                ((x.ProductoId.HasValue && ids.Contains(x.ProductoId.Value)) ||
                 (x.CategoriaId.HasValue && categorias.Contains(x.CategoriaId.Value)) ||
                 (x.FamiliaId.HasValue && familias.Contains(x.FamiliaId.Value))));
            if (!string.IsNullOrEmpty(descuentosRespuesta.Error))
            return Error<IReadOnlyDictionary<int, TDescuentoAplicado>>(Mensajes.ErrorOperacion);
            var descuentos = (descuentosRespuesta.Data ?? []).ToList();

            // ToDictionary crea una entrada por producto
            // dentro filtra sus candidatos y ResolucionDescuentos escoge el que deja menor precio
            var resultado = productos.ToDictionary(
                producto => producto.ProductoId,
                producto => ResolucionDescuentos.Calcular(
                    producto.ProductoId,
                    producto.PrecioVenta,
                    descuentos
                        .Where(x =>
                            ((x.TipoDescuento is "PRODUCTO" or "PROMOCIONAL") && x.ProductoId == producto.ProductoId) ||
                            (x.TipoDescuento == "CATEGORIA" && x.CategoriaId == producto.CategoriaId) ||
                            (x.TipoDescuento == "FAMILIA" && x.FamiliaId == producto.Categoria.FamiliaId))
                        .Select(x => new TDescuentoCandidato
                        {
                            DescuentoId = x.DescuentoId,
                            TipoDescuento = x.TipoDescuento,
                            Nombre = x.Nombre,
                            Porcentaje = x.Porcentaje
                        })));
            return new Respuesta<IReadOnlyDictionary<int, TDescuentoAplicado>> { Data = resultado };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resolver descuentos para productos.");
            return Error<IReadOnlyDictionary<int, TDescuentoAplicado>>(Mensajes.ErrorOperacion);
        }
    }

    // revisa datos generales y despues confirma que el destino elegido exista
    // devuelve null cuando todo esta bien
    private async Task<string?> ValidarAsync(TDescuento datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre)) return "El nombre es obligatorio.";
        if (datos.Nombre.Length > 150) return "El nombre no puede superar 150 caracteres.";
        if (!TiposPermitidos.Contains(datos.TipoDescuento)) return "Selecciona un tipo de descuento válido.";
        if (datos.Porcentaje is <= 0m or > 100m) return "El porcentaje debe ser mayor que 0 y menor o igual que 100.";
        if (datos.FechaInicio == default || datos.FechaFin == default) return "Las fechas de inicio y fin son obligatorias.";
        if (datos.FechaFin < datos.FechaInicio) return "La fecha de fin debe ser igual o posterior a la fecha de inicio.";

        // cada tipo debe traer un solo ID y dejar los otros dos vacios
        if (datos.TipoDescuento == "FAMILIA")
        {
            if (!datos.FamiliaId.HasValue || datos.FamiliaId <= 0 || datos.CategoriaId.HasValue || datos.ProductoId.HasValue)
                return "Selecciona únicamente la familia del descuento.";
            var destino = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(x => x.FamiliaId == datos.FamiliaId.Value);
            if (destino.Data == null) return "La familia seleccionada no existe.";
        }
        else if (datos.TipoDescuento == "CATEGORIA")
        {
            if (!datos.CategoriaId.HasValue || datos.CategoriaId <= 0 || datos.FamiliaId.HasValue || datos.ProductoId.HasValue)
                return "Selecciona únicamente la categoría del descuento.";
            var destino = await _unidadDeTrabajo.TCategoria.ObtenerEntidadAsync(x => x.CategoriaId == datos.CategoriaId.Value);
            if (destino.Data == null) return "La categoría seleccionada no existe.";
        }
        else
        {
            if (!datos.ProductoId.HasValue || datos.ProductoId <= 0 || datos.FamiliaId.HasValue || datos.CategoriaId.HasValue)
                return "Selecciona únicamente el producto del descuento.";
            var destino = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(x => x.ProductoId == datos.ProductoId.Value);
            if (destino.Data == null) return "El producto seleccionado no existe.";
        }

        return null;
    }

    // crea una entidad vacia y usa el mismo metodo que aplica una modificacion
    private static Descuento CrearEntidad(TDescuento datos)
    {
        var entidad = new Descuento();
        Aplicar(datos, entidad);
        return entidad;
    }

    // copia los valores del DTO y deja solamente la llave que corresponde al tipo
    private static void Aplicar(TDescuento datos, Descuento entidad)
    {
        entidad.Nombre = datos.Nombre;
        entidad.TipoDescuento = datos.TipoDescuento;
        entidad.Porcentaje = datos.Porcentaje;
        entidad.MontoFijo = null;
        entidad.FechaInicio = datos.FechaInicio;
        entidad.FechaFin = datos.FechaFin;
        entidad.Activo = datos.Activo;
        entidad.FamiliaId = datos.TipoDescuento == "FAMILIA" ? datos.FamiliaId : null;
        entidad.CategoriaId = datos.TipoDescuento == "CATEGORIA" ? datos.CategoriaId : null;
        entidad.ProductoId = datos.TipoDescuento is "PRODUCTO" or "PROMOCIONAL" ? datos.ProductoId : null;
    }

    // convierte la entidad a la respuesta y resuelve el nombre del destino con switch
    private static TDescuento Mapear(Descuento entidad, DateTime ahora) => new()
    {
        DescuentoId = entidad.DescuentoId,
        Nombre = entidad.Nombre,
        TipoDescuento = entidad.TipoDescuento,
        ProductoId = entidad.ProductoId,
        CategoriaId = entidad.CategoriaId,
        FamiliaId = entidad.FamiliaId,
        Porcentaje = entidad.Porcentaje,
        FechaInicio = entidad.FechaInicio,
        FechaFin = entidad.FechaFin,
        Activo = entidad.Activo,
        Destino = entidad.TipoDescuento switch
        {
            "FAMILIA" => entidad.Familia?.Nombre ?? string.Empty,
            "CATEGORIA" => entidad.Categoria?.Nombre ?? string.Empty,
            _ => entidad.Producto?.Nombre ?? string.Empty
        },
        VigenciaActual = ResolucionDescuentos.EstadoVigencia(
            entidad.Activo, entidad.FechaInicio, entidad.FechaFin, ahora)
    };

    // limpia nombre y tipo y borra IDs que no se usan para evitar combinaciones confusas
    private static void Normalizar(TDescuento datos)
    {
        datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
        datos.TipoDescuento = (datos.TipoDescuento ?? string.Empty).Trim().ToUpperInvariant();
        if (datos.TipoDescuento == "FAMILIA")
        {
            datos.CategoriaId = null;
            datos.ProductoId = null;
        }
        else if (datos.TipoDescuento == "CATEGORIA")
        {
            datos.FamiliaId = null;
            datos.ProductoId = null;
        }
        else if (datos.TipoDescuento is "PRODUCTO" or "PROMOCIONAL")
        {
            datos.FamiliaId = null;
            datos.CategoriaId = null;
        }
    }

    // apunta quien creo, modifico, activo o desactivo el descuento
    private async Task RegistrarBitacora(int usuarioId, string accion, int descuentoId, string detalle)
    {
        var respuesta = await _unidadDeTrabajo.TBitacoraSistema.InsertarAsync(new BitacoraSistema
        {
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow,
            Accion = accion,
            Entidad = "Descuento",
            EntidadId = descuentoId.ToString(),
            Detalle = detalle
        });
        if (!string.IsNullOrEmpty(respuesta.Error))
            _logger.LogWarning("No fue posible registrar la bitácora del descuento: {Error}", respuesta.Error);
    }

    private static Respuesta<T> Error<T>(string mensaje) => new() { Success = false, Error = mensaje };
}
