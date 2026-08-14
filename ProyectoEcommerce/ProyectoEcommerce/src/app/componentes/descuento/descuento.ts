import { DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import {
  ICatalogosDescuento,
  IDescuento,
  IProductoSelectorDescuento,
  TipoDescuento
} from '../../model/IDescuento';

import { DescuentoService } from '../../services/descuento';


// esta pantalla permite crear editar activar y desactivar descuentos
// tambien cambia los destinos disponibles dependiendo del tipo de descuento
@Component({
  selector: 'app-descuento',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './descuento.html',
  styleUrl: './descuento.css'
})
export class Descuento implements OnInit {

  // herramientas y servicios que usa este componente
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(DescuentoService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);


  // aqui se guarda la lista de descuentos que viene de la API
  descuentos: IDescuento[] = [];

  // estos catalogos contienen las familias categorias y productos
  // que se usan para llenar los select del formulario
  catalogos: ICatalogosDescuento = {
    familias: [],
    categorias: [],
    productos: []
  };


  // variables que controlan el estado de la pantalla
  cargando = true;
  guardando = false;
  mostrarFormulario = false;

  // si vale 0 estamos creando
  // si tiene un id estamos editando un descuento que ya existe
  editandoId = 0;

  // mensajes que aparecen en pantalla
  mensaje = '';
  error = '';

  // este error se usa especificamente para comparar las 2 fechas
  errorFechas = '';


  // tipos de descuento que puede escoger el Administrador
  // valor es el codigo interno y etiqueta es lo que se muestra en pantalla
  readonly tipos: {
    valor: TipoDescuento;
    etiqueta: string
  }[] = [
      {
        valor: 'FAMILIA',
        etiqueta: 'Familia'
      },
      {
        valor: 'CATEGORIA',
        etiqueta: 'Categoría'
      },
      {
        valor: 'PRODUCTO',
        etiqueta: 'Producto'
      },
      {
        valor: 'PROMOCIONAL',
        etiqueta: 'Promocional'
      }
    ];


  // formulario principal para crear o modificar un descuento
  readonly formulario = this.fb.nonNullable.group({

    nombre: [
      '',
      [
        Validators.required,
        Validators.maxLength(150)
      ]
    ],

    // empieza como descuento por familia
    tipoDescuento: [
      'FAMILIA' as TipoDescuento,
      Validators.required
    ],

    porcentaje: [
      0,
      [
        Validators.required,
        Validators.min(0.01),
        Validators.max(100)
      ]
    ],

    fechaInicio: [
      '',
      Validators.required
    ],

    fechaFin: [
      '',
      Validators.required
    ],

    activo: [true],

    // estos ids representan el destino final del descuento
    familiaId: [0],
    categoriaId: [0],
    productoId: [0],

    // estos 2 campos solamente ayudan a filtrar las opciones de los select
    filtroFamiliaId: [0],
    filtroCategoriaId: [0]

  });


  // devuelve directamente el tipo de descuento escogido actualmente
  get tipo(): TipoDescuento {
    return this.formulario.controls.tipoDescuento.value;
  }


  // devuelve solamente las categorias que corresponden a la familia usada como filtro
  get categoriasFiltradas() {

    const familiaId =
      this.formulario.controls.filtroFamiliaId.value;

    // filter recorre todas las categorias
    // si familiaId vale 0 deja pasar todas
    // si tiene un id deja solamente las que pertenecen a esa familia
    return this.catalogos.categorias.filter(
      x => !familiaId || x.familiaId === familiaId
    );
  }


  // devuelve los productos que cumplen con los filtros de familia y categoria
  get productosFiltrados(): IProductoSelectorDescuento[] {

    const familiaId =
      this.formulario.controls.filtroFamiliaId.value;

    const categoriaId =
      this.formulario.controls.filtroCategoriaId.value;

    // cada producto tiene que cumplir los filtros que tengan un valor
    return this.catalogos.productos.filter(
      x =>
        (!familiaId || x.familiaId === familiaId) &&
        (!categoriaId || x.categoriaId === categoriaId)
    );
  }


  // ngOnInit se ejecuta cuando se abre esta pantalla
  // forkJoin carga descuentos y catalogos al mismo tiempo
  ngOnInit(): void {

    // forkJoin espera que las 2 solicitudes terminen correctamente
    // despues entrega las 2 respuestas juntas en next
    forkJoin({
      descuentos: this.servicio.listar(),
      catalogos: this.servicio.catalogos()
    })

      // finalize se ejecuta tanto si las solicitudes salieron bien como si fallaron
      .pipe(
        finalize(() => {
          this.cargando = false;
          this.cdr.markForCheck();
        })
      )

      .subscribe({

        next: respuesta => {

          // si data viene null o undefined usa un arreglo vacio
          this.descuentos =
            respuesta.descuentos.data ?? [];

          // si no vienen catalogos mantiene el objeto que ya existia
          this.catalogos =
            respuesta.catalogos.data ?? this.catalogos;

          // revisa si la URL venia con algun descuento ya preseleccionado
          this.aplicarPreseleccion();
        },

        error: err => {
          this.error = this.mensajeError(err);
        }

      });
  }


  // abre el formulario con valores iniciales para crear un descuento nuevo
  nuevo(): void {

    // agarra la fecha y hora actual
    const inicio = new Date();

    // deja segundos y milisegundos en 0
    inicio.setSeconds(0, 0);

    // crea otra fecha usando la fecha de inicio
    const fin = new Date(inicio);

    // pone la fecha final 7 dias despues
    fin.setDate(fin.getDate() + 7);

    // 0 significa que estamos creando y no editando
    this.editandoId = 0;


    // limpia el formulario y carga los valores iniciales
    this.formulario.reset({

      nombre: '',

      tipoDescuento: 'FAMILIA',

      porcentaje: 0,

      // fechaLocal convierte las fechas al formato que usa datetime-local
      fechaInicio: this.fechaLocal(inicio),

      fechaFin: this.fechaLocal(fin),

      activo: true,

      familiaId: 0,
      categoriaId: 0,
      productoId: 0,
      filtroFamiliaId: 0,
      filtroCategoriaId: 0

    });


    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }


  // carga en el formulario los datos de un descuento que ya existe
  editar(descuento: IDescuento): void {

    // guarda el id para saber cual registro se esta modificando
    this.editandoId = descuento.descuentoId;


    // si el descuento tiene categoria busca esa categoria dentro de los catalogos
    // esto sirve para poder saber tambien a cual familia pertenece
    const categoria = descuento.categoriaId
      ? this.catalogos.categorias.find(
        x => x.categoriaId === descuento.categoriaId
      )
      : undefined;


    // si el descuento tiene producto busca ese producto
    // asi despues puede recuperar su familia y categoria
    const producto = descuento.productoId
      ? this.catalogos.productos.find(
        x => x.productoId === descuento.productoId
      )
      : undefined;


    // carga dentro del formulario todos los valores actuales del descuento
    this.formulario.reset({

      nombre: descuento.nombre,

      tipoDescuento: descuento.tipoDescuento,

      porcentaje: descuento.porcentaje,

      fechaInicio:
        this.fechaLocal(
          new Date(descuento.fechaInicio)
        ),

      fechaFin:
        this.fechaLocal(
          new Date(descuento.fechaFin)
        ),

      activo: descuento.activo,

      // ?? usa 0 si alguno de estos ids viene null o undefined
      familiaId:
        descuento.familiaId ?? 0,

      categoriaId:
        descuento.categoriaId ?? 0,

      productoId:
        descuento.productoId ?? 0,

      // intenta sacar la familia directamente del descuento
      // si no viene la busca desde la categoria o desde el producto
      filtroFamiliaId:
        descuento.familiaId ??
        categoria?.familiaId ??
        producto?.familiaId ??
        0,

      // si es un producto recupera su categoria para dejar el filtro seleccionado
      filtroCategoriaId:
        producto?.categoriaId ?? 0

    });


    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }


  // cierra el formulario sin guardar cambios
  cancelar(): void {

    this.mostrarFormulario = false;

    this.editandoId = 0;

    this.errorFechas = '';
  }


  // cuando cambia el tipo limpia todos los destinos y filtros anteriores
  // asi no queda seleccionado algo que pertenecia a otro tipo de descuento
  cambiarTipo(): void {

    this.formulario.patchValue({
      familiaId: 0,
      categoriaId: 0,
      productoId: 0,
      filtroFamiliaId: 0,
      filtroCategoriaId: 0
    });
  }


  // cuando cambia la familia usada como filtro
  // limpia categoria producto y el filtro anterior de categoria
  cambiarFiltroFamilia(): void {

    this.formulario.patchValue({
      categoriaId: 0,
      productoId: 0,
      filtroCategoriaId: 0
    });
  }


  // cuando cambia la categoria limpia el producto que estaba escogido antes
  cambiarFiltroCategoria(): void {

    this.formulario.controls.productoId.setValue(0);
  }


  // valida el formulario las fechas y el destino
  // despues decide si insertar o modificar segun editandoId
  guardar(): void {

    // muestra todos los errores del formulario
    this.formulario.markAllAsTouched();

    this.errorFechas = '';

    // agarra todos los valores actuales del formulario
    const valor = this.formulario.getRawValue();


    // no sigue si el formulario tiene errores o si ya se esta guardando
    if (
      this.formulario.invalid ||
      this.guardando
    ) {
      return;
    }


    // getTime convierte las fechas a numeros para poder compararlas
    // no permite que la fecha final quede antes de la inicial
    if (
      new Date(valor.fechaFin).getTime() <
      new Date(valor.fechaInicio).getTime()
    ) {

      this.errorFechas =
        'La fecha de fin debe ser igual o posterior a la fecha de inicio.';

      return;
    }


    // escoge cual id representa el destino segun el tipo de descuento
    const destino =
      valor.tipoDescuento === 'FAMILIA'
        ? valor.familiaId
        : valor.tipoDescuento === 'CATEGORIA'
          ? valor.categoriaId
          : valor.productoId;


    // si el destino queda en 0 significa que no se selecciono uno obligatorio
    if (!destino) {

      this.error =
        'Selecciona el destino obligatorio del descuento.';

      return;
    }


    // arma el objeto que se manda a la API
    const datos: IDescuento = {

      descuentoId: this.editandoId,

      // trim quita espacios al principio y al final
      nombre: valor.nombre.trim(),

      tipoDescuento: valor.tipoDescuento,

      // Number asegura que el porcentaje se mande como numero
      porcentaje: Number(valor.porcentaje),

      fechaInicio: valor.fechaInicio,

      fechaFin: valor.fechaFin,

      activo: valor.activo,


      // solamente manda familiaId cuando el tipo es FAMILIA
      familiaId:
        valor.tipoDescuento === 'FAMILIA'
          ? valor.familiaId
          : null,


      // solamente manda categoriaId cuando el tipo es CATEGORIA
      categoriaId:
        valor.tipoDescuento === 'CATEGORIA'
          ? valor.categoriaId
          : null,


      // PRODUCTO y PROMOCIONAL usan productoId como destino
      productoId:
        valor.tipoDescuento === 'PRODUCTO' ||
          valor.tipoDescuento === 'PROMOCIONAL'
          ? valor.productoId
          : null,


      // estos valores se completan desde la informacion que maneja la API
      destino: '',

      vigenciaActual: 'Inactivo'

    };


    // si editandoId tiene valor modifica
    // si vale 0 inserta un descuento nuevo
    const solicitud = this.editandoId
      ? this.servicio.modificar(datos)
      : this.servicio.insertar(datos);


    this.guardando = true;
    this.limpiarMensajes();


    solicitud

      // cuando termine vuelve a quitar el estado guardando
      .pipe(
        finalize(() => {
          this.guardando = false;
          this.cdr.markForCheck();
        })
      )

      .subscribe({

        next: () => {

          // el mensaje cambia dependiendo de si era edicion o creacion
          this.mensaje = this.editandoId
            ? 'Descuento actualizado correctamente.'
            : 'Descuento creado correctamente.';

          this.cancelar();

          // vuelve a pedir la lista para mostrar los datos actualizados
          this.recargar();
        },

        error: err => {
          this.error = this.mensajeError(err);
        }

      });
  }


  // manda a la API el estado contrario al que tiene actualmente el descuento
  cambiarEstado(descuento: IDescuento): void {

    this.servicio
      .cambiarEstado(
        descuento.descuentoId,
        !descuento.activo
      )

      .subscribe({

        next: () => {

          // el mensaje depende del estado que tenia antes de hacer el cambio
          this.mensaje = descuento.activo
            ? 'Descuento desactivado.'
            : 'Descuento activado.';

          this.recargar();
        },

        error: err => {
          this.error = this.mensajeError(err);
        }

      });
  }


  // convierte el codigo interno del tipo a un texto para mostrar en la tabla
  etiquetaTipo(tipo: TipoDescuento): string {

    // find busca el tipo correspondiente
    // si no lo encuentra devuelve directamente el codigo recibido
    return this.tipos
      .find(x => x.valor === tipo)
      ?.etiqueta ?? tipo;
  }


  // arma el nombre de la clase CSS segun la vigencia calculada por la API
  claseVigencia(vigencia: string): string {

    // toLowerCase convierte el estado a minusculas
    // por ejemplo Vigente termina como status-vigente
    return `status-${vigencia.toLowerCase()}`;
  }


  // vuelve a pedir solamente la lista de descuentos
  private recargar(): void {

    this.servicio
      .listar()

      .subscribe({

        next: respuesta => {

          this.descuentos =
            respuesta.data ?? [];

          this.cdr.markForCheck();
        },

        error: err => {
          this.error = this.mensajeError(err);
        }

      });
  }


  // revisa si la URL trajo un tipo y algun destino ya seleccionados
  // esto permite abrir el formulario desde otra pantalla con datos preparados
  private aplicarPreseleccion(): void {

    // snapshot lee los query params actuales de la URL
    // por ejemplo ?tipo=CATEGORIA&categoriaId=5
    const tipo =
      this.route.snapshot.queryParamMap.get('tipo') as
      TipoDescuento | null;


    // some revisa si el tipo recibido realmente existe dentro de los tipos permitidos
    if (
      !tipo ||
      !this.tipos.some(x => x.valor === tipo)
    ) {
      return;
    }


    // abre el formulario con los valores normales de un descuento nuevo
    this.nuevo();


    // busca los posibles ids que vengan dentro de la URL
    // si no vienen o no se pueden convertir usa 0
    const familiaId =
      Number(
        this.route.snapshot.queryParamMap.get('familiaId')
      ) || 0;

    const categoriaId =
      Number(
        this.route.snapshot.queryParamMap.get('categoriaId')
      ) || 0;

    const productoId =
      Number(
        this.route.snapshot.queryParamMap.get('productoId')
      ) || 0;


    // busca la categoria recibida para poder conocer tambien su familia
    const categoria =
      this.catalogos.categorias.find(
        x => x.categoriaId === categoriaId
      );


    // busca el producto recibido para poder sacar su familia y categoria
    const producto =
      this.catalogos.productos.find(
        x => x.productoId === productoId
      );


    // carga en el formulario la informacion que venia preseleccionada
    this.formulario.patchValue({

      tipoDescuento: tipo,


      // solamente deja familiaId si el tipo realmente es FAMILIA
      familiaId:
        tipo === 'FAMILIA'
          ? familiaId
          : 0,


      // solamente deja categoriaId si el tipo es CATEGORIA
      categoriaId:
        tipo === 'CATEGORIA'
          ? categoriaId
          : 0,


      // PRODUCTO y PROMOCIONAL usan productoId
      productoId:
        tipo === 'PRODUCTO' ||
          tipo === 'PROMOCIONAL'
          ? productoId
          : 0,


      // intenta obtener la familia desde el parametro
      // si no viene la saca de la categoria o producto encontrados
      filtroFamiliaId:
        familiaId ||
        categoria?.familiaId ||
        producto?.familiaId ||
        0,


      // si existe producto deja su categoria como filtro
      filtroCategoriaId:
        producto?.categoriaId || 0

    });
  }


  // convierte una fecha normal al formato que necesita un input datetime-local
  private fechaLocal(fecha: Date): string {

    // getTimezoneOffset devuelve la diferencia de zona horaria en minutos
    // se compensa antes de convertir la fecha para que el input muestre la hora local correcta
    const compensada =
      new Date(
        fecha.getTime() -
        fecha.getTimezoneOffset() * 60000
      );


    // toISOString genera la fecha completa
    // slice deja solamente año mes dia hora y minutos
    return compensada
      .toISOString()
      .slice(0, 16);
  }


  // limpia los mensajes antes de empezar otra accion
  private limpiarMensajes(): void {

    this.mensaje = '';
    this.error = '';
    this.errorFechas = '';
  }


  // intenta sacar primero el mensaje que venga de la API
  // si no encuentra ninguno usa un mensaje general
  private mensajeError(error: any): string {

    return error?.error?.error ||
      error?.error?.title ||
      'No fue posible completar la operación.';
  }

}
