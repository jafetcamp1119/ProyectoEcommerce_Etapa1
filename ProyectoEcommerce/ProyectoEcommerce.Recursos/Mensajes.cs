namespace ProyectoEcommerce.Recursos;


// esta clase guarda en un solo lugar los mensajes que se usan varias veces en el sistema
// asi no hay que escribir el mismo texto en cada LN Controller o validacion
// tambien ayuda a que si algun dia se cambia un mensaje solo haya que cambiarlo aqui
public static class Mensajes
{

    // ========================= MENSAJES GENERALES =========================

    // se usa cuando ya existe otro registro con los mismos datos
    public const string RegistroDuplicado =
        "Ya existe un registro con los datos indicados.";

    // aparece cuando se intenta modificar algo que ya no existe
    public const string RegistroNoExisteModificar =
        "No existe el registro a modificar.";

    // aparece cuando se intenta eliminar algo que no existe
    public const string RegistroNoExisteEliminar =
        "No existe el registro a eliminar.";

    // mensaje general cuando una busqueda no encuentra lo que se estaba buscando
    public const string RegistroNoEncontrado =
        "Registro no encontrado.";

    // mensaje general para operaciones que fallaron y no tienen uno mas especifico
    public const string ErrorOperacion =
        "No fue posible completar la operación.";



    // ========================= AUTENTICACION Y USUARIOS =========================

    // se usa cuando alguien intenta registrar un correo que ya pertenece a otra cuenta
    public const string CorreoDuplicado =
        "El correo ya se encuentra registrado.";

    // se muestra cuando el correo o la contraseña del login no coinciden
    // se deja un mensaje general para no decir cual de los 2 datos estaba incorrecto
    public const string CredencialesIncorrectas =
        "Correo o contraseña incorrectos";

    // confirma que el registro del usuario termino correctamente
    public const string CuentaCreada =
        "La cuenta fue creada correctamente";

    // error general si el proceso de iniciar sesion falla
    public const string ErrorAutenticacion =
        "No fue posible iniciar sesión.";

    // error general si no se pudo crear una cuenta
    public const string ErrorRegistro =
        "No fue posible crear la cuenta.";

    // se usa cuando un nombre obligatorio viene vacio
    public const string NombreObligatorio =
        "El nombre es obligatorio.";

    // aparece cuando la persona fallo demasiadas veces el login y la cuenta queda bloqueada
    public const string AccesoBloqueado =
        "Se alcanzó el límite de intentos permitidos. Intenta nuevamente más tarde.";



    // ========================= ROLES =========================

    // se usa si el RolId recibido no existe o el rol ya no esta activo
    public const string RolNoEncontrado =
        "El rol seleccionado no existe o no está disponible.";

    // evita quitar o desactivar al ultimo administrador que queda en el sistema
    public const string UltimoAdministrador =
        "La operación no está permitida porque debe existir al menos otro Administrador activo.";



    // ========================= CONFIGURACION INICIAL =========================

    // aparece si alguien intenta usar otra vez la configuracion inicial despues de crear el primer admin
    public const string ConfiguracionInicialNoDisponible =
        "La configuración inicial de LessPrice ya fue completada.";

    // mensaje general si algo falla mientras se crea el primer administrador
    public const string ErrorConfiguracionInicial =
        "No fue posible completar la configuración inicial de LessPrice.";



    // ========================= FAMILIAS Y CATEGORIAS =========================

    // se muestra cuando no se escogio una familia valida
    public const string FamiliaObligatoria =
        "Debe seleccionar una familia válida.";

    // se usa cuando el id de la familia no existe en la BD
    public const string FamiliaNoEncontrada =
        "La familia seleccionada no existe.";

    // se usa cuando la categoria escogida para un producto no existe o ya no esta disponible
    public const string CategoriaProductoNoEncontrada =
        "La categoría seleccionada no existe o no está disponible.";



    // ========================= IMPUESTOS Y FECHAS =========================

    // evita guardar porcentajes fuera del rango permitido
    public const string PorcentajeInvalido =
        "El porcentaje debe estar entre 0 y 100.";

    // evita que una fecha final quede antes de la fecha donde empieza algo
    public const string FechasInvalidas =
        "La fecha final no puede ser anterior a la fecha inicial.";

    // se usa si el impuesto que se intento asignar al producto no existe o esta inactivo
    public const string ImpuestoProductoNoEncontrado =
        "El impuesto seleccionado no existe o no está disponible.";



    // ========================= PRODUCTOS =========================

    // aparece si se busca un producto que no existe
    public const string ProductoNoEncontrado =
        "El producto solicitado no existe.";

    // evita que 2 productos tengan exactamente el mismo codigo
    public const string CodigoProductoDuplicado =
        "Ya existe un producto con el código indicado.";

    // se usa cuando intentan guardar un producto sin codigo
    public const string CodigoProductoObligatorio =
        "El código del producto es obligatorio.";

    // evita guardar precios costos stock o cantidades con numeros negativos
    public const string ValoresProductoInvalidos =
        "Los precios, el costo y las cantidades no pueden ser negativos.";

    // valida los filtros de precio para que el maximo no quede debajo del minimo
    public const string RangoPreciosInvalido =
        "El precio máximo no puede ser menor que el precio mínimo.";

    // error general cuando no se pudo obtener la lista de productos
    public const string ErrorProductos =
        "No fue posible cargar los productos.";

    // confirma que los datos del producto se pudieron guardar
    public const string ProductoGuardado =
        "La información del producto fue guardada correctamente.";



    // ========================= CARRITO =========================

    // no deja agregar un producto que no existe o que ya esta desactivado
    public const string ProductoNoDisponibleCarrito =
        "El producto no existe o no está disponible para comprar.";

    // aparece cuando la cantidad pedida es mayor a lo que queda en inventario
    public const string StockInsuficienteCarrito =
        "No hay stock suficiente para agregar esa cantidad.";

    // evita cantidades como 0 o numeros negativos dentro del carrito
    public const string CantidadCarritoInvalida =
        "La cantidad debe ser mayor que cero.";

    // se usa cuando no se pudo sacar correctamente el UsuarioId de la sesion actual
    public const string SesionInvalidaCarrito =
        "No fue posible identificar al usuario autenticado.";

    // error general si falla alguna accion del carrito
    public const string ErrorCarrito =
        "No fue posible actualizar el carrito.";

    // confirma que el producto entro correctamente al carrito
    public const string ProductoAgregadoCarrito =
        "El producto fue agregado al carrito.";

    // aparece si intentan modificar o eliminar una linea que no pertenece al carrito abierto del usuario
    public const string DetalleCarritoNoEncontrado =
        "El producto no pertenece a tu carrito abierto.";

}
