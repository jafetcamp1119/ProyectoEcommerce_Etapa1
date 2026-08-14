import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { ICategoria } from '../../model/ICategoria';
import {
  IFiltroProductos,
  IProducto,
  IProductoCatalogo,
  IProductoGuardar
} from '../../model/IProducto';
import { IProductoImagen } from '../../model/IProductoImagen';

import { AutenticacionService } from '../../services/autenticacion';
import { CarritoService } from '../../services/carrito';
import { ProductoImagenService } from '../../services/producto-imagen';
import { ProductoService } from '../../services/producto';

import { FamiliasCliente } from '../familias-cliente/familias-cliente';


// mezcla la informacion que viene del catalogo
// con algunos datos administrativos que puede tener IProducto
type ProductoVista =
  IProductoCatalogo &
  Partial<IProducto>;


// esta pantalla sirve tanto para el catalogo del Cliente
// como para el mantenimiento completo de productos del Administrador
@Component({
  selector: 'app-producto',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CurrencyPipe,
    DatePipe,
    FamiliasCliente
  ],
  templateUrl: './producto.html',
  styleUrl: './producto.css'
})
export class Producto implements OnInit {

  // ========================= SERVICIOS =========================

  // sirve para crear los formularios reactivos
  private readonly fb = inject(FormBuilder);

  // servicio principal de productos
  private readonly servicio = inject(ProductoService);

  // permite saber quien inicio sesion y cual es su rol
  private readonly autenticacion = inject(AutenticacionService);

  // se usa para pedirle a Angular que actualice la pantalla
  private readonly cdr = inject(ChangeDetectorRef);

  // servicio que permite agregar productos al carrito
  private readonly carrito = inject(CarritoService);

  // permite leer parametros y datos de la ruta actual
  private readonly route = inject(ActivatedRoute);

  // permite cambiar de ruta desde TypeScript
  private readonly router = inject(Router);

  // maneja las imagenes que pertenecen a los productos
  private readonly servicioImagen = inject(ProductoImagenService);


  // ========================= ROL ACTUAL =========================

  // guarda desde el inicio si la persona es Administrador
  readonly esAdmin =
    this.autenticacion.esAdministrador();

  // guarda desde el inicio si la persona es Cliente
  readonly esCliente =
    this.autenticacion.esCliente();



  // tamaños permitidos para mostrar productos por pagina
  // as const hace que TypeScript conserve exactamente estos valores
  readonly tamanos = [
    25,
    50,
    75,
    100
  ] as const;


  // ========================= CATALOGOS Y PRODUCTOS =========================

  // productos que se muestran actualmente en pantalla
  productos: ProductoVista[] = [];


  // familias usadas para filtros y para mostrar nombres
  familias: {
    familiaId: number;
    nombre: string;
  }[] = [];


  // categorias disponibles
  categorias: ICategoria[] = [];


  // impuestos disponibles para crear o modificar productos
  impuestos: {
    impuestoId: number;
    nombre: string;
    porcentaje: number;
  }[] = [];


  // ========================= IMAGENES Y CARRITO =========================

  // guarda los ids de productos cuya imagen fallo al cargar
  // Set evita guardar el mismo id varias veces
  imagenesFallidas = new Set<number>();


  // guarda los productos que se estan agregando al carrito
  // sirve para evitar mandar la misma accion varias veces
  procesandoCarrito = new Set<number>();


  // controla si la lista se esta cargando
  cargando = true;

  // controla si se esta guardando o cambiando un producto
  guardando = false;


  // guarda las imagenes que ya tiene el producto
  imagenesProducto: IProductoImagen[] = [];


  // guarda temporalmente los archivos que selecciona el Administrador
  archivosSeleccionados: File[] = [];


  // guarda las vistas previas de las imagenes antes de subirlas
  previsualizaciones: string[] = [];


  // indica si las nuevas imagenes se estan subiendo
  subiendoImagenes = false;


  // ========================= FORMULARIO Y MENSAJES =========================

  mostrarFormulario = false;

  // 0 significa que se esta creando
  // otro numero significa que se esta editando ese producto
  editandoId = 0;


  pagina = 1;

  tamanoPagina: 25 | 50 | 75 | 100 = 25;

  total = 0;


  mensaje = '';
  error = '';


  // ========================= NAVEGACION DEL CLIENTE =========================

  // familia que viene dentro de la ruta
  familiaRutaId = 0;

  familiaSeleccionadaNombre = '';


  // categoria que viene dentro de la ruta
  categoriaRutaId = 0;

  categoriaSeleccionadaNombre = '';


  // indica desde donde esta viendo productos el Cliente
  // global = busqueda general
  // familia = productos dentro de una familia
  // categoria = productos de una categoria especifica
  alcanceCliente:
    '' |
    'global' |
    'familia' |
    'categoria' = '';


  // ========================= FILTROS =========================

  readonly filtros = this.fb.nonNullable.group({

    texto: [
      '',
      Validators.maxLength(120)
    ],

    familiaId: [''],

    categoriaId: [''],

    precioMinimo: [
      '',
      Validators.min(0)
    ],

    precioMaximo: [
      '',
      Validators.min(0)
    ],

    disponibilidad: [''],

    estado: [''],

    orden: ['nombre_asc']

  });


  // ========================= FORMULARIO DE PRODUCTO =========================

  readonly formulario = this.fb.nonNullable.group({

    codigo: [
      '',
      [
        Validators.required,
        Validators.maxLength(50)
      ]
    ],

    nombre: [
      '',
      [
        Validators.required,
        Validators.maxLength(120)
      ]
    ],

    descripcion: [
      '',
      Validators.maxLength(500)
    ],

    categoriaId: [
      0,
      Validators.min(1)
    ],

    impuestoId: [
      0,
      Validators.min(1)
    ],

    precioVenta: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    costo: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    stock: [
      0,
      [
        Validators.required,
        Validators.min(0),

        // solamente permite numeros enteros
        Validators.pattern(/^\d+$/)
      ]
    ],

    stockMinimo: [
      0,
      [
        Validators.required,
        Validators.min(0),

        // solamente permite numeros enteros
        Validators.pattern(/^\d+$/)
      ]
    ],

    activo: [true]

  });


  // ========================= CATEGORIAS FILTRADAS =========================

  // devuelve solamente las categorias de la familia seleccionada
  get categoriasFiltradas(): ICategoria[] {

    // Number convierte el valor del select a numero
    // si no existe usa 0
    const familiaId =
      Number(
        this.filtros.controls.familiaId.value
      ) || 0;


    // si existe familia filtra sus categorias
    // si no existe devuelve todas
    return familiaId
      ? this.categorias.filter(
        x => x.familiaId === familiaId
      )
      : this.categorias;
  }


  // ========================= TOTAL DE PAGINAS =========================

  get totalPaginas(): number {

    // Math.ceil redondea hacia arriba
    // Math.max evita que el resultado llegue a 0
    return Math.max(
      1,
      Math.ceil(
        this.total / this.tamanoPagina
      )
    );
  }


  // ========================= PRIMER NIVEL DEL CLIENTE =========================

  // decide si en vez de productos se debe mostrar la pantalla de familias
  get mostrarFamiliasCliente(): boolean {

    return this.esCliente &&
      !this.alcanceCliente;
  }


  // ========================= TITULO DEL CLIENTE =========================

  // cambia el titulo segun el lugar desde donde se estan viendo los productos
  get tituloCliente(): string {

    if (this.alcanceCliente === 'global') {
      return 'Resultados en todo el catálogo';
    }


    if (this.alcanceCliente === 'familia') {
      return `Resultados en ${this.familiaSeleccionadaNombre}`;
    }


    return `Productos de ${this.categoriaSeleccionadaNombre}`;
  }


  // ========================= DESCRIPCION DEL CLIENTE =========================

  // cambia la explicacion que aparece debajo del titulo
  get descripcionCliente(): string {

    if (this.alcanceCliente === 'global') {
      return 'Busca y filtra productos activos de todas las familias.';
    }


    if (this.alcanceCliente === 'familia') {
      return 'La búsqueda está limitada a los productos de esta familia.';
    }


    return 'Busca y filtra los productos disponibles en esta categoría.';
  }


  // ========================= RUTA PARA VOLVER =========================

  get rutaRegresoCliente(): any[] {

    // si existe familia vuelve a esa familia
    // si no vuelve al inicio de productos
    return this.familiaRutaId
      ? [
        '/productos/familia',
        this.familiaRutaId
      ]
      : ['/productos'];
  }


  // texto del boton para regresar
  get textoRegresoCliente(): string {

    return this.familiaRutaId
      ? 'Volver a categorías'
      : 'Volver a familias';
  }


  // =========================================================
  // ========================= INICIO =========================
  // =========================================================

  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  // prepara las rutas del Cliente y despues carga los catalogos
  ngOnInit(): void {

    // ========================= RUTA DEL CLIENTE =========================

    if (this.esCliente) {

      // data lee el alcance que fue configurado dentro de las rutas de Angular
      this.alcanceCliente =
        (
          this.route.snapshot.data['alcanceCliente'] ?? ''
        ) as typeof this.alcanceCliente;


      // si no existe alcance se queda mostrando las familias del Cliente
      if (!this.alcanceCliente) {

        this.cargando = false;

        return;
      }


      // busca el parametro texto que puede venir en la URL
      // trim quita espacios al inicio y al final
      const textoInicial =
        this.route.snapshot.queryParamMap
          .get('texto')
          ?.trim() ?? '';


      // coloca ese texto dentro del buscador
      this.filtros.controls.texto
        .setValue(textoInicial);


      // ========================= FAMILIA DE LA RUTA =========================

      // una busqueda global no necesita familia
      if (this.alcanceCliente !== 'global') {

        // busca familiaId dentro de la ruta y lo convierte a numero
        const familiaId =
          Number(
            this.route.snapshot.paramMap.get('familiaId')
          );


        // revisa que sea un entero valido mayor que 0
        if (
          !Number.isInteger(familiaId) ||
          familiaId <= 0
        ) {

          this.cargando = false;

          this.error =
            'La familia solicitada no es válida.';

          return;
        }


        // guarda la familia para usarla despues en los filtros
        this.familiaRutaId = familiaId;


        // el filtro interno tambien recibe la familia
        this.filtros.controls.familiaId
          .setValue(
            String(familiaId)
          );

      }


      // ========================= CATEGORIA DE LA RUTA =========================

      // solamente necesita categoria cuando el alcance es categoria
      if (this.alcanceCliente === 'categoria') {

        const categoriaId =
          Number(
            this.route.snapshot.paramMap.get('categoriaId')
          );


        if (
          !Number.isInteger(categoriaId) ||
          categoriaId <= 0
        ) {

          this.cargando = false;

          this.error =
            'La categoría solicitada no es válida.';

          return;
        }


        this.categoriaRutaId = categoriaId;


        this.filtros.controls.categoriaId
          .setValue(
            String(categoriaId)
          );

      }


      // comprueba que alcanceCliente tenga uno de los valores permitidos
      if (
        ![
          'global',
          'familia',
          'categoria'
        ].includes(this.alcanceCliente)
      ) {

        this.cargando = false;

        this.error =
          'El alcance del catálogo no es válido.';

        return;
      }
    }


    // ========================= CARGAR CATALOGOS =========================

    // trae familias categorias e impuestos necesarios para trabajar
    this.servicio
      .catalogos()
      .subscribe({

        next: respuesta => {

          // si algun arreglo viene null o undefined usa []
          this.familias =
            respuesta.data?.familias ?? [];

          this.categorias =
            respuesta.data?.categorias ?? [];

          this.impuestos =
            respuesta.data?.impuestos ?? [];


          // ========================= VALIDAR FAMILIA =========================

          if (
            this.esCliente &&
            this.familiaRutaId
          ) {

            // busca que la familia de la URL realmente exista
            const familia =
              this.familias.find(
                x =>
                  x.familiaId ===
                  this.familiaRutaId
              );


            // si no la encuentra detiene la carga
            if (!familia) {

              this.cargando = false;

              this.error =
                'La familia solicitada no está disponible.';

              this.cdr.markForCheck();

              return;
            }


            // guarda el nombre para mostrarlo en pantalla
            this.familiaSeleccionadaNombre =
              familia.nombre;
          }


          // ========================= VALIDAR CATEGORIA =========================

          if (
            this.esCliente &&
            this.categoriaRutaId
          ) {

            // busca la categoria y tambien comprueba
            // que pertenezca a la familia de la ruta
            const categoria =
              this.categorias.find(
                x =>
                  x.categoriaId ===
                  this.categoriaRutaId &&
                  x.familiaId ===
                  this.familiaRutaId
              );


            if (!categoria) {

              this.cargando = false;

              this.error =
                'La categoría solicitada no pertenece a la familia o no está disponible.';

              this.cdr.markForCheck();

              return;
            }


            this.categoriaSeleccionadaNombre =
              categoria.nombre;
          }


          // si el Cliente esta en familia o busqueda global
          // pero no escribio texto no hace una consulta de productos
          if (
            this.esCliente &&
            this.alcanceCliente !== 'categoria' &&
            !this.filtros.controls.texto.value.trim()
          ) {

            this.cargando = false;

            this.cdr.markForCheck();

            return;
          }


          // si todo esta correcto finalmente carga los productos
          this.cargar();

          this.cdr.markForCheck();

        },


        error: err => {

          this.cargando = false;

          this.error =
            this.mensajeError(err);

          this.cdr.markForCheck();

        }

      });
  }


  // =========================================================
  // ========================= BUSQUEDA =======================
  // =========================================================

  // aplica los filtros actuales y vuelve a la pagina 1
  buscar(): void {

    const texto =
      this.filtros.controls.texto.value.trim();


    // si el Cliente esta fuera de una categoria
    // y deja vacio el buscador vuelve al nivel anterior
    if (
      this.esCliente &&
      this.alcanceCliente !== 'categoria' &&
      !texto
    ) {

      void this.router.navigate(
        this.rutaRegresoCliente
      );

      return;
    }


    this.pagina = 1;

    this.cargar();
  }


  // ========================= LIMPIAR FILTROS =========================

  limpiarFiltros(): void {

    // para el Cliente fuera de una categoria
    // limpiar significa volver al nivel anterior
    if (
      this.esCliente &&
      this.alcanceCliente !== 'categoria'
    ) {

      void this.router.navigate(
        this.rutaRegresoCliente
      );

      return;
    }


    // devuelve los filtros a sus valores iniciales
    // pero conserva familia y categoria cuando vienen de la ruta del Cliente
    this.filtros.reset({

      texto: '',

      familiaId:
        this.esCliente
          ? String(this.familiaRutaId)
          : '',

      categoriaId:
        this.esCliente
          ? String(this.categoriaRutaId)
          : '',

      precioMinimo: '',

      precioMaximo: '',

      disponibilidad: '',

      estado: '',

      orden: 'nombre_asc'

    });


    this.pagina = 1;

    this.cargar();
  }


  // ========================= CAMBIO DE FAMILIA =========================

  // cuando cambia la familia comprueba
  // que la categoria seleccionada todavia pertenezca a ella
  cambiarFamilia(): void {

    const categoriaId =
      Number(
        this.filtros.controls.categoriaId.value
      ) || 0;


    // some revisa si la categoria existe dentro de categoriasFiltradas
    if (
      categoriaId &&
      !this.categoriasFiltradas.some(
        x => x.categoriaId === categoriaId
      )
    ) {

      // si ya no corresponde limpia la categoria
      this.filtros.controls.categoriaId
        .setValue('');

    }
  }


  // ========================= TAMAÑO DE PAGINA =========================

  cambiarTamano(valor: string): void {

    const tamano =
      Number(valor);


    // comprueba que el numero recibido sea uno de los tamaños permitidos
    if (
      this.tamanos.includes(
        tamano as 25 | 50 | 75 | 100
      )
    ) {

      this.tamanoPagina =
        tamano as 25 | 50 | 75 | 100;

      this.pagina = 1;

      this.cargar();

    }
  }


  // ========================= CAMBIAR PAGINA =========================

  irPagina(delta: number): void {

    // suma o resta una pagina
    // Math.max evita bajar de 1
    // Math.min evita superar el total
    const nuevaPagina =
      Math.min(
        this.totalPaginas,
        Math.max(
          1,
          this.pagina + delta
        )
      );


    // solamente vuelve a consultar si la pagina realmente cambio
    if (nuevaPagina !== this.pagina) {

      this.pagina = nuevaPagina;

      this.cargar();

    }
  }


  // =========================================================
  // ==================== NUEVO PRODUCTO ======================
  // =========================================================

  // abre un formulario limpio para crear un producto
  nuevo(): void {

    this.editandoId = 0;


    this.formulario.reset({

      codigo: '',

      nombre: '',

      descripcion: '',

      categoriaId: 0,

      impuestoId: 0,

      precioVenta: 0,

      costo: 0,

      stock: 0,

      stockMinimo: 0,

      activo: true

    });


    this.mostrarFormulario = true;

    this.limpiarMensajes();
  }


  // =========================================================
  // ==================== EDITAR PRODUCTO =====================
  // =========================================================

  editar(producto: ProductoVista): void {

    // solamente el Admin puede editar
    // codigo undefined indica que no vienen completos los datos administrativos
    if (
      !this.esAdmin ||
      producto.codigo === undefined
    ) {
      return;
    }


    // guarda el id que se va a modificar
    this.editandoId =
      producto.productoId;


    // trae las imagenes que ya tiene este producto
    this.cargarImagenes(
      producto.productoId
    );


    // carga los datos actuales dentro del formulario
    this.formulario.reset({

      codigo:
        producto.codigo,

      nombre:
        producto.nombre,

      descripcion:
        producto.descripcion ?? '',

      categoriaId:
        producto.categoriaId,

      impuestoId:
        producto.impuestoId,

      precioVenta:
        producto.precioVenta,

      costo:
        producto.costo ?? 0,

      stock:
        producto.stock ?? 0,

      stockMinimo:
        producto.stockMinimo ?? 0,

      activo:
        producto.activo ?? true

    });


    this.mostrarFormulario = true;

    this.limpiarMensajes();


    // queueMicrotask espera que Angular termine el ciclo actual
    // despues busca el formulario y baja suavemente hasta el
    queueMicrotask(() =>
      document
        .getElementById('formulario-producto')
        ?.scrollIntoView({
          behavior: 'smooth'
        })
    );
  }


  // ========================= CANCELAR FORMULARIO =========================

  cancelarFormulario(): void {

    // oculta el formulario
    this.mostrarFormulario = false;


    // deja de editar cualquier producto
    this.editandoId = 0;


    // limpia las imagenes que habia cargado del producto
    this.imagenesProducto = [];


    // limpia los archivos seleccionados desde la computadora
    this.archivosSeleccionados = [];


    // limpia las vistas previas
    this.previsualizaciones = [];
  }


  // =========================================================
  // ==================== GUARDAR PRODUCTO ====================
  // =========================================================

  // valida y decide si debe insertar o modificar
  guardar(): void {

    // hace visibles los errores de los campos
    this.formulario.markAllAsTouched();


    // no continua si:
    // no es Admin
    // el formulario tiene errores
    // o ya existe un guardado
    if (
      !this.esAdmin ||
      this.formulario.invalid ||
      this.guardando
    ) {
      return;
    }


    // agarra todos los valores actuales del formulario
    const valores =
      this.formulario.getRawValue();


    // arma el objeto que se manda al backend
    const datos: IProductoGuardar = {

      productoId:
        this.editandoId,

      categoriaId:
        valores.categoriaId,

      impuestoId:
        valores.impuestoId,

      // trim quita espacios del inicio y final
      codigo:
        valores.codigo.trim(),

      nombre:
        valores.nombre.trim(),

      // si la descripcion queda vacia manda null
      descripcion:
        valores.descripcion.trim() || null,

      // Number asegura que estos valores salgan como numeros
      precioVenta:
        Number(valores.precioVenta),

      costo:
        Number(valores.costo),

      stock:
        Number(valores.stock),

      stockMinimo:
        Number(valores.stockMinimo),

      activo:
        valores.activo

    };


    // true significa que estamos modificando un producto existente
    const editando =
      this.editandoId > 0;


    this.guardando = true;

    this.limpiarMensajes();


    // decide cual metodo del servicio debe ejecutar
    const solicitud =
      editando
        ? this.servicio.modificar(datos)
        : this.servicio.insertar(datos);


    solicitud.subscribe({

      next: respuesta => {

        // ========================= PRODUCTO EDITADO =========================

        // si era una edicion termina despues de modificar sus datos
        if (editando) {

          this.guardando = false;

          this.mensaje =
            'Producto actualizado correctamente.';

          this.cancelarFormulario();

          // false permite conservar el mensaje anterior
          this.cargar(false);

          this.cdr.markForCheck();

          return;
        }


        // ========================= PRODUCTO NUEVO =========================

        // intenta sacar el id que devolvio la API
        const productoId =
          respuesta.data?.productoId;


        // sin id no puede continuar con la carga de imagenes
        if (!productoId) {

          this.guardando = false;

          this.error =
            'El producto se creó, pero no se pudo obtener su ID.';

          this.cdr.markForCheck();

          return;
        }


        // ========================= SIN IMAGENES NUEVAS =========================

        // si no se seleccionaron imagenes termina normalmente
        if (
          this.archivosSeleccionados.length === 0
        ) {

          this.guardando = false;

          this.mensaje =
            'Producto creado correctamente.';

          this.cancelarFormulario();

          this.cargar(false);

          this.cdr.markForCheck();

          return;
        }


        // ========================= SUBIR IMAGENES DEL NUEVO PRODUCTO =========================

        // primero se creo el producto para obtener su ID
        // ahora manda las imagenes relacionadas con ese producto
        this.servicioImagen
          .subirImagenes(
            productoId,
            this.archivosSeleccionados
          )

          .pipe(
            finalize(() => {

              this.guardando = false;

              this.cdr.markForCheck();

            })
          )

          .subscribe({

            next: () => {

              this.mensaje =
                'Producto e imágenes creados correctamente.';

              this.cancelarFormulario();

              this.cargar(false);

            },


            error: err => {

              // el producto ya existe aunque las imagenes hayan fallado
              this.error =
                'El producto se creó, pero hubo un problema al subir las imágenes. ' +
                this.mensajeError(err);


              // conserva el id para que se pueda continuar trabajando con ese producto
              this.editandoId =
                productoId;

              this.cargar(false);

            }

          });

      },


      error: err => {

        this.guardando = false;

        this.error =
          this.mensajeError(err);

        this.cdr.markForCheck();

      }

    });
  }


  // =========================================================
  // ==================== CAMBIAR ESTADO ======================
  // =========================================================

  // activa o desactiva un producto
  cambiarEstado(
    producto: ProductoVista
  ): void {

    // solamente el Admin puede hacerlo
    // tambien comprueba que activo exista y que no haya otro guardado
    if (
      !this.esAdmin ||
      producto.activo === undefined ||
      this.guardando
    ) {
      return;
    }


    this.guardando = true;

    this.limpiarMensajes();


    // manda el estado contrario al que tiene actualmente
    this.servicio
      .cambiarEstado(
        producto.productoId,
        !producto.activo
      )

      .pipe(
        finalize(() => {

          this.guardando = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: () => {

          this.mensaje =
            producto.activo
              ? 'Producto desactivado.'
              : 'Producto activado.';

          this.cargar(false);

        },


        error: err => {
          this.error =
            this.mensajeError(err);
        }

      });
  }


  // =========================================================
  // ======================== CARRITO =========================
  // =========================================================

  // agrega una unidad del producto al carrito
  agregarAlCarrito(
    producto: ProductoVista
  ): void {

    // no permite continuar si:
    // no es Cliente
    // el producto no esta disponible
    // o ya se esta agregando ese mismo producto
    if (
      !this.esCliente ||
      !producto.disponible ||
      this.procesandoCarrito.has(producto.productoId)
    ) {
      return;
    }


    this.limpiarMensajes();


    // guarda el id mientras se procesa
    this.procesandoCarrito.add(
      producto.productoId
    );


    // manda 1 como cantidad inicial
    this.carrito
      .agregar(
        producto.productoId,
        1
      )

      .pipe(
        finalize(() => {

          // vuelve a liberar el boton de ese producto
          this.procesandoCarrito.delete(
            producto.productoId
          );

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: () => {
          this.mensaje =
            'El producto fue agregado al carrito.';
        },

        error: err => {
          this.error =
            this.mensajeError(err);
        }

      });
  }


  // =========================================================
  // =================== CARGAR IMAGENES ======================
  // =========================================================

  // trae todas las imagenes que pertenecen a un producto
  cargarImagenes(
    productoId: number
  ): void {

    this.servicioImagen
      .listarPorProducto(productoId)
      .subscribe({

        next: respuesta => {

          this.imagenesProducto =
            respuesta.data ?? [];

          this.cdr.markForCheck();

        },


        // si falla simplemente deja la lista vacia
        error: () => {

          this.imagenesProducto = [];

          this.cdr.markForCheck();

        }

      });
  }


  // =========================================================
  // ================= SELECCIONAR IMAGENES ===================
  // =========================================================

  // recibe las imagenes que el Administrador selecciono desde el input
  seleccionarImagenes(
    event: Event
  ): void {

    // obtiene el input que genero el evento
    const input =
      event.target as HTMLInputElement;


    // si no se escogieron archivos no hace nada
    if (!input.files?.length) {
      return;
    }


    // Array.from convierte FileList en un arreglo normal
    const nuevosArchivos =
      Array.from(input.files);


    // suma:
    // imagenes que ya estan guardadas
    // imagenes seleccionadas antes
    // imagenes seleccionadas ahora
    const cantidadTotal =
      this.imagenesProducto.length +
      this.archivosSeleccionados.length +
      nuevosArchivos.length;


    // no permite pasar del maximo de 3
    if (cantidadTotal > 3) {

      this.error =
        'El producto puede tener como máximo 3 imágenes.';

      input.value = '';

      return;
    }


    // ... mete todos los archivos nuevos dentro del arreglo
    // sin borrar los que ya estaban seleccionados
    this.archivosSeleccionados.push(
      ...nuevosArchivos
    );


    // recorre solamente las imagenes que se acaban de seleccionar
    for (const archivo of nuevosArchivos) {

      // si el archivo no tiene tipo image lo salta
      if (!archivo.type.startsWith('image/')) {
        continue;
      }


      // FileReader permite leer el archivo desde el navegador
      const lector =
        new FileReader();


      // onload se ejecuta cuando termina de leerlo
      lector.onload = () => {

        // guarda el resultado para mostrar la vista previa
        this.previsualizaciones.push(
          lector.result as string
        );

        this.cdr.markForCheck();

      };


      // convierte el archivo en una URL que puede usar un img
      lector.readAsDataURL(archivo);

    }


    // limpia el input para permitir seleccionar otras imagenes despues
    input.value = '';


    this.error = '';
  }


  // =========================================================
  // ================ QUITAR IMAGEN SELECCIONADA ==============
  // =========================================================

  // elimina una imagen que todavia no se ha subido
  eliminarImagenSeleccionada(
    indice: number
  ): void {

    // splice elimina un elemento usando su posicion
    this.archivosSeleccionados.splice(
      indice,
      1
    );


    // quita tambien su vista previa
    this.previsualizaciones.splice(
      indice,
      1
    );


    this.cdr.markForCheck();
  }


  // =========================================================
  // ==================== SUBIR IMAGENES ======================
  // =========================================================

  // sube las imagenes seleccionadas de un producto que ya existe
  subirImagenes(): void {

    // el producto tiene que existir primero
    if (this.editandoId <= 0) {
      return;
    }


    // tiene que existir por lo menos una imagen seleccionada
    if (!this.archivosSeleccionados.length) {
      return;
    }


    // evita mandar la misma solicitud varias veces
    if (this.subiendoImagenes) {
      return;
    }


    this.subiendoImagenes = true;


    this.limpiarMensajes();


    this.servicioImagen
      .subirImagenes(
        this.editandoId,
        this.archivosSeleccionados
      )

      .pipe(
        finalize(() => {

          this.subiendoImagenes = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: () => {

          this.mensaje =
            'Imágenes agregadas correctamente.';


          // limpia los archivos temporales
          this.archivosSeleccionados = [];


          // limpia las vistas previas
          this.previsualizaciones = [];


          // vuelve a traer las imagenes que quedaron guardadas
          this.cargarImagenes(
            this.editandoId
          );


          // actualiza tambien las tarjetas del catalogo
          this.cargar(false);

        },


        error: err => {

          this.error =
            this.mensajeError(err);

        }

      });
  }


  // =========================================================
  // ================== IMAGEN PRINCIPAL ======================
  // =========================================================

  // cambia cual imagen del producto se considera principal
  hacerPrincipal(
    imagenId: number
  ): void {

    this.limpiarMensajes();


    this.servicioImagen
      .establecerPrincipal(imagenId)
      .subscribe({

        next: () => {

          this.mensaje =
            'Imagen principal actualizada correctamente.';


          // actualiza las imagenes del formulario
          this.cargarImagenes(
            this.editandoId
          );


          // actualiza la imagen que aparece en las tarjetas
          this.cargar(false);

        },


        error: err => {

          this.error =
            this.mensajeError(err);

        }

      });
  }


  // =========================================================
  // =================== ELIMINAR IMAGEN ======================
  // =========================================================

  eliminarImagen(
    imagen: IProductoImagen
  ): void {

    // pregunta antes de eliminar definitivamente la imagen
    const confirmar =
      confirm(
        '¿Desea eliminar esta imagen del producto?'
      );


    // si el Administrador cancela no hace nada
    if (!confirmar) {
      return;
    }


    this.limpiarMensajes();


    // manda a la API el id de la imagen
    this.servicioImagen
      .eliminar(imagen.imagenId)
      .subscribe({

        next: () => {

          this.mensaje =
            'Imagen eliminada correctamente.';


          // actualiza las imagenes restantes
          this.cargarImagenes(
            this.editandoId
          );


          // actualiza tambien las tarjetas de productos
          this.cargar(false);

        },


        error: err => {

          this.error =
            this.mensajeError(err);

        }

      });
  }


  // =========================================================
  // =================== IMAGEN CON ERROR =====================
  // =========================================================

  // recuerda que la imagen principal de este producto fallo
  // asi el HTML puede mostrar sus iniciales en vez de seguir intentando
  marcarImagenError(
    productoId: number
  ): void {

    this.imagenesFallidas.add(
      productoId
    );
  }


  // =========================================================
  // ======================= INICIALES ========================
  // =========================================================

  // genera hasta 2 letras para usar cuando no hay imagen
  iniciales(
    nombre: string
  ): string {

    // trim limpia extremos
    // split separa por uno o varios espacios
    // filter elimina valores vacios
    const palabras =
      nombre
        .trim()
        .split(/\s+/)
        .filter(Boolean);


    // si no existe ninguna palabra usa PR
    if (!palabras.length) {
      return 'PR';
    }


    // si solamente existe una palabra usa sus primeras 2 letras
    if (palabras.length === 1) {

      return palabras[0]
        .slice(0, 2)
        .toUpperCase();

    }


    // si existen varias usa la primera letra de las primeras 2 palabras
    return `${palabras[0][0]}${palabras[1][0]}`
      .toUpperCase();
  }


  // =========================================================
  // ================== NOMBRE DE FAMILIA =====================
  // =========================================================

  // busca el nombre de una familia usando su id
  nombreFamilia(
    familiaId: number
  ): string {

    // ?.nombre intenta sacar el nombre si encontro la familia
    // ?? usa Familia si no encontro ninguna
    return this.familias
      .find(
        x => x.familiaId === familiaId
      )
      ?.nombre ?? 'Familia';
  }


  // =========================================================
  // ===================== CLASE DE STOCK =====================
  // =========================================================

  // devuelve la clase CSS que corresponde al estado del stock
  claseStock(
    estado: string
  ): string {

    if (estado === 'Disponible') {
      return 'stock-ok';
    }


    if (estado === 'Stock bajo') {
      return 'stock-low';
    }


    return 'stock-out';
  }


  // =========================================================
  // =================== CARGAR PRODUCTOS =====================
  // =========================================================

  // arma los filtros actuales
  // escoge la consulta correcta segun el rol
  // y carga la pagina de productos
  private cargar(
    limpiarMensajes = true
  ): void {

    // no consulta si los filtros tienen algun error
    if (this.filtros.invalid) {
      return;
    }


    // agarra todos los valores actuales
    const valores =
      this.filtros.getRawValue();


    // arma el objeto que se manda a la API
    const filtro: IFiltroProductos = {

      // si el texto queda vacio manda undefined
      texto:
        valores.texto.trim() || undefined,


      // para el Cliente la familia viene obligatoriamente desde la ruta
      // para el Admin viene desde el select de filtros
      familiaId:
        this.esCliente
          ? this.familiaRutaId || undefined
          : Number(valores.familiaId) || undefined,


      // igual con la categoria
      categoriaId:
        this.esCliente
          ? this.categoriaRutaId || undefined
          : Number(valores.categoriaId) || undefined,


      // si el campo esta vacio no manda filtro de precio
      precioMinimo:
        valores.precioMinimo === ''
          ? undefined
          : Number(valores.precioMinimo),


      precioMaximo:
        valores.precioMaximo === ''
          ? undefined
          : Number(valores.precioMaximo),


      // as indica a TypeScript que este valor corresponde
      // al tipo permitido dentro de IFiltroProductos
      disponibilidad:
        (valores.disponibilidad || undefined) as
          IFiltroProductos['disponibilidad'],


      // vacio significa todos
      // activo significa true
      // cualquier otro valor en este filtro queda false
      activo:
        valores.estado === ''
          ? undefined
          : valores.estado === 'activo',


      pagina:
        this.pagina,


      tamanoPagina:
        this.tamanoPagina,


      orden:
        valores.orden as IFiltroProductos['orden']

    };


    // normalmente limpia los mensajes antes de consultar
    // algunas operaciones mandan false para conservar el mensaje de exito
    if (limpiarMensajes) {
      this.limpiarMensajes();
    }


    this.cargando = true;


    // Admin usa la consulta administrativa
    // Cliente usa solamente el catalogo publico permitido
    const solicitud =
      this.esAdmin
        ? this.servicio.administracion(filtro)
        : this.servicio.catalogo(filtro);


    solicitud

      // finalize siempre apaga el estado de carga
      .pipe(
        finalize(() => {

          this.cargando = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: respuesta => {

          // guarda los productos de la pagina actual
          this.productos =
            respuesta.data?.items ?? [];


          // guarda el total general para calcular la paginacion
          this.total =
            respuesta.data?.total ?? 0;


          // usa la pagina devuelta por la API
          // si no viene conserva la actual
          this.pagina =
            respuesta.data?.pagina ??
            this.pagina;


          // permite volver a intentar cargar las imagenes
          // con la nueva respuesta del servidor
          this.imagenesFallidas.clear();

        },


        error: err => {

          this.productos = [];

          this.total = 0;

          this.error =
            this.mensajeError(err);

        }

      });
  }


  // =========================================================
  // =================== LIMPIAR MENSAJES =====================
  // =========================================================

  private limpiarMensajes(): void {

    this.mensaje = '';

    this.error = '';
  }


  // =========================================================
  // =================== MENSAJE DE ERROR =====================
  // =========================================================

  // intenta sacar el mensaje mas util que venga desde la API
  private mensajeError(
    error: any
  ): string {

    // algunas respuestas de validacion traen un objeto
    // donde cada propiedad contiene un arreglo de mensajes
    const validaciones =
      error?.error?.errors as
      Record<string, string[]> |
      undefined;


    if (validaciones) {

      // Object.values saca todos los arreglos
      // flat los convierte en una sola lista
      // [0] agarra el primer mensaje disponible
      return Object
        .values(validaciones)
        .flat()[0] ??
        'Revisa los datos ingresados.';
    }


    // intenta primero con error
    // despues title
    // y por ultimo usa el mensaje general
    return error?.error?.error ||
      error?.error?.title ||
      'No fue posible completar la operación.';
  }

}
