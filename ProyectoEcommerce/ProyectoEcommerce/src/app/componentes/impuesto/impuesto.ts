import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';

import { finalize } from 'rxjs';

import { IImpuesto } from '../../model/IImpuesto';
import { ImpuestoService } from '../../services/impuesto';


// esta pantalla permite consultar crear editar activar y desactivar impuestos
@Component({
  selector: 'app-impuesto',
  imports: [ReactiveFormsModule],
  templateUrl: './impuesto.html',
  styleUrl: './impuesto.css'
})
export class Impuesto implements OnInit {

  // herramientas y servicios que usa este componente
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(ImpuestoService);
  private readonly cdr = inject(ChangeDetectorRef);


  // aqui se guardan los impuestos que vienen desde la API
  impuestos: IImpuesto[] = [];


  // controlan diferentes estados de la pantalla
  cargando = true;
  guardando = false;
  mostrarFormulario = false;


  // si vale 0 estamos creando
  // si tiene un id estamos editando un impuesto existente
  editandoId = 0;


  // mensajes que se muestran despues de las operaciones
  mensaje = '';
  error = '';


  // datos que controlan la paginacion
  pagina = 1;
  tamanoPagina = 25;

  // cantidades de filas que puede escoger el usuario
  readonly tamanos = [
    25,
    50,
    75,
    100
  ];


  // formulario para crear o editar impuestos
  readonly formulario = this.fb.nonNullable.group({

    nombre: [
      '',
      [
        Validators.required,
        Validators.maxLength(80)
      ]
    ],

    porcentaje: [
      0,
      [
        Validators.required,
        Validators.min(0),
        Validators.max(100)
      ]
    ],

    // cuando se crea el formulario empieza usando la fecha de hoy
    fechaInicio: [
      this.hoy(),
      Validators.required
    ],

    // la fecha final es opcional
    fechaFin: ['']

  }, {

    // fechasValidas revisa la relacion entre fechaInicio y fechaFin
    validators: this.fechasValidas

  });


  // calcula cuantas paginas se necesitan segun la cantidad de impuestos
  get totalPaginas(): number {

    // Math.ceil redondea hacia arriba
    // por ejemplo 26 registros con 25 por pagina necesitan 2 paginas
    // Math.max evita que el total llegue a 0
    return Math.max(
      1,
      Math.ceil(
        this.impuestos.length / this.tamanoPagina
      )
    );
  }


  // devuelve solamente los impuestos que pertenecen a la pagina actual
  get impuestosPagina(): IImpuesto[] {

    // calcula desde que posicion debe empezar la pagina
    const inicio =
      (this.pagina - 1) * this.tamanoPagina;

    // slice toma solamente el pedazo del arreglo correspondiente a esta pagina
    return this.impuestos.slice(
      inicio,
      inicio + this.tamanoPagina
    );
  }


  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  ngOnInit(): void {
    this.cargar();
  }


  // trae todos los impuestos desde la API
  cargar(): void {

    this.cargando = true;
    this.error = '';


    this.servicio
      .listar()

      // finalize se ejecuta cuando termina la solicitud
      // sin importar si termino bien o con error
      .pipe(
        finalize(() => {

          this.cargando = false;

          // le avisa a Angular que vuelva a revisar la pantalla
          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: respuesta => {

          // si data viene null o undefined usa un arreglo vacio
          // sort acomoda los impuestos alfabeticamente por nombre
          this.impuestos = (respuesta.data ?? [])
            .sort(
              (a, b) => a.nombre.localeCompare(b.nombre)
            );

        },

        // si la API falla prepara el mensaje de error
        error: err => {
          this.error = this.mensajeError(err);
        }

      });
  }


  // abre el formulario limpio para crear un impuesto nuevo
  // usa la fecha de hoy como fecha inicial
  nuevo(): void {

    // 0 indica que no se esta editando ningun impuesto existente
    this.editandoId = 0;


    this.formulario.reset({

      nombre: '',

      porcentaje: 0,

      fechaInicio: this.hoy(),

      fechaFin: ''

    });


    this.mostrarFormulario = true;

    this.limpiarMensajes();
  }


  // copia los datos del impuesto escogido dentro del formulario
  editar(item: IImpuesto): void {

    // guarda el id para saber cual impuesto se va a modificar
    this.editandoId = item.impuestoId;


    this.formulario.reset({

      nombre:
        item.nombre,

      porcentaje:
        item.porcentaje,

      fechaInicio:
        item.fechaInicio,

      // si fechaFin viene null o undefined deja el campo vacio
      fechaFin:
        item.fechaFin ?? ''

    });


    this.mostrarFormulario = true;

    this.limpiarMensajes();
  }


  // cierra el formulario sin guardar cambios
  cancelar(): void {

    this.mostrarFormulario = false;

    this.editandoId = 0;

    this.formulario.reset();
  }


  // valida el formulario y decide si debe insertar o modificar
  guardar(): void {

    // hace que Angular muestre los errores de los campos si existen
    this.formulario.markAllAsTouched();


    // no continua si el formulario tiene errores
    // o si ya existe otra solicitud guardando
    if (
      this.formulario.invalid ||
      this.guardando
    ) {
      return;
    }


    // agarra todos los valores actuales del formulario
    const valor =
      this.formulario.getRawValue();


    // busca el impuesto actual cuando se esta editando
    // sirve para conservar su estado activo o inactivo
    const actual = this.impuestos.find(
      x => x.impuestoId === this.editandoId
    );


    // arma el objeto que se va a mandar a la API
    const datos: IImpuesto = {

      impuestoId:
        this.editandoId,


      // trim quita espacios al inicio y al final
      nombre:
        valor.nombre.trim(),


      // Number asegura que porcentaje se mande como numero
      porcentaje:
        Number(valor.porcentaje),


      fechaInicio:
        valor.fechaInicio,


      // si fechaFin queda vacia manda null
      fechaFin:
        valor.fechaFin || null,


      // conserva el estado actual si existe
      // si es nuevo empieza activo
      activo:
        actual?.activo ?? true

    };


    // si editandoId tiene un valor modifica
    // si vale 0 inserta un impuesto nuevo
    const solicitud =
      this.editandoId
        ? this.servicio.modificar(datos)
        : this.servicio.insertar(datos);


    this.guardando = true;

    this.limpiarMensajes();


    solicitud

      // al terminar vuelve a quitar el estado guardando
      .pipe(
        finalize(() => {

          this.guardando = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: () => {

          // cambia el mensaje dependiendo de si se creo o se edito
          this.mensaje =
            this.editandoId
              ? 'Impuesto actualizado correctamente.'
              : 'Impuesto creado correctamente.';


          this.cancelar();

          // vuelve a cargar la lista para mostrar los cambios
          this.cargar();

        },

        error: err => {
          this.error = this.mensajeError(err);
        }

      });
  }


  // cambia el impuesto entre activo e inactivo
  cambiarEstado(item: IImpuesto): void {

    this.limpiarMensajes();


    // esta funcion se usa cuando la operacion termina correctamente
    const alCompletar = () => {

      this.mensaje =
        item.activo
          ? 'Impuesto desactivado.'
          : 'Impuesto activado.';

      this.cargar();

    };


    // esta funcion maneja los errores de cualquiera de las 2 operaciones
    const alFallar = (err: any) => {

      this.error =
        this.mensajeError(err);

      this.cdr.markForCheck();

    };


    // si actualmente esta activo llama eliminar
    // en este proyecto esa accion hace una desactivacion logica
    if (item.activo) {

      this.servicio
        .eliminar(item.impuestoId)
        .subscribe({
          next: alCompletar,
          error: alFallar
        });

    } else {

      // ...item copia todas las propiedades del impuesto
      // despues activo true cambia solamente ese valor
      this.servicio
        .modificar({
          ...item,
          activo: true
        })
        .subscribe({
          next: alCompletar,
          error: alFallar
        });

    }
  }


  // cambia la cantidad de registros que se muestran por pagina
  cambiarTamano(valor: string): void {

    // convierte el valor del select a numero
    this.tamanoPagina =
      Number(valor);

    // vuelve a la primera pagina para evitar quedar fuera del nuevo rango
    this.pagina = 1;
  }


  // mueve la pagina hacia adelante o hacia atras
  irPagina(delta: number): void {

    // pagina + delta calcula la nueva pagina
    // Math.max evita bajar de 1
    // Math.min evita pasar del total de paginas
    this.pagina = Math.min(
      this.totalPaginas,
      Math.max(
        1,
        this.pagina + delta
      )
    );
  }


  // devuelve la fecha actual en el formato que necesita un input type date
  private hoy(): string {

    // toISOString empieza con yyyy-MM-dd
    // slice deja solamente esos primeros 10 caracteres
    return new Date()
      .toISOString()
      .slice(0, 10);
  }


  // valida las 2 fechas usando el formulario completo
  // devuelve un error si fechaFin queda antes de fechaInicio
  private fechasValidas(
    control: AbstractControl
  ): ValidationErrors | null {

    // get busca los controles por su nombre dentro del formulario
    const inicio =
      control.get('fechaInicio')?.value;

    const fin =
      control.get('fechaFin')?.value;


    // si existen las 2 fechas y fin es menor que inicio
    // devuelve el error fechasInvalidas
    // si todo esta bien devuelve null
    return inicio &&
      fin &&
      fin < inicio
      ? { fechasInvalidas: true }
      : null;
  }


  // limpia mensajes anteriores antes de empezar otra accion
  private limpiarMensajes(): void {

    this.mensaje = '';
    this.error = '';
  }


  // intenta agarrar primero el mensaje que mando la API
  // si no encuentra ninguno usa un mensaje general
  private mensajeError(error: any): string {

    return error?.error?.error ||
      error?.error?.title ||
      'No fue posible completar la operación.';
  }

}
