import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';

import { IProducto, IProductoCatalogo } from '../../model/IProducto';
import { IProductoImagen } from '../../model/IProductoImagen';

import { AutenticacionService } from '../../services/autenticacion';
import { CarritoService } from '../../services/carrito';
import { ProductoImagenService } from '../../services/producto-imagen';
import { ProductoService } from '../../services/producto';

import { ProductoCarrusel } from '../producto-carrusel/producto-carrusel';


// junta los datos que vienen del catalogo
// con algunos datos administrativos que puede traer IProducto
type ProductoDetalleVista =
  IProductoCatalogo &
  Partial<IProducto>;


// esta pantalla muestra toda la informacion de un producto
// si entra un Cliente tambien permite agregarlo al carrito
@Component({
  selector: 'app-producto-detalle',
  imports: [
    RouterLink,
    CurrencyPipe,
    DatePipe,
    ProductoCarrusel
  ],
  templateUrl: './producto-detalle.html',
  styleUrl: './producto-detalle.css'
})
export class ProductoDetalle implements OnInit {

  // ========================= SERVICIOS =========================

  // permite leer el productoId que viene dentro de la ruta
  private readonly route =
    inject(ActivatedRoute);

  // servicio principal de productos
  private readonly servicio =
    inject(ProductoService);

  // servicio que trae las imagenes relacionadas con el producto
  private readonly imagenesServicio =
    inject(ProductoImagenService);

  // sirve para conocer el rol de la persona que inicio sesion
  private readonly autenticacion =
    inject(AutenticacionService);

  // se usa para pedirle a Angular que actualice la pantalla
  private readonly cdr =
    inject(ChangeDetectorRef);

  // servicio que permite agregar productos al carrito
  private readonly carrito =
    inject(CarritoService);


  // ========================= ROL ACTUAL =========================

  // guarda si la persona actual es Administrador
  readonly esAdmin =
    this.autenticacion.esAdministrador();

  // guarda si la persona actual es Cliente
  readonly esCliente =
    this.autenticacion.esCliente();


  // ========================= DATOS DE LA PANTALLA =========================

  // aqui se guarda el producto cuando termina de cargar
  // empieza en null porque todavia no se ha obtenido
  producto: ProductoDetalleVista | null = null;


  // guarda todas las imagenes relacionadas con el producto
  imagenes: IProductoImagen[] = [];


  // controla si se esta esperando la respuesta de la API
  cargando = true;


  // guarda errores relacionados con la carga del producto
  error = '';


  // mensaje que aparece si el producto se agrega correctamente al carrito
  mensajeCarrito = '';


  // mensaje que aparece si falla la accion del carrito
  errorCarrito = '';


  // evita mandar varias veces la misma solicitud al carrito
  procesandoCarrito = false;


  // ========================= RUTA PARA VOLVER =========================

  // calcula a donde debe regresar el boton de volver
  get rutaRegreso(): any[] {

    // si es Cliente y ya existe el producto
    // vuelve directamente a la categoria donde estaba
    return this.esCliente && this.producto
      ? [
        '/productos/familia',
        this.producto.familiaId,
        'categoria',
        this.producto.categoriaId
      ]
      : ['/productos'];
  }


  // =========================================================
  // ========================= INICIO =========================
  // =========================================================

  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  // lee productoId de la ruta y empieza a cargar el detalle
  ngOnInit(): void {

    // paramMap avisa tambien si los parametros de la ruta cambian
    this.route.paramMap
      .subscribe(parametros => {

        // busca productoId y lo convierte a numero
        const productoId =
          Number(
            parametros.get('productoId')
          );


        // queueMicrotask espera que Angular termine el ciclo actual
        // y despues ejecuta esta parte
        queueMicrotask(() => {

          // revisa que el id sea un entero valido mayor que 0
          if (
            !Number.isInteger(productoId) ||
            productoId <= 0
          ) {

            this.cargando = false;

            this.error =
              'El producto solicitado no es válido.';

            this.cdr.markForCheck();

            return;
          }


          // si el id esta bien empieza la carga del producto
          this.cargar(productoId);

        });

      });
  }


  // =========================================================
  // ================= CARGAR EL PRODUCTO =====================
  // =========================================================

  // carga al mismo tiempo el detalle del producto y sus imagenes
  private cargar(
    productoId: number
  ): void {

    // activa el estado de carga
    this.cargando = true;


    // limpia cualquier error anterior
    this.error = '';


    // limpia el producto anterior mientras se trae el nuevo
    this.producto = null;


    // decide cual detalle pedir dependiendo del rol
    const productoSolicitud =
      this.esAdmin
        ? this.servicio.detalleAdministracion(productoId)
        : this.servicio.detalle(productoId);


    // forkJoin ejecuta las solicitudes juntas
    // y espera que ambas terminen para devolver el resultado
    forkJoin({

      // solicitud con la informacion principal del producto
      producto:
        productoSolicitud,


      // solicitud con las imagenes del producto
      imagenes:
        this.imagenesServicio
          .listarPorProducto(productoId)

          .pipe(

            // si solamente falla la carga de imagenes
            // devuelve una respuesta vacia para que el producto igual pueda mostrarse
            catchError(() =>
              of({
                success: false,
                data: [] as IProductoImagen[],
                error: ''
              })
            )

          )

    })

      // finalize siempre se ejecuta cuando termina
      // tanto si la solicitud sale bien como si falla
      .pipe(
        finalize(() => {

          this.cargando = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        // ========================= RESPUESTA CORRECTA =========================

        next: resultado => {

          // guarda la informacion principal
          this.producto =
            resultado.producto.data;


          // guarda las imagenes
          // si vienen null o undefined usa una lista vacia
          this.imagenes =
            resultado.imagenes.data ?? [];


          // si la API respondio pero no trajo producto
          // muestra este mensaje
          if (!this.producto) {

            this.error =
              'No se encontró el producto solicitado.';

          }

        },


        // ========================= ERROR =========================

        error: err => {

          // si viene 404 muestra un mensaje especifico
          // si viene otro error intenta usar el mensaje de la API
          this.error =
            err?.status === 404
              ? 'No se encontró el producto solicitado.'
              : (
                err?.error?.error ||
                'No fue posible cargar el detalle del producto.'
              );

        }

      });
  }


  // =========================================================
  // ======================== CARRITO =========================
  // =========================================================

  // agrega una unidad del producto actual al carrito
  // y muestra el resultado sin salir de esta pantalla
  agregarAlCarrito(): void {

    // no deja continuar si:
    // no es Cliente
    // no existe producto disponible
    // o ya se esta procesando esta accion
    if (
      !this.esCliente ||
      !this.producto?.disponible ||
      this.procesandoCarrito
    ) {
      return;
    }


    // limpia mensajes anteriores del carrito
    this.mensajeCarrito = '';

    this.errorCarrito = '';


    // bloquea temporalmente el boton
    this.procesandoCarrito = true;


    // manda el id del producto y una cantidad inicial de 1
    this.carrito
      .agregar(
        this.producto.productoId,
        1
      )

      .pipe(
        finalize(() => {

          // cuando termine vuelve a habilitar el boton
          this.procesandoCarrito = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        // si todo sale bien muestra el mensaje
        next: () => {

          this.mensajeCarrito =
            'El producto fue agregado al carrito.';

        },


        // si falla intenta mostrar el mensaje que mando la API
        // si no existe usa el mensaje general
        error: err => {

          this.errorCarrito =
            err?.error?.error ||
            'No fue posible agregar el producto al carrito.';

        }

      });
  }

}
