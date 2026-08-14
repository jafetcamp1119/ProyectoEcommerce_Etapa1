using System.Data;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// coordina el mantenimiento y el catálogo previo que cada proveedor ofrece a LessPrice
public class ProveedorLN : IProveedorLN
{
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProveedorLN> _logger;
    private readonly IMapper _mapper;

    public ProveedorLN(
        IUnidadTrabajoEF unidadTrabajo,
        IConfiguration configuration,
        ILogger<ProveedorLN> logger,
        IMapper mapper)
    {
        _unidadDeTrabajo = unidadTrabajo;
        _configuration = configuration;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Respuesta<IEnumerable<TProveedor>>> ListarAsync(bool soloActivos = false)
    {
        try
        {
            var respuesta = soloActivos
                ? await _unidadDeTrabajo.TProveedor.BuscarAsync(x => x.Activo)
                : await _unidadDeTrabajo.TProveedor.ListarAsync();
            if (!string.IsNullOrEmpty(respuesta.Error))
                return Error<IEnumerable<TProveedor>>("No fue posible cargar los proveedores.");

            return new Respuesta<IEnumerable<TProveedor>>
            {
                Data = _mapper.Map<IEnumerable<TProveedor>>(respuesta.Data ?? [])
                    .OrderBy(x => x.Nombre)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar proveedores.");
            return Error<IEnumerable<TProveedor>>("No fue posible cargar los proveedores.");
        }
    }

    public async Task<Respuesta<TProveedor>> ObtenerAsync(int proveedorId)
    {
        if (proveedorId <= 0) return Error<TProveedor>("El proveedor no existe.");
        try
        {
            var respuesta = await _unidadDeTrabajo.TProveedor.ObtenerEntidadAsync(
                x => x.ProveedorId == proveedorId);
            return respuesta.Data == null
                ? Error<TProveedor>("El proveedor no existe.")
                : new Respuesta<TProveedor> { Data = _mapper.Map<TProveedor>(respuesta.Data) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ProveedorId {ProveedorId}.", proveedorId);
            return Error<TProveedor>("No fue posible cargar el proveedor.");
        }
    }

    public async Task<Respuesta<TProveedor>> InsertarAsync(TProveedor datos, int administradorId)
    {
        Limpiar(datos);
        var validacion = Validar(datos, administradorId);
        if (validacion != null) return Error<TProveedor>(validacion);

        try
        {
            var duplicado = await BuscarDuplicadoAsync(datos, 0);
            if (duplicado) return Error<TProveedor>("Ya existe un proveedor con el mismo nombre o correo.");

            var entidad = _mapper.Map<Proveedor>(datos);
            entidad.Activo = true;
            entidad.FechaRegistro = DateTime.UtcNow;
            var insercion = await _unidadDeTrabajo.TProveedor.InsertarAsync(entidad);
            if (insercion.Data == null || !string.IsNullOrEmpty(insercion.Error))
                return Error<TProveedor>("No fue posible crear el proveedor.");

            await RegistrarBitacoraAsync(
                administradorId,
                "CREAR_PROVEEDOR",
                entidad.ProveedorId,
                entidad.Nombre);
            return await ObtenerAsync(entidad.ProveedorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el proveedor {Nombre}.", datos.Nombre);
            return Error<TProveedor>("No fue posible crear el proveedor.");
        }
    }

    public async Task<Respuesta<TProveedor>> ModificarAsync(TProveedor datos, int administradorId)
    {
        Limpiar(datos);
        var validacion = Validar(datos, administradorId);
        if (validacion != null) return Error<TProveedor>(validacion);

        try
        {
            var actual = await _unidadDeTrabajo.TProveedor.ObtenerEntidadAsync(
                x => x.ProveedorId == datos.ProveedorId);
            if (actual.Data == null) return Error<TProveedor>("El proveedor no existe.");
            if (await BuscarDuplicadoAsync(datos, datos.ProveedorId))
                return Error<TProveedor>("Ya existe un proveedor con el mismo nombre o correo.");

            var estado = actual.Data.Activo;
            _mapper.Map(datos, actual.Data);
            actual.Data.Activo = estado;
            var modificacion = await _unidadDeTrabajo.TProveedor.ModificarAsync(actual.Data);
            if (modificacion.Data == null || !string.IsNullOrEmpty(modificacion.Error))
                return Error<TProveedor>("No fue posible modificar el proveedor.");

            await RegistrarBitacoraAsync(
                administradorId,
                "MODIFICAR_PROVEEDOR",
                datos.ProveedorId,
                datos.Nombre);
            return await ObtenerAsync(datos.ProveedorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al modificar ProveedorId {ProveedorId}.", datos.ProveedorId);
            return Error<TProveedor>("No fue posible modificar el proveedor.");
        }
    }

    public async Task<Respuesta<TProveedor>> CambiarEstadoAsync(
        int proveedorId,
        bool activo,
        int administradorId)
    {
        if (proveedorId <= 0 || administradorId <= 0)
            return Error<TProveedor>("El proveedor no existe.");

        try
        {
            var actual = await _unidadDeTrabajo.TProveedor.ObtenerEntidadAsync(
                x => x.ProveedorId == proveedorId);
            if (actual.Data == null) return Error<TProveedor>("El proveedor no existe.");

            actual.Data.Activo = activo;
            var modificacion = await _unidadDeTrabajo.TProveedor.ModificarAsync(actual.Data);
            if (modificacion.Data == null || !string.IsNullOrEmpty(modificacion.Error))
                return Error<TProveedor>("No fue posible cambiar el estado del proveedor.");

            await RegistrarBitacoraAsync(
                administradorId,
                activo ? "ACTIVAR_PROVEEDOR" : "DESACTIVAR_PROVEEDOR",
                proveedorId,
                actual.Data.Nombre);
            return await ObtenerAsync(proveedorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar el estado de ProveedorId {ProveedorId}.", proveedorId);
            return Error<TProveedor>("No fue posible cambiar el estado del proveedor.");
        }
    }

    public async Task<Respuesta<IEnumerable<TFamiliaOfertaProveedor>>> ListarFamiliasAsync(
        int proveedorId,
        bool soloDisponibles = false)
    {
        if (proveedorId <= 0) return Error<IEnumerable<TFamiliaOfertaProveedor>>("El proveedor no existe.");
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SELECT relacion.ProveedorId, familia.FamiliaId, familia.Nombre,
                       familia.Descripcion, familia.UrlImagen, familia.Activo
                FROM dbo.ProveedorFamilias relacion
                INNER JOIN dbo.FamiliasProducto familia ON familia.FamiliaId = relacion.FamiliaId
                WHERE relacion.ProveedorId = @ProveedorId AND relacion.Activo = 1
                  AND (@SoloDisponibles = 0 OR familia.Activo = 0)
                ORDER BY familia.Nombre;
                """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.AddWithValue("@SoloDisponibles", soloDisponibles);
            var items = new List<TFamiliaOfertaProveedor>();
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                items.Add(new TFamiliaOfertaProveedor
                {
                    ProveedorId = lector.GetInt32(0),
                    FamiliaId = lector.GetInt32(1),
                    Nombre = lector.GetString(2),
                    Descripcion = lector.IsDBNull(3) ? null : lector.GetString(3),
                    UrlImagen = lector.IsDBNull(4) ? null : lector.GetString(4),
                    Incorporada = lector.GetBoolean(5)
                });
            }
            return new Respuesta<IEnumerable<TFamiliaOfertaProveedor>> { Data = items };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar familias del ProveedorId {ProveedorId}.", proveedorId);
            return Error<IEnumerable<TFamiliaOfertaProveedor>>("No fue posible cargar las familias del proveedor.");
        }
    }

    public async Task<Respuesta<IEnumerable<TCategoriaOfertaProveedor>>> ListarCategoriasAsync(
        int proveedorId,
        int? familiaId,
        bool soloDisponibles = false)
    {
        if (proveedorId <= 0) return Error<IEnumerable<TCategoriaOfertaProveedor>>("El proveedor no existe.");
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = soloDisponibles
                ? """
                    SELECT @ProveedorId, categoria.FamiliaId, categoria.CategoriaId,
                           familia.Nombre, categoria.Nombre, categoria.Descripcion,
                           categoria.UrlImagen, CONVERT(bit, 0)
                    FROM dbo.Categorias categoria
                    INNER JOIN dbo.FamiliasProducto familia
                        ON familia.FamiliaId = categoria.FamiliaId
                    WHERE categoria.Activo = 1
                      AND familia.Activo = 1
                      AND (@FamiliaId IS NULL OR categoria.FamiliaId = @FamiliaId)
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM dbo.ProveedorCategorias relacion
                          WHERE relacion.ProveedorId = @ProveedorId
                            AND relacion.CategoriaId = categoria.CategoriaId
                            AND relacion.Activo = 1
                      )
                    ORDER BY familia.Nombre, categoria.Nombre;
                    """
                : """
                    SELECT relacion.ProveedorId, categoria.FamiliaId, categoria.CategoriaId,
                           familia.Nombre, categoria.Nombre, categoria.Descripcion,
                           categoria.UrlImagen, relacion.Activo
                    FROM dbo.ProveedorCategorias relacion
                    INNER JOIN dbo.Categorias categoria
                        ON categoria.CategoriaId = relacion.CategoriaId
                    INNER JOIN dbo.FamiliasProducto familia
                        ON familia.FamiliaId = categoria.FamiliaId
                    WHERE relacion.ProveedorId = @ProveedorId
                      AND relacion.Activo = 1
                      AND (@FamiliaId IS NULL OR categoria.FamiliaId = @FamiliaId)
                    ORDER BY familia.Nombre, categoria.Nombre;
                    """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.Add("@FamiliaId", SqlDbType.Int).Value = (object?)familiaId ?? DBNull.Value;
            var items = new List<TCategoriaOfertaProveedor>();
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                items.Add(new TCategoriaOfertaProveedor
                {
                    ProveedorId = lector.GetInt32(0),
                    FamiliaId = lector.GetInt32(1),
                    CategoriaId = lector.GetInt32(2),
                    FamiliaNombre = lector.GetString(3),
                    Nombre = lector.GetString(4),
                    Descripcion = lector.IsDBNull(5) ? null : lector.GetString(5),
                    UrlImagen = lector.IsDBNull(6) ? null : lector.GetString(6),
                    Incorporada = lector.GetBoolean(7)
                });
            }
            return new Respuesta<IEnumerable<TCategoriaOfertaProveedor>> { Data = items };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar categorías del ProveedorId {ProveedorId}.", proveedorId);
            return Error<IEnumerable<TCategoriaOfertaProveedor>>("No fue posible cargar las categorías del proveedor.");
        }
    }

    public async Task<Respuesta<IEnumerable<TProductoOfertaProveedor>>> ListarProductosAsync(
        int proveedorId,
        int? categoriaId,
        bool soloDisponibles = false)
    {
        if (proveedorId <= 0) return Error<IEnumerable<TProductoOfertaProveedor>>("El proveedor no existe.");
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SELECT oferta.ProductoProveedorCatalogoId, proveedor.ProveedorId, proveedor.Nombre,
                       familia.FamiliaId, familia.Nombre, categoria.CategoriaId, categoria.Nombre,
                       oferta.Nombre, oferta.PrecioCompra, oferta.ImpuestoId, impuesto.Nombre,
                       impuesto.Porcentaje, oferta.ProductoId, producto.Codigo,
                       COALESCE(producto.Stock, 0), oferta.Activo
                FROM dbo.ProductosProveedorCatalogo oferta
                INNER JOIN dbo.ProveedorCategorias relacion
                    ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
                INNER JOIN dbo.Proveedores proveedor ON proveedor.ProveedorId = relacion.ProveedorId
                INNER JOIN dbo.Categorias categoria ON categoria.CategoriaId = relacion.CategoriaId
                INNER JOIN dbo.FamiliasProducto familia ON familia.FamiliaId = categoria.FamiliaId
                INNER JOIN dbo.Impuestos impuesto ON impuesto.ImpuestoId = oferta.ImpuestoId
                LEFT JOIN dbo.Productos producto ON producto.ProductoId = oferta.ProductoId
                WHERE proveedor.ProveedorId = @ProveedorId
                  AND relacion.Activo = 1
                  AND (@CategoriaId IS NULL OR categoria.CategoriaId = @CategoriaId)
                  AND (@SoloDisponibles = 0 OR (oferta.ProductoId IS NULL AND oferta.Activo = 1))
                ORDER BY familia.Nombre, categoria.Nombre, oferta.Nombre;
                """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.Add("@CategoriaId", SqlDbType.Int).Value = (object?)categoriaId ?? DBNull.Value;
            comando.Parameters.AddWithValue("@SoloDisponibles", soloDisponibles);
            var items = new List<TProductoOfertaProveedor>();
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                items.Add(new TProductoOfertaProveedor
                {
                    ProductoProveedorCatalogoId = lector.GetInt32(0),
                    ProveedorId = lector.GetInt32(1),
                    ProveedorNombre = lector.GetString(2),
                    FamiliaId = lector.GetInt32(3),
                    FamiliaNombre = lector.GetString(4),
                    CategoriaId = lector.GetInt32(5),
                    CategoriaNombre = lector.GetString(6),
                    Nombre = lector.GetString(7),
                    PrecioCompra = lector.GetDecimal(8),
                    ImpuestoId = lector.GetInt32(9),
                    ImpuestoNombre = lector.GetString(10),
                    ImpuestoPorcentaje = lector.GetDecimal(11),
                    ProductoId = lector.IsDBNull(12) ? null : lector.GetInt32(12),
                    Codigo = lector.IsDBNull(13) ? null : lector.GetString(13),
                    Stock = lector.GetInt32(14),
                    Activo = lector.GetBoolean(15)
                });
            }
            return new Respuesta<IEnumerable<TProductoOfertaProveedor>> { Data = items };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar productos del ProveedorId {ProveedorId}.", proveedorId);
            return Error<IEnumerable<TProductoOfertaProveedor>>("No fue posible cargar los productos del proveedor.");
        }
    }

    public async Task<Respuesta<IEnumerable<TProductoExistenteProveedor>>> ListarProductosExistentesAsync(
        int proveedorId,
        int categoriaId)
    {
        if (proveedorId <= 0 || categoriaId <= 0)
            return Error<IEnumerable<TProductoExistenteProveedor>>("El proveedor o la categoría no es válido.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.ProveedorCategorias relacion
                    INNER JOIN dbo.Proveedores proveedor
                        ON proveedor.ProveedorId = relacion.ProveedorId
                    WHERE relacion.ProveedorId = @ProveedorId
                      AND relacion.CategoriaId = @CategoriaId
                      AND relacion.Activo = 1
                )
                    THROW 51210, 'La categoría no está relacionada con el proveedor.', 1;

                SELECT producto.ProductoId, producto.Codigo, producto.Nombre,
                       producto.ImpuestoId, impuesto.Nombre, producto.Activo
                FROM dbo.Productos producto
                INNER JOIN dbo.Impuestos impuesto
                    ON impuesto.ImpuestoId = producto.ImpuestoId
                WHERE producto.CategoriaId = @CategoriaId
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.ProductosProveedorCatalogo oferta
                      INNER JOIN dbo.ProveedorCategorias relacion
                          ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
                      WHERE relacion.ProveedorId = @ProveedorId
                        AND oferta.ProductoId = producto.ProductoId
                  )
                ORDER BY producto.Nombre;
                """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.AddWithValue("@CategoriaId", categoriaId);

            var productos = new List<TProductoExistenteProveedor>();
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                productos.Add(new TProductoExistenteProveedor
                {
                    ProductoId = lector.GetInt32(0),
                    Codigo = lector.GetString(1),
                    Nombre = lector.GetString(2),
                    ImpuestoId = lector.GetInt32(3),
                    ImpuestoNombre = lector.GetString(4),
                    Activo = lector.GetBoolean(5)
                });
            }

            return new Respuesta<IEnumerable<TProductoExistenteProveedor>> { Data = productos };
        }
        catch (SqlException ex) when (ex.Number == 51210)
        {
            return Error<IEnumerable<TProductoExistenteProveedor>>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al listar productos existentes para ProveedorId {ProveedorId} y CategoriaId {CategoriaId}.",
                proveedorId,
                categoriaId);
            return Error<IEnumerable<TProductoExistenteProveedor>>(
                "No fue posible cargar los productos existentes.");
        }
    }

    public async Task<Respuesta<TCategoriaProveedorResultado>> AsociarCategoriaExistenteAsync(
        int proveedorId,
        int categoriaId,
        int administradorId)
    {
        if (proveedorId <= 0 || categoriaId <= 0 || administradorId <= 0)
            return Error<TCategoriaProveedorResultado>("Los datos de la categoría no son válidos.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SET XACT_ABORT ON;
                SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                BEGIN TRANSACTION;

                BEGIN TRY
                    IF NOT EXISTS
                    (
                        SELECT 1 FROM dbo.Proveedores WITH (UPDLOCK, HOLDLOCK)
                        WHERE ProveedorId = @ProveedorId
                    )
                        THROW 51211, 'El proveedor no existe.', 1;

                    DECLARE @FamiliaId INT;
                    DECLARE @Nombre NVARCHAR(80);

                    SELECT @FamiliaId = FamiliaId, @Nombre = Nombre
                    FROM dbo.Categorias WITH (UPDLOCK, HOLDLOCK)
                    WHERE CategoriaId = @CategoriaId;

                    IF @FamiliaId IS NULL
                        THROW 51212, 'La categoría seleccionada no existe.', 1;

                    IF EXISTS
                    (
                        SELECT 1 FROM dbo.ProveedorFamilias
                        WHERE ProveedorId = @ProveedorId AND FamiliaId = @FamiliaId
                    )
                        UPDATE dbo.ProveedorFamilias
                        SET Activo = 1
                        WHERE ProveedorId = @ProveedorId AND FamiliaId = @FamiliaId;
                    ELSE
                        INSERT dbo.ProveedorFamilias (ProveedorId, FamiliaId, Activo)
                        VALUES (@ProveedorId, @FamiliaId, 1);

                    IF EXISTS
                    (
                        SELECT 1 FROM dbo.ProveedorCategorias
                        WHERE ProveedorId = @ProveedorId AND CategoriaId = @CategoriaId
                    )
                        UPDATE dbo.ProveedorCategorias
                        SET Activo = 1
                        WHERE ProveedorId = @ProveedorId AND CategoriaId = @CategoriaId;
                    ELSE
                        INSERT dbo.ProveedorCategorias (ProveedorId, CategoriaId, Activo)
                        VALUES (@ProveedorId, @CategoriaId, 1);

                    INSERT dbo.BitacoraSistema
                        (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
                    VALUES
                        (@UsuarioId, SYSDATETIME(), N'ASOCIAR_CATEGORIA_PROVEEDOR',
                         N'Categoria', CONVERT(NVARCHAR(80), @CategoriaId),
                         CONCAT(N'Proveedor: ', @ProveedorId));

                    COMMIT TRANSACTION;

                    SELECT @CategoriaId, @FamiliaId, @Nombre, CONVERT(bit, 1);
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;
                """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.AddWithValue("@CategoriaId", categoriaId);
            comando.Parameters.AddWithValue("@UsuarioId", administradorId);

            return await LeerCategoriaProveedorAsync(comando);
        }
        catch (SqlException ex) when (ex.Number is >= 51211 and <= 51212)
        {
            return Error<TCategoriaProveedorResultado>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asociar CategoriaId {CategoriaId} al proveedor.", categoriaId);
            return Error<TCategoriaProveedorResultado>("No fue posible asociar la categoría.");
        }
    }

    public async Task<Respuesta<TCategoriaProveedorResultado>> CrearCategoriaAsync(
        int proveedorId,
        TCategoriaNuevaProveedor datos,
        int administradorId)
    {
        datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
        datos.Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion)
            ? null
            : datos.Descripcion.Trim();
        if (proveedorId <= 0 || administradorId <= 0 || datos.FamiliaId <= 0 ||
            string.IsNullOrWhiteSpace(datos.Nombre))
            return Error<TCategoriaProveedorResultado>("Los datos de la categoría no son válidos.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SET XACT_ABORT ON;
                SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                BEGIN TRANSACTION;

                BEGIN TRY
                    IF NOT EXISTS
                    (
                        SELECT 1 FROM dbo.Proveedores WITH (UPDLOCK, HOLDLOCK)
                        WHERE ProveedorId = @ProveedorId
                    )
                        THROW 51211, 'El proveedor no existe.', 1;

                    IF NOT EXISTS
                    (
                        SELECT 1 FROM dbo.FamiliasProducto WITH (UPDLOCK, HOLDLOCK)
                        WHERE FamiliaId = @FamiliaId AND Activo = 1
                    )
                        THROW 51213, 'La familia seleccionada no está disponible.', 1;

                    DECLARE @CategoriaId INT;
                    DECLARE @YaExistia BIT = 1;

                    SELECT @CategoriaId = CategoriaId
                    FROM dbo.Categorias WITH (UPDLOCK, HOLDLOCK)
                    WHERE FamiliaId = @FamiliaId
                      AND Nombre = @Nombre;

                    IF @CategoriaId IS NULL
                    BEGIN
                        INSERT dbo.Categorias
                            (FamiliaId, Nombre, Descripcion, UrlImagen, Activo)
                        VALUES
                            (@FamiliaId, @Nombre, @Descripcion, NULL, 1);

                        SET @CategoriaId = CONVERT(INT, SCOPE_IDENTITY());
                        SET @YaExistia = 0;
                    END;

                    IF EXISTS
                    (
                        SELECT 1 FROM dbo.ProveedorFamilias
                        WHERE ProveedorId = @ProveedorId AND FamiliaId = @FamiliaId
                    )
                        UPDATE dbo.ProveedorFamilias
                        SET Activo = 1
                        WHERE ProveedorId = @ProveedorId AND FamiliaId = @FamiliaId;
                    ELSE
                        INSERT dbo.ProveedorFamilias (ProveedorId, FamiliaId, Activo)
                        VALUES (@ProveedorId, @FamiliaId, 1);

                    IF EXISTS
                    (
                        SELECT 1 FROM dbo.ProveedorCategorias
                        WHERE ProveedorId = @ProveedorId AND CategoriaId = @CategoriaId
                    )
                        UPDATE dbo.ProveedorCategorias
                        SET Activo = 1
                        WHERE ProveedorId = @ProveedorId AND CategoriaId = @CategoriaId;
                    ELSE
                        INSERT dbo.ProveedorCategorias (ProveedorId, CategoriaId, Activo)
                        VALUES (@ProveedorId, @CategoriaId, 1);

                    INSERT dbo.BitacoraSistema
                        (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
                    VALUES
                        (@UsuarioId, SYSDATETIME(),
                         CASE WHEN @YaExistia = 1
                              THEN N'ASOCIAR_CATEGORIA_PROVEEDOR'
                              ELSE N'CREAR_CATEGORIA_PROVEEDOR' END,
                         N'Categoria', CONVERT(NVARCHAR(80), @CategoriaId),
                         CONCAT(N'Proveedor: ', @ProveedorId));

                    COMMIT TRANSACTION;

                    SELECT @CategoriaId, @FamiliaId, @Nombre, @YaExistia;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;
                """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.AddWithValue("@FamiliaId", datos.FamiliaId);
            comando.Parameters.AddWithValue("@Nombre", datos.Nombre);
            comando.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 250).Value =
                (object?)datos.Descripcion ?? DBNull.Value;
            comando.Parameters.AddWithValue("@UsuarioId", administradorId);

            return await LeerCategoriaProveedorAsync(comando);
        }
        catch (SqlException ex) when (ex.Number is >= 51211 and <= 51213)
        {
            return Error<TCategoriaProveedorResultado>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear la categoría {Nombre} desde el proveedor.", datos.Nombre);
            return Error<TCategoriaProveedorResultado>("No fue posible crear la categoría.");
        }
    }

    public async Task<Respuesta<TProductoOfertaProveedor>> AgregarProductoExistenteAsync(
        int proveedorId,
        int categoriaId,
        TAgregarProductoExistenteProveedor datos,
        int administradorId)
    {
        if (proveedorId <= 0 || categoriaId <= 0 || datos.ProductoId <= 0 ||
            datos.PrecioCompra <= 0 || administradorId <= 0)
            return Error<TProductoOfertaProveedor>("Los datos del producto no son válidos.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SET XACT_ABORT ON;
                SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                BEGIN TRANSACTION;

                BEGIN TRY
                    DECLARE @ProveedorCategoriaId INT;
                    DECLARE @Nombre NVARCHAR(120);
                    DECLARE @ImpuestoId INT;
                    DECLARE @OfertaId INT;

                    SELECT @ProveedorCategoriaId = relacion.ProveedorCategoriaId
                    FROM dbo.ProveedorCategorias relacion WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Proveedores proveedor
                        ON proveedor.ProveedorId = relacion.ProveedorId
                    WHERE relacion.ProveedorId = @ProveedorId
                      AND relacion.CategoriaId = @CategoriaId
                      AND relacion.Activo = 1;

                    IF @ProveedorCategoriaId IS NULL
                        THROW 51220, 'La categoría no está relacionada con el proveedor.', 1;

                    SELECT @Nombre = producto.Nombre,
                           @ImpuestoId = producto.ImpuestoId
                    FROM dbo.Productos producto WITH (UPDLOCK, HOLDLOCK)
                    WHERE producto.ProductoId = @ProductoId
                      AND producto.CategoriaId = @CategoriaId;

                    IF @Nombre IS NULL
                        THROW 51221, 'El producto no existe dentro de la categoría seleccionada.', 1;

                    SELECT @OfertaId = ProductoProveedorCatalogoId
                    FROM dbo.ProductosProveedorCatalogo WITH (UPDLOCK, HOLDLOCK)
                    WHERE ProveedorCategoriaId = @ProveedorCategoriaId
                      AND ProductoId = @ProductoId;

                    IF @OfertaId IS NULL
                    BEGIN
                        SELECT @OfertaId = ProductoProveedorCatalogoId
                        FROM dbo.ProductosProveedorCatalogo WITH (UPDLOCK, HOLDLOCK)
                        WHERE ProveedorCategoriaId = @ProveedorCategoriaId
                          AND ProductoId IS NULL
                          AND Nombre = @Nombre;
                    END;

                    IF @OfertaId IS NULL
                    BEGIN
                        INSERT dbo.ProductosProveedorCatalogo
                            (ProveedorCategoriaId, ImpuestoId, Nombre, PrecioCompra,
                             ProductoId, Activo, FechaActualizacion)
                        VALUES
                            (@ProveedorCategoriaId, @ImpuestoId, @Nombre, @PrecioCompra,
                             @ProductoId, 1, SYSDATETIME());
                        SET @OfertaId = CONVERT(INT, SCOPE_IDENTITY());
                    END
                    ELSE
                    BEGIN
                        UPDATE dbo.ProductosProveedorCatalogo
                        SET ImpuestoId = @ImpuestoId,
                            PrecioCompra = @PrecioCompra,
                            ProductoId = @ProductoId,
                            Activo = 1,
                            FechaActualizacion = SYSDATETIME()
                        WHERE ProductoProveedorCatalogoId = @OfertaId;
                    END;

                    IF EXISTS
                    (
                        SELECT 1 FROM dbo.ProductoProveedor
                        WHERE ProductoId = @ProductoId AND ProveedorId = @ProveedorId
                    )
                        UPDATE dbo.ProductoProveedor
                        SET PrecioCompra = @PrecioCompra, Activo = 1
                        WHERE ProductoId = @ProductoId AND ProveedorId = @ProveedorId;
                    ELSE
                        INSERT dbo.ProductoProveedor
                            (ProductoId, ProveedorId, PrecioCompra, Activo)
                        VALUES
                            (@ProductoId, @ProveedorId, @PrecioCompra, 1);

                    INSERT dbo.BitacoraSistema
                        (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
                    VALUES
                        (@UsuarioId, SYSDATETIME(), N'ASOCIAR_PRODUCTO_PROVEEDOR',
                         N'Producto', CONVERT(NVARCHAR(80), @ProductoId),
                         CONCAT(N'Proveedor: ', @ProveedorId));

                    COMMIT TRANSACTION;
                    SELECT @OfertaId;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;
                """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.AddWithValue("@CategoriaId", categoriaId);
            comando.Parameters.AddWithValue("@ProductoId", datos.ProductoId);
            comando.Parameters.AddWithValue("@PrecioCompra", datos.PrecioCompra);
            comando.Parameters.AddWithValue("@UsuarioId", administradorId);

            var ofertaId = Convert.ToInt32(await comando.ExecuteScalarAsync());
            return await ObtenerOfertaAsync(ofertaId);
        }
        catch (SqlException ex) when (ex.Number is >= 51220 and <= 51221)
        {
            return Error<TProductoOfertaProveedor>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al relacionar ProductoId {ProductoId} con el proveedor.", datos.ProductoId);
            return Error<TProductoOfertaProveedor>("No fue posible relacionar el producto existente.");
        }
    }

    public async Task<Respuesta<TProductoOfertaProveedor>> CrearProductoAsync(
        int proveedorId,
        int categoriaId,
        TCrearProductoProveedor datos,
        int administradorId)
    {
        datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
        if (proveedorId <= 0 || categoriaId <= 0 || datos.ImpuestoId <= 0 ||
            datos.PrecioCompra <= 0 || administradorId <= 0 ||
            string.IsNullOrWhiteSpace(datos.Nombre))
            return Error<TProductoOfertaProveedor>("Los datos del producto no son válidos.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SET XACT_ABORT ON;
                SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                BEGIN TRANSACTION;

                BEGIN TRY
                    DECLARE @ProveedorCategoriaId INT;

                    SELECT @ProveedorCategoriaId = relacion.ProveedorCategoriaId
                    FROM dbo.ProveedorCategorias relacion WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Proveedores proveedor
                        ON proveedor.ProveedorId = relacion.ProveedorId
                    WHERE relacion.ProveedorId = @ProveedorId
                      AND relacion.CategoriaId = @CategoriaId
                      AND relacion.Activo = 1;

                    IF @ProveedorCategoriaId IS NULL
                        THROW 51220, 'La categoría no está relacionada con el proveedor.', 1;

                    IF NOT EXISTS
                    (
                        SELECT 1 FROM dbo.Impuestos WITH (UPDLOCK, HOLDLOCK)
                        WHERE ImpuestoId = @ImpuestoId AND Activo = 1
                    )
                        THROW 51222, 'El impuesto seleccionado no está disponible.', 1;

                    IF EXISTS
                    (
                        SELECT 1 FROM dbo.Productos WITH (UPDLOCK, HOLDLOCK)
                        WHERE CategoriaId = @CategoriaId AND Nombre = @Nombre
                    )
                        THROW 51223, 'El producto ya existe en LessPrice. Utilice la opción de producto existente.', 1;

                    IF EXISTS
                    (
                        SELECT 1 FROM dbo.ProductosProveedorCatalogo WITH (UPDLOCK, HOLDLOCK)
                        WHERE ProveedorCategoriaId = @ProveedorCategoriaId
                          AND Nombre = @Nombre
                    )
                        THROW 51224, 'El producto ya existe en el catálogo de este proveedor.', 1;

                    INSERT dbo.ProductosProveedorCatalogo
                        (ProveedorCategoriaId, ImpuestoId, Nombre, PrecioCompra,
                         ProductoId, Activo, FechaActualizacion)
                    VALUES
                        (@ProveedorCategoriaId, @ImpuestoId, @Nombre, @PrecioCompra,
                         NULL, @Activo, SYSDATETIME());

                    DECLARE @OfertaId INT = CONVERT(INT, SCOPE_IDENTITY());

                    INSERT dbo.BitacoraSistema
                        (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
                    VALUES
                        (@UsuarioId, SYSDATETIME(), N'CREAR_PRODUCTO_PROVEEDOR',
                         N'ProductoProveedorCatalogo', CONVERT(NVARCHAR(80), @OfertaId),
                         CONCAT(N'Proveedor: ', @ProveedorId));

                    COMMIT TRANSACTION;
                    SELECT @OfertaId;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;
                """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.AddWithValue("@CategoriaId", categoriaId);
            comando.Parameters.AddWithValue("@ImpuestoId", datos.ImpuestoId);
            comando.Parameters.AddWithValue("@Nombre", datos.Nombre);
            comando.Parameters.AddWithValue("@PrecioCompra", datos.PrecioCompra);
            comando.Parameters.AddWithValue("@Activo", datos.Activo);
            comando.Parameters.AddWithValue("@UsuarioId", administradorId);

            var ofertaId = Convert.ToInt32(await comando.ExecuteScalarAsync());
            return await ObtenerOfertaAsync(ofertaId);
        }
        catch (SqlException ex) when (ex.Number is >= 51220 and <= 51224)
        {
            return Error<TProductoOfertaProveedor>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el producto {Nombre} en el catálogo del proveedor.", datos.Nombre);
            return Error<TProductoOfertaProveedor>("No fue posible crear el producto del proveedor.");
        }
    }

    public async Task<Respuesta<TProductoOfertaProveedor>> ModificarProductoAsync(
        int ofertaId,
        TModificarProductoProveedor datos,
        int administradorId)
    {
        if (ofertaId <= 0 || datos.PrecioCompra <= 0 || datos.ImpuestoId <= 0 ||
            administradorId <= 0)
            return Error<TProductoOfertaProveedor>("Los datos del producto no son válidos.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                BEGIN TRY
                    IF NOT EXISTS
                    (
                        SELECT 1 FROM dbo.Impuestos
                        WHERE ImpuestoId = @ImpuestoId AND Activo = 1
                    )
                        THROW 51222, 'El impuesto seleccionado no está disponible.', 1;

                    DECLARE @ProductoId INT;
                    DECLARE @ProveedorId INT;

                    SELECT @ProductoId = oferta.ProductoId,
                           @ProveedorId = relacion.ProveedorId
                    FROM dbo.ProductosProveedorCatalogo oferta WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.ProveedorCategorias relacion
                        ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
                    WHERE oferta.ProductoProveedorCatalogoId = @OfertaId;

                    IF @ProveedorId IS NULL
                        THROW 51225, 'El producto del proveedor no existe.', 1;

                    UPDATE dbo.ProductosProveedorCatalogo
                    SET PrecioCompra = @PrecioCompra,
                        ImpuestoId = @ImpuestoId,
                        Activo = @Activo,
                        FechaActualizacion = SYSDATETIME()
                    WHERE ProductoProveedorCatalogoId = @OfertaId;

                    IF @ProductoId IS NOT NULL
                    BEGIN
                        UPDATE dbo.ProductoProveedor
                        SET PrecioCompra = @PrecioCompra,
                            Activo = @Activo
                        WHERE ProductoId = @ProductoId
                          AND ProveedorId = @ProveedorId;

                        UPDATE dbo.Productos
                        SET ImpuestoId = @ImpuestoId
                        WHERE ProductoId = @ProductoId;
                    END;

                    INSERT dbo.BitacoraSistema
                        (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
                    VALUES
                        (@UsuarioId, SYSDATETIME(), N'MODIFICAR_PRODUCTO_PROVEEDOR',
                         N'ProductoProveedorCatalogo', CONVERT(NVARCHAR(80), @OfertaId),
                         CONCAT(N'Proveedor: ', @ProveedorId));

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;
                """;
            comando.Parameters.AddWithValue("@OfertaId", ofertaId);
            comando.Parameters.AddWithValue("@PrecioCompra", datos.PrecioCompra);
            comando.Parameters.AddWithValue("@ImpuestoId", datos.ImpuestoId);
            comando.Parameters.AddWithValue("@Activo", datos.Activo);
            comando.Parameters.AddWithValue("@UsuarioId", administradorId);
            await comando.ExecuteNonQueryAsync();

            return await ObtenerOfertaAsync(ofertaId);
        }
        catch (SqlException ex) when (ex.Number is 51222 or 51225)
        {
            return Error<TProductoOfertaProveedor>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al modificar la oferta {OfertaId}.", ofertaId);
            return Error<TProductoOfertaProveedor>("No fue posible modificar el producto del proveedor.");
        }
    }

    public Task<Respuesta<bool>> IncorporarFamiliaAsync(int proveedorId, int familiaId, int administradorId) =>
        IncorporarCatalogoAsync(proveedorId, familiaId, administradorId, true);

    public Task<Respuesta<bool>> IncorporarCategoriaAsync(int proveedorId, int categoriaId, int administradorId) =>
        IncorporarCatalogoAsync(proveedorId, categoriaId, administradorId, false);

    public async Task<Respuesta<TProductoIncorporadoProveedor>> IncorporarProductoAsync(
        int ofertaId,
        TIncorporarProductoProveedor datos,
        int administradorId)
    {
        if (ofertaId <= 0 || administradorId <= 0 || datos.StockMinimo < 0)
            return Error<TProductoIncorporadoProveedor>("Los datos para incorporar el producto no son válidos.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandType = CommandType.StoredProcedure;
            comando.CommandText = "dbo.sp_IncorporarProductoProveedor";
            comando.Parameters.AddWithValue("@ProductoProveedorCatalogoId", ofertaId);
            comando.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 500).Value =
                string.IsNullOrWhiteSpace(datos.Descripcion) ? DBNull.Value : datos.Descripcion.Trim();
            comando.Parameters.AddWithValue("@StockMinimo", datos.StockMinimo);
            comando.Parameters.AddWithValue("@UsuarioId", administradorId);
            await using var lector = await comando.ExecuteReaderAsync();
            if (!await lector.ReadAsync())
                return Error<TProductoIncorporadoProveedor>("No fue posible incorporar el producto.");

            return new Respuesta<TProductoIncorporadoProveedor>
            {
                Data = new TProductoIncorporadoProveedor
                {
                    ProductoId = lector.GetInt32(0),
                    Codigo = lector.GetString(1),
                    Nombre = lector.GetString(2),
                    PrecioCompra = lector.GetDecimal(3),
                    PrecioVenta = lector.GetDecimal(4),
                    Stock = lector.GetInt32(5)
                }
            };
        }
        catch (SqlException ex) when (ex.Number is >= 51001 and <= 51005)
        {
            return Error<TProductoIncorporadoProveedor>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al incorporar la oferta {OfertaId}.", ofertaId);
            return Error<TProductoIncorporadoProveedor>("No fue posible incorporar el producto.");
        }
    }

    private static async Task<Respuesta<TCategoriaProveedorResultado>> LeerCategoriaProveedorAsync(
        SqlCommand comando)
    {
        await using var lector = await comando.ExecuteReaderAsync();
        if (!await lector.ReadAsync())
            return Error<TCategoriaProveedorResultado>("No fue posible guardar la categoría del proveedor.");

        return new Respuesta<TCategoriaProveedorResultado>
        {
            Data = new TCategoriaProveedorResultado
            {
                CategoriaId = lector.GetInt32(0),
                FamiliaId = lector.GetInt32(1),
                Nombre = lector.GetString(2),
                YaExistia = lector.GetBoolean(3)
            }
        };
    }

    private async Task<Respuesta<TProductoOfertaProveedor>> ObtenerOfertaAsync(int ofertaId)
    {
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SELECT relacion.ProveedorId, relacion.CategoriaId
                FROM dbo.ProductosProveedorCatalogo oferta
                INNER JOIN dbo.ProveedorCategorias relacion
                    ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
                WHERE oferta.ProductoProveedorCatalogoId = @OfertaId;
                """;
            comando.Parameters.AddWithValue("@OfertaId", ofertaId);
            await using var lector = await comando.ExecuteReaderAsync();
            if (!await lector.ReadAsync())
                return Error<TProductoOfertaProveedor>("El producto del proveedor no existe.");

            var proveedorId = lector.GetInt32(0);
            var categoriaId = lector.GetInt32(1);
            var listado = await ListarProductosAsync(proveedorId, categoriaId);
            if (!string.IsNullOrEmpty(listado.Error))
                return Error<TProductoOfertaProveedor>(listado.Error);

            var oferta = listado.Data?.FirstOrDefault(
                x => x.ProductoProveedorCatalogoId == ofertaId);
            return oferta == null
                ? Error<TProductoOfertaProveedor>("El producto del proveedor no existe.")
                : new Respuesta<TProductoOfertaProveedor> { Data = oferta };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la oferta {OfertaId}.", ofertaId);
            return Error<TProductoOfertaProveedor>("No fue posible cargar el producto del proveedor.");
        }
    }

    private async Task<Respuesta<bool>> IncorporarCatalogoAsync(
        int proveedorId,
        int catalogoId,
        int administradorId,
        bool familia)
    {
        if (proveedorId <= 0 || catalogoId <= 0 || administradorId <= 0)
            return Error<bool>("Los datos no son válidos.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            await using var transaccion = await conexion.BeginTransactionAsync();
            var tablaRelacion = familia ? "dbo.ProveedorFamilias" : "dbo.ProveedorCategorias";
            var tablaCatalogo = familia ? "dbo.FamiliasProducto" : "dbo.Categorias";
            var columnaId = familia ? "FamiliaId" : "CategoriaId";
            var comando = conexion.CreateCommand();
            comando.Transaction = (SqlTransaction)transaccion;
            comando.CommandText = $"""
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM {tablaRelacion} relacion
                    INNER JOIN dbo.Proveedores proveedor ON proveedor.ProveedorId = relacion.ProveedorId
                    WHERE relacion.ProveedorId = @ProveedorId
                      AND relacion.{columnaId} = @CatalogoId
                      AND relacion.Activo = 1
                      AND proveedor.Activo = 1
                )
                    THROW 51201, 'El elemento no está disponible mediante este proveedor.', 1;

                UPDATE {tablaCatalogo} SET Activo = 1 WHERE {columnaId} = @CatalogoId;

                INSERT dbo.BitacoraSistema (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
                VALUES (@UsuarioId, SYSDATETIME(), @Accion, @Entidad,
                        CONVERT(NVARCHAR(80), @CatalogoId), CONCAT(N'Proveedor: ', @ProveedorId));
                """;
            comando.Parameters.AddWithValue("@ProveedorId", proveedorId);
            comando.Parameters.AddWithValue("@CatalogoId", catalogoId);
            comando.Parameters.AddWithValue("@UsuarioId", administradorId);
            comando.Parameters.AddWithValue("@Accion", familia ? "INCORPORAR_FAMILIA" : "INCORPORAR_CATEGORIA");
            comando.Parameters.AddWithValue("@Entidad", familia ? "FamiliaProducto" : "Categoria");
            await comando.ExecuteNonQueryAsync();
            await transaccion.CommitAsync();
            return new Respuesta<bool> { Data = true };
        }
        catch (SqlException ex) when (ex.Number == 51201)
        {
            return Error<bool>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al incorporar un catálogo ofrecido por ProveedorId {ProveedorId}.", proveedorId);
            return Error<bool>("No fue posible incorporar el elemento al catálogo.");
        }
    }

    private async Task<bool> BuscarDuplicadoAsync(TProveedor datos, int proveedorId)
    {
        var respuesta = await _unidadDeTrabajo.TProveedor.ObtenerEntidadAsync(x =>
            x.ProveedorId != proveedorId &&
            (x.Nombre == datos.Nombre ||
             (datos.Correo != null && x.Correo == datos.Correo)));
        return respuesta.Data != null;
    }

    private async Task RegistrarBitacoraAsync(
        int administradorId,
        string accion,
        int proveedorId,
        string detalle)
    {
        await _unidadDeTrabajo.TBitacoraSistema.InsertarAsync(new BitacoraSistema
        {
            UsuarioId = administradorId,
            Fecha = DateTime.UtcNow,
            Accion = accion,
            Entidad = "Proveedor",
            EntidadId = proveedorId.ToString(),
            Detalle = detalle
        });
    }

    private static string? Validar(TProveedor datos, int administradorId)
    {
        if (administradorId <= 0) return "La sesión del administrador no es válida.";
        if (string.IsNullOrWhiteSpace(datos.Nombre)) return "El nombre es obligatorio.";
        if (datos.Nombre.Length > 150 || datos.Correo?.Length > 150 ||
            datos.Telefono?.Length > 30 || datos.Direccion?.Length > 250)
            return "Uno de los datos del proveedor supera la longitud permitida.";
        return null;
    }

    private static void Limpiar(TProveedor datos)
    {
        datos.Nombre = (datos.Nombre ?? string.Empty).Trim();
        datos.Correo = string.IsNullOrWhiteSpace(datos.Correo)
            ? null
            : datos.Correo.Trim().ToLowerInvariant();
        datos.Telefono = string.IsNullOrWhiteSpace(datos.Telefono) ? null : datos.Telefono.Trim();
        datos.Direccion = string.IsNullOrWhiteSpace(datos.Direccion) ? null : datos.Direccion.Trim();
        datos.UrlImagen = string.IsNullOrWhiteSpace(datos.UrlImagen) ? null : datos.UrlImagen.Trim();
    }

    private string CadenaConexion() => _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No existe la conexión DefaultConnection.");

    private static string MensajeSql(SqlException ex) =>
        ex.Message.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)[0];

    private static Respuesta<T> Error<T>(string mensaje) =>
        new() { Success = false, Error = mensaje };
}
