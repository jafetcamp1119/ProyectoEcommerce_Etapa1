import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import { FamiliaProductoService } from '../../services/familia-producto';


// esta pantalla permite listar crear editar activar y desactivar familias
// tambien maneja la imagen opcional que puede tener cada familia
@Component({
  selector: 'app-familia-producto',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './familia-producto.html',
  styleUrl: './familia-producto.css'
})
export class FamiliaProducto implements OnInit {

  // herramientas y servicios que usa este componente
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(FamiliaProductoService);
  private readonly cdr = inject(ChangeDetectorRef);


  // aqui se guardan todas las familias que vienen de la API
  familias: IFamiliaProducto[] = [];

  // controlan diferentes estados de la pantalla
  cargando = true;
  guardando = false;
  mostrarFormulario = false;

  // mensajes que se muestran despues de las operaciones
  mensaje = '';
  error = '';

  // si vale 0 estamos creando
  // si tiene un id estamos editando una familia existente
  editandoId = 0;

  // indica si al guardar se debe quitar la imagen que ya tenia la familia
  eliminarImagenActual = false;


  // guarda las URL que ya dieron error al intentar mostrar la imagen
  // Set evita guardar la misma URL varias veces
  private readonly imagenesConError = new Set<string>();


  // guarda temporalmente el archivo nuevo que escogio la persona
  imagenSeleccionada: File | null = null;


  // guarda la imagen que se enseña como vista previa antes de subirla
  previsualizacionImagen: string | null = null;


  // formulario para crear o editar una familia
  readonly formulario = this.fb.nonNullable.group({

    nombre: [
      '',
      [
        Validators.required,
        Validators.maxLength(80)
      ]
    ],

    descripcion: [
      '',
      Validators.maxLength(250)
    ]

  });


  // ngOnInit se ejecuta automaticamente cuando se abre este componente
  // llama cargar para traer las familias desde la API
  ngOnInit(): void {
    this.cargar();
  }


  // trae todas las familias y las acomoda por nombre
  cargar(): void {

    this.cargando = true;
    this.error = '';

    this.servicio
      .listar()

      // finalize se ejecuta siempre cuando termina la solicitud
      // no importa si termino correctamente o con error
      .pipe(
        finalize(() => {

          this.cargando = false;

          // le avisa a Angular que vuelva a revisar la pantalla
          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: respuesta => {

          // ?? [] usa un arreglo vacio si la API devuelve null o undefined
          // sort acomoda las familias alfabeticamente por nombre
          // localeCompare compara los textos para decidir cual va primero
          this.familias = (respuesta.data ?? [])
            .sort(
              (a, b) => a.nombre.localeCompare(b.nombre)
            );

        },

        // si falla intenta sacar el mensaje que vino de la API
        error: err => {
          this.error = this.mensajeError(err);
        }

      });
  }


  // abre un formulario limpio para crear una familia nueva
  nuevo(): void {

    // 0 indica que no estamos editando ninguna familia existente
    this.editandoId = 0;

    // no hay ninguna imagen anterior pendiente de eliminar
    this.eliminarImagenActual = false;

    // limpia cualquier archivo que se hubiera seleccionado antes
    this.imagenSeleccionada = null;
    this.previsualizacionImagen = null;


    // reset deja los campos del formulario vacios
    this.formulario.reset({
      nombre: '',
      descripcion: ''
    });


    this.mostrarFormulario = true;

    this.limpiarMensajes();
  }


  // abre el formulario con la informacion que ya tiene la familia escogida
  editar(familia: IFamiliaProducto): void {

    // guarda el id para saber cual familia se va a modificar
    this.editandoId = familia.familiaId;

    this.eliminarImagenActual = false;

    // cuando apenas se abre la edicion todavia no existe una imagen nueva seleccionada
    this.imagenSeleccionada = null;

    // muestra como vista previa la imagen que ya estaba guardada
    // si no existe utiliza null
    this.previsualizacionImagen =
      familia.urlImagen ?? null;


    // carga los datos actuales dentro del formulario
    this.formulario.reset({

      nombre: familia.nombre,

      descripcion:
        familia.descripcion ?? ''

    });


    this.mostrarFormulario = true;

    this.limpiarMensajes();
  }


  // cierra el formulario y limpia los datos temporales
  // no guarda ningun cambio
  cancelar(): void {

    this.mostrarFormulario = false;

    this.editandoId = 0;

    this.formulario.reset();


    // limpia el archivo seleccionado y su vista previa
    this.imagenSeleccionada = null;

    this.previsualizacionImagen = null;
  }


  // recibe el archivo seleccionado desde el input del HTML
  // valida que sea una imagen y prepara una vista previa sin subirla todavia
  seleccionarImagen(event: Event): void {

    // event.target es el input que genero el evento
    // as HTMLInputElement permite usar propiedades como files y value
    const input =
      event.target as HTMLInputElement;


    // si la persona no escogio ningun archivo no hace nada
    if (!input.files?.length) {
      return;
    }


    // agarra el primer archivo seleccionado
    const archivo = input.files[0];


    // startsWith revisa que el tipo del archivo empiece con image/
    // asi evita aceptar archivos que no sean imagenes
    if (!archivo.type.startsWith('image/')) {

      this.error =
        'Debe seleccionar un archivo de imagen.';

      // limpia el input para quitar el archivo incorrecto
      input.value = '';

      return;
    }


    // 5 * 1024 * 1024 representa 5 MB en bytes
    if (archivo.size > 5 * 1024 * 1024) {

      this.error =
        'La imagen no puede superar los 5 MB.';

      input.value = '';

      return;
    }


    // guarda el archivo temporalmente para subirlo despues
    this.imagenSeleccionada = archivo;


    // como existe una imagen nueva ya no queda pendiente
    // eliminar la anterior sin reemplazarla
    this.eliminarImagenActual = false;


    // FileReader permite leer el archivo que se escogio desde la computadora
    const lector = new FileReader();


    // onload se ejecuta cuando FileReader termina de leer la imagen
    lector.onload = () => {

      // guarda el resultado como texto para poder usarlo en el src de la imagen
      this.previsualizacionImagen =
        lector.result as string;

      this.cdr.markForCheck();

    };


    // readAsDataURL convierte el archivo en una direccion temporal
    // que puede usar el [src] del HTML para mostrar la vista previa
    lector.readAsDataURL(archivo);


    this.error = '';
  }


  // quita la imagen que actualmente se esta mostrando en el formulario
  quitarImagenSeleccionada(): void {

    // si habia una imagen nueva seleccionada primero la quita de memoria
    if (this.imagenSeleccionada) {

      this.imagenSeleccionada = null;


      // busca la familia que se estaba editando
      const actual = this.familias.find(
        x => x.familiaId === this.editandoId
      );


      // !! convierte el resultado en true o false
      // si la familia tenia una imagen anterior queda marcado que se debe eliminar
      this.eliminarImagenActual =
        !!actual?.urlImagen;


      this.previsualizacionImagen = null;

      this.cdr.markForCheck();

      return;
    }


    // si estamos editando una familia que ya tenia imagen
    // deja marcado que al guardar esa imagen debe quedar en null
    if (
      this.editandoId > 0 &&
      this.previsualizacionImagen
    ) {

      this.eliminarImagenActual = true;

      this.previsualizacionImagen = null;

      this.cdr.markForCheck();

    }
  }


  // guarda primero los datos de la familia
  // cuando ya tiene un ID sube la imagen nueva en una segunda solicitud
  guardar(): void {

    // hace que se muestren los errores de los campos si existen
    this.formulario.markAllAsTouched();


    // no continua si el formulario tiene errores
    // o si ya existe otra solicitud de guardado
    if (
      this.formulario.invalid ||
      this.guardando
    ) {
      return;
    }


    // getRawValue agarra todos los valores actuales del formulario
    const valores =
      this.formulario.getRawValue();


    // si estamos editando busca la familia original
    // esto permite conservar datos que no cambiaron
    const actual = this.familias.find(
      x => x.familiaId === this.editandoId
    );


    // arma el objeto que se va a mandar a la API
    const datos: IFamiliaProducto = {

      familiaId: this.editandoId,


      // trim quita espacios que hayan quedado al inicio o al final
      nombre:
        valores.nombre.trim(),


      // si despues de limpiar queda vacia guarda null
      descripcion:
        valores.descripcion.trim() || null,


      // si se pidio quitar la imagen manda null
      // si no conserva la URL que ya tenia la familia
      urlImagen:
        this.eliminarImagenActual
          ? null
          : actual?.urlImagen ?? null,


      // si estamos creando una nueva familia queda activa por defecto
      activo:
        actual?.activo ?? true

    };


    // true significa que ya existe una familia y se va a modificar
    const editando =
      this.editandoId > 0;


    // escoge el metodo del servicio dependiendo de si se crea o se edita
    const solicitud =
      editando
        ? this.servicio.modificar(datos)
        : this.servicio.insertar(datos);


    this.guardando = true;

    this.limpiarMensajes();


    // ejecuta la solicitud para guardar los datos de la familia
    solicitud.subscribe({

      next: respuesta => {

        // intenta sacar el id que devolvio la API
        // si estamos editando y no viene usa el id que ya teniamos
        const familiaId =
          respuesta.data?.familiaId ??
          this.editandoId;


        // si no se pudo obtener el id no se puede continuar con la imagen
        if (!familiaId) {

          this.guardando = false;

          this.error =
            'La familia se guardó, pero no se pudo obtener su ID.';


          this.cdr.markForCheck();

          return;
        }


        // si no se escogio una imagen nueva el proceso termina aqui
        if (!this.imagenSeleccionada) {

          this.guardando = false;


          // el mensaje cambia dependiendo de si era creacion o edicion
          this.mensaje =
            editando
              ? 'Familia actualizada correctamente.'
              : 'Familia creada correctamente.';


          this.cancelar();

          // vuelve a cargar la lista para mostrar los datos actualizados
          this.cargar();

          this.cdr.markForCheck();

          return;
        }


        // si existe una imagen nueva la sube despues de guardar la familia
        this.servicio
          .subirImagen(
            familiaId,
            this.imagenSeleccionada
          )
          .subscribe({

            // entra aqui si tambien se pudo subir la imagen
            next: () => {

              this.guardando = false;


              this.mensaje =
                editando
                  ? 'Familia e imagen actualizadas correctamente.'
                  : 'Familia e imagen creadas correctamente.';


              this.cancelar();

              this.cargar();

              this.cdr.markForCheck();

            },


            // entra aqui si fallo la solicitud de subir la imagen
            error: err => {

              this.guardando = false;

              this.error =
                this.mensajeError(err);

              this.cdr.markForCheck();

            }

          });

      },


      // entra aqui si fallo el guardado de la familia
      error: err => {

        this.guardando = false;

        this.error =
          this.mensajeError(err);

        this.cdr.markForCheck();

      }

    });
  }


  // cambia la familia entre activa e inactiva
  cambiarEstado(
    familia: IFamiliaProducto
  ): void {

    this.limpiarMensajes();


    // esta funcion se usa cuando cualquiera de las 2 operaciones termina bien
    const alCompletar = () => {

      this.mensaje =
        familia.activo
          ? 'Familia desactivada.'
          : 'Familia activada.';

      this.cargar();

    };


    // esta funcion maneja el error tanto al activar como al desactivar
    const alFallar = (err: any) => {

      this.error =
        this.mensajeError(err);

      this.cdr.markForCheck();

    };


    // si actualmente esta activa llama eliminar
    // en este proyecto esa accion hace una desactivacion logica
    if (familia.activo) {

      this.servicio
        .eliminar(familia.familiaId)
        .subscribe({
          next: alCompletar,
          error: alFallar
        });

    } else {

      // ...familia copia todas las propiedades que ya tenia
      // despues activo true reemplaza solamente ese valor
      this.servicio
        .modificar({
          ...familia,
          activo: true
        })
        .subscribe({
          next: alCompletar,
          error: alFallar
        });

    }
  }


  // crea un texto con hasta las primeras 2 iniciales del nombre
  // se usa como placeholder cuando la familia no tiene imagen
  iniciales(nombre: string): string {

    // split separa el nombre por espacios
    // slice deja solamente las primeras 2 palabras
    // map agarra la primera letra de cada una
    // join las une y toUpperCase las pasa a mayusculas
    return nombre
      .split(' ')
      .slice(0, 2)
      .map(x => x[0])
      .join('')
      .toUpperCase();
  }


  // revisa si la imagen se puede mostrar
  mostrarImagen(
    url: string | null
  ): boolean {

    // tiene que existir una URL y no puede estar dentro de las que ya fallaron
    return !!url &&
      !this.imagenesConError.has(url);
  }


  // cuando una imagen falla guarda su URL para no seguir intentando mostrarla
  marcarImagenConError(
    url: string | null
  ): void {

    if (url) {
      this.imagenesConError.add(url);
    }
  }


  // limpia cualquier mensaje anterior
  private limpiarMensajes(): void {

    this.mensaje = '';
    this.error = '';
  }


  // intenta sacar primero el mensaje que mando la API
  // si no encuentra ninguno usa un mensaje general
  private mensajeError(error: any): string {

    return error?.error?.error ||
      error?.error?.title ||
      'No fue posible completar la operación.';
  }

}
