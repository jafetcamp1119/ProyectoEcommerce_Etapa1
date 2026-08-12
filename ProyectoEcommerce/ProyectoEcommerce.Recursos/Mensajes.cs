namespace ProyectoEcommerce.Recursos;

public static class Mensajes
{
    public const string RegistroDuplicado = "Ya existe un registro con los datos indicados.";
    public const string RegistroNoExisteModificar = "No existe el registro a modificar.";
    public const string RegistroNoExisteEliminar = "No existe el registro a eliminar.";
    public const string RegistroNoEncontrado = "Registro no encontrado.";
    public const string CorreoDuplicado = "El correo ya se encuentra registrado.";
    public const string CredencialesIncorrectas = "Correo o contraseña incorrectos";
    public const string CuentaCreada = "La cuenta fue creada correctamente";
    public const string ErrorAutenticacion = "No fue posible iniciar sesión.";
    public const string ErrorRegistro = "No fue posible crear la cuenta.";
    public const string NombreObligatorio = "El nombre es obligatorio.";
    public const string FamiliaObligatoria = "Debe seleccionar una familia válida.";
    public const string FamiliaNoEncontrada = "La familia seleccionada no existe.";
    public const string PorcentajeInvalido = "El porcentaje debe estar entre 0 y 100.";
    public const string FechasInvalidas = "La fecha final no puede ser anterior a la fecha inicial.";
    public const string ErrorOperacion = "No fue posible completar la operación.";
    public const string AccesoBloqueado = "Se alcanzó el límite de intentos permitidos. Intenta nuevamente más tarde.";
    public const string RolNoEncontrado = "El rol seleccionado no existe o no está disponible.";
    public const string UltimoAdministrador = "La operación no está permitida porque debe existir al menos otro Administrador activo.";
    public const string ConfiguracionInicialNoDisponible = "La configuración inicial de LessPrice ya fue completada.";
    public const string CorreoAdministradorInicialNoAutorizado = "El correo indicado no está autorizado para la configuración inicial.";
    public const string ErrorConfiguracionInicial = "No fue posible completar la configuración inicial de LessPrice.";
    public const string CorreoReservadoConfiguracionInicial = "El correo está reservado para la configuración inicial de LessPrice.";
    public const string ProductoNoEncontrado = "El producto solicitado no existe.";
    public const string CodigoProductoDuplicado = "Ya existe un producto con el código indicado.";
    public const string CodigoProductoObligatorio = "El código del producto es obligatorio.";
    public const string CategoriaProductoNoEncontrada = "La categoría seleccionada no existe o no está disponible.";
    public const string ImpuestoProductoNoEncontrado = "El impuesto seleccionado no existe o no está disponible.";
    public const string ValoresProductoInvalidos = "Los precios, el costo y las cantidades no pueden ser negativos.";
    public const string RangoPreciosInvalido = "El precio máximo no puede ser menor que el precio mínimo.";
    public const string ErrorProductos = "No fue posible cargar los productos.";
    public const string ProductoNoDisponibleCarrito = "El producto no existe o no está disponible para comprar.";
    public const string StockInsuficienteCarrito = "No hay stock suficiente para agregar esa cantidad.";
    public const string CantidadCarritoInvalida = "La cantidad debe ser mayor que cero.";
    public const string SesionInvalidaCarrito = "No fue posible identificar al usuario autenticado.";
    public const string ErrorCarrito = "No fue posible actualizar el carrito.";
    public const string ProductoAgregadoCarrito = "El producto fue agregado al carrito.";
    public const string DetalleCarritoNoEncontrado = "El producto no pertenece a tu carrito abierto.";
    public const string ProductoGuardado = "La información del producto fue guardada correctamente.";
}
