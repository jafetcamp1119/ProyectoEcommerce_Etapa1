import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { IFiltroOrdenes, IOrdenResumen } from '../../model/IOrden';
import { AutenticacionService } from '../../services/autenticacion';
import { OrdenService } from '../../services/orden';


// esta pantalla muestra las ordenes del Cliente
// o todas las ordenes si la persona que entro es Administrador
@Component({
  selector: 'app-orden',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CurrencyPipe,
    DatePipe
  ],
  templateUrl: './orden.html',
  styleUrl: './orden.css'
})
export class Orden implements OnInit {

  // herramientas y servicios que usa este componente
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(OrdenService);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly cdr = inject(ChangeDetectorRef);


  // guarda si la sesion actual pertenece a un Administrador
  // esto cambia la informacion y acciones que se muestran en pantalla
  readonly esAdmin =
    this.autenticacion.esAdministrador();


  // cantidades de ordenes que se pueden mostrar por pagina
  // as const hace que TypeScript conserve exactamente estos valores
  readonly tamanos = [
    25,
    50,
    75,
    100
  ] as const;


  // estados que se pueden usar dentro del filtro
  readonly estados = [
    'PROFORMA',
    'PENDIENTE',
    'CONFIRMADA',
    'FACTURADA',
    'CANCELADA'
  ];


  // aqui se guardan las ordenes que devuelve la API
  ordenes: IOrdenResumen[] = [];


  // controla si en este momento se estan cargando las ordenes
  cargando = true;


  // datos de la paginacion
  pagina = 1;

  tamanoPagina: 25 | 50 | 75 | 100 = 25;

  total = 0;


  // mensajes que aparecen en pantalla
  error = '';
  mensaje = '';


  // guarda los ids de las ordenes que tienen alguna operacion en proceso
  // Set evita repetir el mismo id y permite revisar rapido con has
  procesando = new Set<number>();


  // formulario que contiene todos los filtros de busqueda
  readonly filtros = this.fb.nonNullable.group({

    numero: [''],

    cliente: [''],

    estado: [''],

    fechaDesde: [''],

    fechaHasta: ['']

  });


  // calcula cuantas paginas existen segun el total de ordenes
  get totalPaginas(): number {

    // Math.ceil redondea hacia arriba
    // por ejemplo 26 registros con 25 por pagina necesitan 2 paginas
    // Math.max evita que el total de paginas llegue a 0
    return Math.max(
      1,
      Math.ceil(
        this.total / this.tamanoPagina
      )
    );
  }


  // ngOnInit se ejecuta automaticamente cuando se abre la pantalla
  // carga la primera pagina de ordenes
  ngOnInit(): void {
    this.cargar();
  }


  // aplica los filtros actuales
  // una busqueda nueva siempre empieza desde la primera pagina
  buscar(): void {

    this.pagina = 1;

    this.cargar();
  }


  // devuelve todos los filtros a su valor inicial
  // y vuelve a consultar las ordenes
  limpiar(): void {

    this.filtros.reset({

      numero: '',

      cliente: '',

      estado: '',

      fechaDesde: '',

      fechaHasta: ''

    });


    this.pagina = 1;

    this.cargar();
  }


  // cambia la cantidad de ordenes que se muestran por pagina
  cambiarTamano(valor: string): void {

    // convierte el valor que viene del select a numero
    // y lo trata como uno de los tamaños permitidos
    const tamano =
      Number(valor) as 25 | 50 | 75 | 100;


    // includes revisa que el numero realmente exista dentro de tamanos
    if (!this.tamanos.includes(tamano)) {
      return;
    }


    this.tamanoPagina = tamano;

    // vuelve a la primera pagina para evitar quedar fuera del nuevo rango
    this.pagina = 1;

    this.cargar();
  }


  // suma o resta paginas sin dejar que el numero salga del rango permitido
  irPagina(delta: number): void {

    // pagina + delta calcula hacia donde se quiere mover
    // Math.max evita bajar de 1
    // Math.min evita pasar de totalPaginas
    const destino = Math.min(
      this.totalPaginas,
      Math.max(
        1,
        this.pagina + delta
      )
    );


    // si ya estamos en esa pagina no hace otra solicitud
    if (destino === this.pagina) {
      return;
    }


    this.pagina = destino;

    this.cargar();
  }


  // intenta cancelar una orden pendiente del Cliente
  // despues vuelve a cargar la lista con el resultado
  cancelar(orden: IOrdenResumen): void {

    // no deja cancelar si:
    // la orden ya no esta pendiente
    // la persona es Administrador
    // o esa misma orden ya tiene otra operacion en proceso
    if (
      orden.estado !== 'PENDIENTE' ||
      this.esAdmin ||
      this.procesando.has(orden.ordenId)
    ) {
      return;
    }


    // confirm muestra una ventana del navegador para confirmar la cancelacion
    if (
      !confirm(
        `¿Deseas cancelar la orden #${orden.numeroOrden}?`
      )
    ) {
      return;
    }


    this.limpiarMensajes();


    // guarda el id para bloquear otras acciones sobre esta orden mientras termina
    this.procesando.add(orden.ordenId);


    this.servicio
      .cancelar(orden.ordenId)

      // finalize se ejecuta tanto si la solicitud sale bien como si falla
      .pipe(
        finalize(() => {

          // quita la orden del grupo de operaciones en proceso
          this.procesando.delete(orden.ordenId);

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: () => {

          this.mensaje =
            'Orden cancelada correctamente.';


          // false hace que cargar no borre el mensaje que acabamos de poner
          this.cargar(false);

        },

        error: err => {

          this.error =
            err?.error?.error ||
            'No fue posible cancelar la orden.';

        }

      });
  }


  // descarga el PDF de la factura cuando este disponible
  descargarFactura(orden: IOrdenResumen): void {

    // no continua si no existe factura
    // o si ya hay otra operacion trabajando con esta orden
    if (
      !orden.facturaDisponible ||
      this.procesando.has(orden.ordenId)
    ) {
      return;
    }


    this.limpiarMensajes();


    // marca esta orden como procesando mientras se descarga el archivo
    this.procesando.add(orden.ordenId);


    this.servicio
      .descargarFactura(orden.ordenId)

      // cuando termine libera nuevamente la orden
      .pipe(
        finalize(() => {

          this.procesando.delete(orden.ordenId);

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        // archivo es el Blob que devuelve la API
        // el nombre se arma usando el numero de la orden
        next: archivo =>
          this.guardarArchivo(
            archivo,
            `Factura-${orden.numeroOrden}.pdf`
          ),

        error: err => {

          // si la API responde 404 significa que no encontro la factura
          // cualquier otro error usa el mensaje general de descarga
          this.error =
            err?.status === 404
              ? 'La factura no está disponible.'
              : 'No fue posible descargar la factura.';

        }

      });
  }


  // cambia el codigo interno del metodo de pago
  // por el texto que se quiere mostrar en pantalla
  etiquetaMetodo(
    metodo: string | null
  ): string {

    // si no existe metodo muestra este texto
    if (!metodo) {
      return 'No registrado';
    }


    // TARJETA se muestra como Tarjeta
    // EFECTIVO se muestra como Efectivo
    // cualquier otro valor se devuelve tal como vino
    return metodo === 'TARJETA'
      ? 'Tarjeta'
      : metodo === 'EFECTIVO'
        ? 'Efectivo'
        : metodo;
  }


  // trae las ordenes desde la API usando los filtros y la pagina actual
  // tambien escoge automaticamente si usar la consulta de Cliente o Administrador
  private cargar(
    limpiarMensajes = true
  ): void {

    // normalmente limpia mensajes antes de consultar
    // pero algunas acciones pueden mandar false para conservarlos
    if (limpiarMensajes) {
      this.limpiarMensajes();
    }


    this.cargando = true;


    // agarra todos los valores actuales del formulario de filtros
    const valores =
      this.filtros.getRawValue();


    // arma el objeto que se manda a la API
    const filtro: IFiltroOrdenes = {

      // trim quita espacios al principio y final
      // si queda vacio manda undefined
      numero:
        valores.numero.trim() || undefined,


      // el filtro por Cliente solamente se manda cuando es Administrador
      cliente:
        this.esAdmin
          ? valores.cliente.trim() || undefined
          : undefined,


      estado:
        valores.estado || undefined,


      fechaDesde:
        valores.fechaDesde || undefined,


      fechaHasta:
        valores.fechaHasta || undefined,


      pagina:
        this.pagina,


      tamanoPagina:
        this.tamanoPagina

    };


    // el Administrador consulta todas las ordenes
    // el Cliente consulta solamente sus propias ordenes
    const solicitud =
      this.esAdmin
        ? this.servicio.administracion(filtro)
        : this.servicio.misOrdenes(filtro);


    solicitud

      // cuando termine la consulta apaga el estado de carga
      .pipe(
        finalize(() => {

          this.cargando = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: respuesta => {

          // guarda solamente los registros de la pagina que devolvio la API
          this.ordenes =
            respuesta.data?.items ?? [];


          // guarda la cantidad total de ordenes
          this.total =
            respuesta.data?.total ?? 0;


          // usa la pagina que devolvio la API
          // si no viene mantiene la actual
          this.pagina =
            respuesta.data?.pagina ?? this.pagina;

        },


        error: err => {

          // si falla deja la lista limpia
          this.ordenes = [];

          this.total = 0;


          this.error =
            err?.error?.error ||
            'No fue posible cargar las órdenes.';

        }

      });
  }


  // recibe un archivo Blob y hace que el navegador lo descargue
  private guardarArchivo(
    archivo: Blob,
    nombre: string
  ): void {

    // crea una URL temporal que apunta al archivo recibido
    const url =
      URL.createObjectURL(archivo);


    // crea temporalmente un enlace HTML
    const enlace =
      document.createElement('a');


    // href apunta al archivo temporal
    enlace.href = url;


    // download indica el nombre con el que se va a guardar
    enlace.download = nombre;


    // simula un click sobre el enlace para iniciar la descarga
    enlace.click();


    // libera la URL temporal de memoria cuando ya no se necesita
    URL.revokeObjectURL(url);
  }


  // limpia los mensajes anteriores
  private limpiarMensajes(): void {

    this.error = '';
    this.mensaje = '';
  }

}
