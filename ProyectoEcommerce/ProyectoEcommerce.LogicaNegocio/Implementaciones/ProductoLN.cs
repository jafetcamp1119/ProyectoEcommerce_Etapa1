using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// aqui se manejan las consultas del catalogo y el mantenimiento de productos
// las relaciones se traen juntas para mostrar familia, categoria, impuesto e imagen principal
public class ProductoLN : IProductoLN
{
    private static readonly List<string> Relaciones = ["Categoria.Familia", "Impuesto", "Imagenes"];
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;
    private readonly ILogger<ProductoLN> _logger;
    private readonly IMapper _mapper;
    private readonly IDescuentoLN _descuentoLN;

    public ProductoLN(IUnidadTrabajoEF unidadTrabajo, ILogger<ProductoLN> logger, IMapper mapper, IDescuentoLN descuentoLN)
    {
        _unidadDeTrabajo = unidadTrabajo;
        _logger = logger;
        _mapper = mapper;
        _descuentoLN = descuentoLN;
    }

    // recibe filtros del Cliente, revisa la ruta Familia -> Categoria y devuelve una pagina de productos activos
    public async Task<Respuesta<TPagina<TProductoCatalogo>>> ListarCatalogoAsync(TFiltroProductos filtro)
    {
        // primero limpia y limita los filtros antes de usarlos para armar una consulta
        var validacion = NormalizarFiltro(filtro);
        if (validacion != null) return Error<TPagina<TProductoCatalogo>>(validacion);

        try
        {
            var validacionAlcance = await ValidarAlcanceCatalogoAsync(filtro);
            if (validacionAlcance != null)
                return Error<TPagina<TProductoCatalogo>>(validacionAlcance);

            // el predicado es la condicion completa que Entity Framework convierte a WHERE
            var predicado = ConstruirFiltro(filtro, false);
            // cuenta todas las coincidencias para que Angular sepa cuantas paginas existen
            var total = await _unidadDeTrabajo.TProducto.ContarAsync(predicado);
            if (!string.IsNullOrEmpty(total.Error)) return Error<TPagina<TProductoCatalogo>>(Mensajes.ErrorProductos);

            // Skip y Take se aplican en el repositorio para traer solo la pagina solicitada
            var pagina = await _unidadDeTrabajo.TProducto.BuscarPaginadoAsync(
                predicado,
                CrearOrden(filtro.Orden),
                (filtro.Pagina - 1) * filtro.TamanoPagina,
                filtro.TamanoPagina,
                Relaciones);
            if (!string.IsNullOrEmpty(pagina.Error)) return Error<TPagina<TProductoCatalogo>>(Mensajes.ErrorProductos);

            // convierte las entidades a los datos seguros del catalogo y luego agrega el mejor descuento
            var items = _mapper.Map<IEnumerable<TProductoCatalogo>>(pagina.Data ?? []).ToList();
            var descuentos = await _descuentoLN.ObtenerMejoresDescuentosAsync(items.Select(x => x.ProductoId));
            if (!string.IsNullOrEmpty(descuentos.Error) || descuentos.Data == null)
                return Error<TPagina<TProductoCatalogo>>(Mensajes.ErrorProductos);
            // TryGetValue encuentra cada descuento por ID sin recorrer la lista completa
            foreach (var item in items)
                if (descuentos.Data.TryGetValue(item.ProductoId, out var descuento)) item.Descuento = descuento;

            return new Respuesta<TPagina<TProductoCatalogo>>
            {
                Data = new TPagina<TProductoCatalogo>
                {
                    Items = items,
                    Pagina = filtro.Pagina,
                    TamanoPagina = filtro.TamanoPagina,
                    Total = total.Data ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar el catálogo de productos.");
            return Error<TPagina<TProductoCatalogo>>(Mensajes.ErrorProductos);
        }
    }

    // hace la misma paginacion para Administracion pero deja filtrar activos e inactivos
    public async Task<Respuesta<TPagina<TProducto>>> ListarAdministracionAsync(TFiltroProductos filtro)
    {
        var validacion = NormalizarFiltro(filtro);
        if (validacion != null) return Error<TPagina<TProducto>>(validacion);

        try
        {
            // true cambia la parte del filtro que controla el estado del producto
            var predicado = ConstruirFiltro(filtro, true);
            var total = await _unidadDeTrabajo.TProducto.ContarAsync(predicado);
            if (!string.IsNullOrEmpty(total.Error)) return Error<TPagina<TProducto>>(Mensajes.ErrorProductos);

            var pagina = await _unidadDeTrabajo.TProducto.BuscarPaginadoAsync(
                predicado,
                CrearOrden(filtro.Orden),
                (filtro.Pagina - 1) * filtro.TamanoPagina,
                filtro.TamanoPagina,
                Relaciones);
            if (!string.IsNullOrEmpty(pagina.Error)) return Error<TPagina<TProducto>>(Mensajes.ErrorProductos);

            var items = _mapper.Map<IEnumerable<TProducto>>(pagina.Data ?? []).ToList();
            var descuentos = await _descuentoLN.ObtenerMejoresDescuentosAsync(items.Select(x => x.ProductoId));
            if (!string.IsNullOrEmpty(descuentos.Error) || descuentos.Data == null)
                return Error<TPagina<TProducto>>(Mensajes.ErrorProductos);
            foreach (var item in items)
                if (descuentos.Data.TryGetValue(item.ProductoId, out var descuento)) item.Descuento = descuento;

            return new Respuesta<TPagina<TProducto>>
            {
                Data = new TPagina<TProducto>
                {
                    Items = items,
                    Pagina = filtro.Pagina,
                    TamanoPagina = filtro.TamanoPagina,
                    Total = total.Data ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar productos para administración.");
            return Error<TPagina<TProducto>>(Mensajes.ErrorProductos);
        }
    }

    // trae el detalle de un producto solo si tambien estan activas su categoria y su familia
    public async Task<Respuesta<TProductoCatalogo>> ObtenerCatalogoAsync(int productoId)
    {
        try
        {
            // la condicion protege la navegacion completa, no basta con que el producto este activo
            var respuesta = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(
                x => x.ProductoId == productoId && x.Activo && x.Categoria.Activo && x.Categoria.Familia.Activo,
                Relaciones);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TProductoCatalogo>(Mensajes.ErrorProductos);
            if (respuesta.Data == null) return Error<TProductoCatalogo>(Mensajes.ProductoNoEncontrado);
            var producto = _mapper.Map<TProductoCatalogo>(respuesta.Data);
            var descuento = await _descuentoLN.ObtenerMejorDescuentoAsync(productoId);
            if (!string.IsNullOrEmpty(descuento.Error) || descuento.Data == null) return Error<TProductoCatalogo>(Mensajes.ErrorProductos);
            producto.Descuento = descuento.Data;
            return new Respuesta<TProductoCatalogo> { Data = producto };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el producto público {ProductoId}.", productoId);
            return Error<TProductoCatalogo>(Mensajes.ErrorProductos);
        }
    }

    // trae un producto por ID para editarlo aunque este inactivo
    public async Task<Respuesta<TProducto>> ObtenerAdministracionAsync(int productoId)
    {
        try
        {
            var respuesta = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(
                x => x.ProductoId == productoId,
                Relaciones);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TProducto>(Mensajes.ErrorProductos);
            if (respuesta.Data == null) return Error<TProducto>(Mensajes.ProductoNoEncontrado);
            var producto = _mapper.Map<TProducto>(respuesta.Data);
            var descuento = await _descuentoLN.ObtenerMejorDescuentoAsync(productoId);
            if (!string.IsNullOrEmpty(descuento.Error) || descuento.Data == null) return Error<TProducto>(Mensajes.ErrorProductos);
            producto.Descuento = descuento.Data;
            return new Respuesta<TProducto> { Data = producto };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el producto administrativo {ProductoId}.", productoId);
            return Error<TProducto>(Mensajes.ErrorProductos);
        }
    }

    // junta familias, categorias e impuestos activos para llenar los select del formulario
    public async Task<Respuesta<TCatalogosProducto>> ListarCatalogosAsync()
    {
        try
        {
            var familias = await _unidadDeTrabajo.TFamiliaProducto.BuscarAsync(x => x.Activo);
            // una categoria solo aparece si ella y su familia estan activas
            var categorias = await _unidadDeTrabajo.TCategoria.BuscarAsync(x => x.Activo && x.Familia.Activo, ["Familia"]);
            var impuestos = await _unidadDeTrabajo.TImpuesto.BuscarAsync(x => x.Activo);
            if (!string.IsNullOrEmpty(familias.Error) || !string.IsNullOrEmpty(categorias.Error) || !string.IsNullOrEmpty(impuestos.Error))
                return Error<TCatalogosProducto>(Mensajes.ErrorOperacion);

            return new Respuesta<TCatalogosProducto>
            {
                Data = new TCatalogosProducto
                {
                    Familias = _mapper.Map<IEnumerable<TFamiliaProducto>>(familias.Data ?? []).OrderBy(x => x.Nombre),
                    Categorias = _mapper.Map<IEnumerable<TCategoria>>(categorias.Data ?? []).OrderBy(x => x.Nombre),
                    Impuestos = _mapper.Map<IEnumerable<TImpuesto>>(impuestos.Data ?? []).OrderBy(x => x.Nombre)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar catálogos para productos.");
            return Error<TCatalogosProducto>(Mensajes.ErrorOperacion);
        }
    }

    // recibe el formulario, valida relaciones y codigo unico, guarda y registra al Administrador
    public async Task<Respuesta<TProducto>> InsertarAsync(TProducto datos, int administradorId)
    {
        try
        {
            Limpiar(datos);
            var validacion = await ValidarAsync(datos, 0);
            if (validacion != null) return Error<TProducto>(validacion);

            // AutoMapper copia los campos editables y la fecha se pone aqui desde el servidor
            var entidad = _mapper.Map<Producto>(datos);
            entidad.FechaCreacion = DateTime.UtcNow;
            var insercion = await _unidadDeTrabajo.TProducto.InsertarAsync(entidad);
            if (insercion.Data == null || !string.IsNullOrEmpty(insercion.Error))
                return Error<TProducto>(Mensajes.ErrorOperacion);

            await RegistrarBitacora(administradorId, "CREAR_PRODUCTO", entidad.ProductoId, $"Código: {entidad.Codigo}");
            return await ObtenerAdministracionAsync(entidad.ProductoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar el producto {Codigo}.", datos.Codigo);
            return Error<TProducto>(Mensajes.ErrorOperacion);
        }
    }

    // busca el producto, valida los datos nuevos y los copia sobre la entidad que ya existe
    public async Task<Respuesta<TProducto>> ModificarAsync(TProducto datos, int administradorId)
    {
        try
        {
            Limpiar(datos);
            var actual = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(x => x.ProductoId == datos.ProductoId);
            if (actual.Data == null) return Error<TProducto>(Mensajes.ProductoNoEncontrado);

            var validacion = await ValidarAsync(datos, datos.ProductoId);
            if (validacion != null) return Error<TProducto>(validacion);

            // esta version de Map actualiza la entidad sin tocar ID, fecha ni navegaciones ignoradas
            _mapper.Map(datos, actual.Data);
            var actualizacion = await _unidadDeTrabajo.TProducto.ModificarAsync(actual.Data);
            if (actualizacion.Data == null || !string.IsNullOrEmpty(actualizacion.Error))
                return Error<TProducto>(Mensajes.ErrorOperacion);

            await RegistrarBitacora(administradorId, "MODIFICAR_PRODUCTO", datos.ProductoId, $"Código: {datos.Codigo}");
            return await ObtenerAdministracionAsync(datos.ProductoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al modificar ProductoId {ProductoId}.", datos.ProductoId);
            return Error<TProducto>(Mensajes.ErrorOperacion);
        }
    }

    // activa o desactiva sin borrar el producto ni su historial
    public async Task<Respuesta<TProducto>> CambiarEstadoAsync(int productoId, bool activo, int administradorId)
    {
        try
        {
            var actual = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(x => x.ProductoId == productoId);
            if (actual.Data == null) return Error<TProducto>(Mensajes.ProductoNoEncontrado);

            actual.Data.Activo = activo;
            var actualizacion = await _unidadDeTrabajo.TProducto.ModificarAsync(actual.Data);
            if (actualizacion.Data == null || !string.IsNullOrEmpty(actualizacion.Error))
                return Error<TProducto>(Mensajes.ErrorOperacion);

            await RegistrarBitacora(administradorId, activo ? "ACTIVAR_PRODUCTO" : "DESACTIVAR_PRODUCTO", productoId, null);
            return await ObtenerAdministracionAsync(productoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar el estado de ProductoId {ProductoId}.", productoId);
            return Error<TProducto>(Mensajes.ErrorOperacion);
        }
    }

    // revisa textos, numeros, categoria, impuesto y codigo unico
    // devuelve el primer mensaje encontrado o null cuando se puede guardar
    private async Task<string?> ValidarAsync(TProducto datos, int productoId)
    {
        if (string.IsNullOrWhiteSpace(datos.Codigo)) return Mensajes.CodigoProductoObligatorio;
        if (string.IsNullOrWhiteSpace(datos.Nombre)) return Mensajes.NombreObligatorio;
        if (datos.Codigo.Length > 50 || datos.Nombre.Length > 120 || datos.Descripcion?.Length > 500)
            return Mensajes.ErrorOperacion;
        if (datos.PrecioVenta < 0 || datos.Costo < 0 || datos.Stock < 0 || datos.StockMinimo < 0)
            return Mensajes.ValoresProductoInvalidos;

        // no deja asociar un producto nuevo a catalogos desactivados
        var categoria = await _unidadDeTrabajo.TCategoria.ObtenerEntidadAsync(x => x.CategoriaId == datos.CategoriaId && x.Activo);
        if (categoria.Data == null) return Mensajes.CategoriaProductoNoEncontrada;
        var impuesto = await _unidadDeTrabajo.TImpuesto.ObtenerEntidadAsync(x => x.ImpuestoId == datos.ImpuestoId && x.Activo);
        if (impuesto.Data == null) return Mensajes.ImpuestoProductoNoEncontrado;
        // excluye el ID actual para que al editar se pueda conservar el mismo codigo
        var duplicado = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(
            x => x.Codigo == datos.Codigo && x.ProductoId != productoId);
        return duplicado.Data != null ? Mensajes.CodigoProductoDuplicado : null;
    }

    // confirma en el servidor que familia y categoria existen, estan activas y si pertenecen entre si
    private async Task<string?> ValidarAlcanceCatalogoAsync(TFiltroProductos filtro)
    {
        if (filtro.CategoriaId.HasValue)
        {
            var categoria = await _unidadDeTrabajo.TCategoria.ObtenerEntidadAsync(
                x => x.CategoriaId == filtro.CategoriaId.Value && x.Activo && x.Familia.Activo);
            if (!string.IsNullOrEmpty(categoria.Error)) return Mensajes.ErrorProductos;
            if (categoria.Data == null) return Mensajes.CategoriaProductoNoEncontrada;
            // evita consultar una categoria usando el ID de otra familia en la URL
            if (filtro.FamiliaId.HasValue && categoria.Data.FamiliaId != filtro.FamiliaId.Value)
                return Mensajes.CategoriaProductoNoEncontrada;
            return null;
        }

        if (filtro.FamiliaId.HasValue)
        {
            var familia = await _unidadDeTrabajo.TFamiliaProducto.ObtenerEntidadAsync(
                x => x.FamiliaId == filtro.FamiliaId.Value && x.Activo);
            if (!string.IsNullOrEmpty(familia.Error)) return Mensajes.ErrorProductos;
            if (familia.Data == null) return Mensajes.FamiliaNoEncontrada;
        }

        return null;
    }

    // para Cliente obliga a que producto, categoria y familia esten activos
    // para Administracion deja escoger el estado y conserva los demas filtros
    private static Expression<Func<Producto, bool>> ConstruirFiltro(TFiltroProductos filtro, bool administracion)
    {
        var texto = filtro.Texto ?? string.Empty;
        var disponibilidad = filtro.Disponibilidad ?? string.Empty;
        // esta expresion todavia no consulta la BD, Entity Framework la traduce cuando se usa
        return x =>
            (administracion
                ? (!filtro.Activo.HasValue || x.Activo == filtro.Activo.Value)
                : x.Activo && x.Categoria.Activo && x.Categoria.Familia.Activo) &&
            (texto == string.Empty || x.Nombre.Contains(texto) || x.Codigo.Contains(texto)) &&
            (!filtro.FamiliaId.HasValue || x.Categoria.FamiliaId == filtro.FamiliaId.Value) &&
            (!filtro.CategoriaId.HasValue || x.CategoriaId == filtro.CategoriaId.Value) &&
            (!filtro.PrecioMinimo.HasValue || x.PrecioVenta >= filtro.PrecioMinimo.Value) &&
            (!filtro.PrecioMaximo.HasValue || x.PrecioVenta <= filtro.PrecioMaximo.Value) &&
            (disponibilidad == string.Empty ||
             (disponibilidad == "disponible" && x.Stock > x.StockMinimo) ||
             (disponibilidad == "bajo" && x.Stock > 0 && x.Stock <= x.StockMinimo) ||
             (disponibilidad == "agotado" && x.Stock == 0));
    }

    // convierte la opcion del selector en el OrderBy que se ejecutara en SQL
    // ThenBy ProductoId desempata para que un registro no salte entre paginas
    private static Func<IQueryable<Producto>, IOrderedQueryable<Producto>> CrearOrden(string? orden) => orden switch
    {
        "nombre_desc" => x => x.OrderByDescending(p => p.Nombre).ThenBy(p => p.ProductoId),
        "precio_asc" => x => x.OrderBy(p => p.PrecioVenta).ThenBy(p => p.ProductoId),
        "precio_desc" => x => x.OrderByDescending(p => p.PrecioVenta).ThenBy(p => p.ProductoId),
        "fecha_asc" => x => x.OrderBy(p => p.FechaCreacion).ThenBy(p => p.ProductoId),
        "fecha_desc" => x => x.OrderByDescending(p => p.FechaCreacion).ThenBy(p => p.ProductoId),
        _ => x => x.OrderBy(p => p.Nombre).ThenBy(p => p.ProductoId)
    };

    // limpia textos, arregla pagina y tamaño y rechaza rangos de precio o estados invalidos
    private static string? NormalizarFiltro(TFiltroProductos filtro)
    {
        filtro.Texto = filtro.Texto?.Trim();
        filtro.Disponibilidad = filtro.Disponibilidad?.Trim().ToLowerInvariant();
        filtro.Orden = filtro.Orden?.Trim().ToLowerInvariant();
        filtro.Pagina = Math.Max(1, filtro.Pagina);
        // solo permite los tamaños que muestra la interfaz
        if (!new[] { 25, 50, 75, 100 }.Contains(filtro.TamanoPagina)) filtro.TamanoPagina = 25;
        if (filtro.PrecioMinimo < 0 || filtro.PrecioMaximo < 0) return Mensajes.ValoresProductoInvalidos;
        if (filtro.PrecioMinimo.HasValue && filtro.PrecioMaximo.HasValue && filtro.PrecioMaximo < filtro.PrecioMinimo)
            return Mensajes.RangoPreciosInvalido;
        if (!string.IsNullOrEmpty(filtro.Disponibilidad) && !new[] { "disponible", "bajo", "agotado" }.Contains(filtro.Disponibilidad))
            return Mensajes.ErrorOperacion;
        return null;
    }

    // guarda cual Administrador hizo el cambio y sobre cual producto
    private async Task RegistrarBitacora(int usuarioId, string accion, int productoId, string? detalle)
    {
        var respuesta = await _unidadDeTrabajo.TBitacoraSistema.InsertarAsync(new BitacoraSistema
        {
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow,
            Accion = accion,
            Entidad = "Producto",
            EntidadId = productoId.ToString(),
            Detalle = detalle
        });
        if (!string.IsNullOrEmpty(respuesta.Error))
            _logger.LogWarning("No fue posible registrar la bitácora del producto: {Error}", respuesta.Error);
    }

    // quita espacios y convierte una descripcion vacia en null antes de validar o guardar
    private static void Limpiar(TProducto datos)
    {
        datos.Codigo = (datos.Codigo ?? string.Empty).Trim();
        datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
        datos.Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : datos.Descripcion.Trim();
    }

    private static Respuesta<T> Error<T>(string mensaje) => new() { Success = false, Error = mensaje };
}
