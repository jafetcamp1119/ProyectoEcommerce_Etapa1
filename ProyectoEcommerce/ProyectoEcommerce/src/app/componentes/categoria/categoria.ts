import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ICategoria } from '../../model/ICategoria';
import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import { CategoriaService } from '../../services/categoria';
import { FamiliaProductoService } from '../../services/familia-producto';


// esta pantalla permite administrar las categorias y mantenerlas relacionadas con una familia
// tambien permite poner quitar o cambiar la imagen de cada categoria
@Component({
  selector: 'app-categoria',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './categoria.html',
  styleUrl: './categoria.css'
})
export class Categoria implements OnInit {

  // inject permite usar estas herramientas y servicios sin tener que crearlos manualmente
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly servicio = inject(CategoriaService);
  private readonly familiasServicio = inject(FamiliaProductoService);
  private readonly cdr = inject(ChangeDetectorRef);


  // aqui se guardan las categorias y familias que vienen de la API
  categorias: ICategoria[] = [];
  familias: IFamiliaProducto[] = [];

  // si familiaId tiene un valor significa que entramos desde una familia especifica
  familiaId = 0;

  // estas variables controlan diferentes estados de la pantalla
  cargando = true;
  guardando = false;
  mostrarFormulario = false;

  // si vale 0 estamos creando y si tiene un id estamos editando
  editandoId = 0;

  // aqui se guarda temporalmente el archivo que la persona selecciono
  imagenSeleccionada: File | null = null;

  // guarda la imagen que se enseña antes de guardar
  previsualizacionImagen: string | null = null;

  // sirve para saber si al guardar hay que quitar una imagen que ya existia
  eliminarImagenActual = false;

  // Set guarda las URL que ya dieron error
  // asi Angular no sigue intentando mostrar una imagen rota cada vez que actualiza la pantalla
  private readonly imagenesConError = new Set<string>();

  mensaje = '';
  error = '';

  // controla la paginacion de la tabla
  pagina = 1;
  tamanoPagina = 25;

  // estas son las cantidades de filas que la persona puede escoger
  readonly tamanos = [25, 50, 75, 100];


  // formulario para crear o editar una categoria
  // familiaId minimo 1 evita guardar una categoria sin familia
  readonly formulario = this.fb.nonNullable.group({
    familiaId: [0, Validators.min(1)],
    nombre: ['', [Validators.required, Validators.maxLength(80)]],
    descripcion: ['', Validators.maxLength(250)]
  });


  // busca dentro de familias la que tenga el mismo id que la familia abierta
  // find devuelve la primera que coincida y si no encuentra ninguna devuelve undefined
  get familiaActual(): IFamiliaProducto | undefined {
    return this.familias.find(
      x => x.familiaId === this.familiaId
    );
  }


  // calcula cuantas paginas hacen falta segun la cantidad de categorias
  get totalPaginas(): number {

    // Math.ceil redondea hacia arriba
    // por ejemplo 26 categorias entre 25 necesita 2 paginas
    // Math.max hace que siempre exista por lo menos pagina 1 aunque la lista este vacia
    return Math.max(
      1,
      Math.ceil(this.categorias.length / this.tamanoPagina)
    );
  }


  // devuelve solamente las categorias que corresponden a la pagina que estamos viendo
  get categoriasPagina(): ICategoria[] {

    // calcula desde que posicion debe empezar esta pagina
    // por ejemplo pagina 2 con 25 filas empieza desde la posicion 25
    const inicio = (this.pagina - 1) * this.tamanoPagina;

    // slice agarra solamente ese pedazo del arreglo sin modificar el arreglo original
    return this.categorias.slice(
      inicio,
      inicio + this.tamanoPagina
    );
  }


  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  ngOnInit(): void {

    // paramMap escucha los parametros que vienen en la ruta
    // por ejemplo /categorias/3 puede traer familiaId = 3
    this.route.paramMap.subscribe(params => {

      // params.get busca familiaId en la ruta
      // Number lo convierte de texto a numero y si no existe queda en 0
      this.familiaId = Number(params.get('familiaId')) || 0;

      // vuelve a pagina 1 cada vez que cambiamos de familia
      this.pagina = 1;

      this.cargarFamilias();
      this.cargarCategorias();
    });
  }


  // trae las familias que se usan en el selector y para mostrar su nombre
  cargarFamilias(): void {

    this.familiasServicio.listar().subscribe({

      // si la API responde bien guarda las familias
      // ?? [] significa que si data viene null o undefined usa un arreglo vacio
      next: respuesta => {
        this.familias = respuesta.data ?? [];
        this.cdr.markForCheck();
      },

      // si falla agarra el mensaje de error que venga de la API
      error: err => {
        this.error = this.mensajeError(err);
        this.cdr.markForCheck();
      }

    });
  }


  // carga todas las categorias o solamente las de una familia dependiendo de donde entramos
  cargarCategorias(): void {

    this.cargando = true;
    this.error = '';

    // si familiaId tiene valor usa listarPorFamilia
    // si vale 0 pide todas las categorias
    const solicitud = this.familiaId
      ? this.servicio.listarPorFamilia(this.familiaId)
      : this.servicio.listar();

    // finalize se ejecuta cuando la solicitud termina tanto si salio bien como si fallo
    solicitud
      .pipe(
        finalize(() => {
          this.cargando = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({

        next: respuesta => {

          // primero usa la lista que venga de la API o un arreglo vacio
          // sort acomoda las categorias por nombre
          // localeCompare compara los textos para saber cual va primero
          this.categorias = (respuesta.data ?? [])
            .sort((a, b) => a.nombre.localeCompare(b.nombre));
        },

        error: err => {
          this.error = this.mensajeError(err);
        }

      });
  }


  // prepara todo para crear una categoria desde cero
  nueva(): void {

    // 0 indica que no estamos editando ninguna categoria existente
    this.editandoId = 0;

    this.eliminarImagenActual = false;
    this.imagenSeleccionada = null;
    this.previsualizacionImagen = null;

    // reset limpia el formulario
    // si entramos desde una familia deja esa familia seleccionada automaticamente
    this.formulario.reset({
      familiaId: this.familiaId,
      nombre: '',
      descripcion: ''
    });

    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }


  // abre el formulario con los datos que ya tiene una categoria
  editar(categoria: ICategoria): void {

    // guarda el id para saber despues que debemos modificar y no insertar
    this.editandoId = categoria.categoriaId;

    this.eliminarImagenActual = false;
    this.imagenSeleccionada = null;

    // ?? null mantiene null si la categoria todavia no tiene imagen
    this.previsualizacionImagen = categoria.urlImagen ?? null;

    // carga en el formulario la informacion que ya estaba guardada
    this.formulario.reset({
      familiaId: categoria.familiaId,
      nombre: categoria.nombre,
      descripcion: categoria.descripcion ?? ''
    });

    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }


  // cierra el formulario y limpia todo lo temporal
  cancelar(): void {

    this.mostrarFormulario = false;
    this.editandoId = 0;

    this.eliminarImagenActual = false;
    this.imagenSeleccionada = null;
    this.previsualizacionImagen = null;

    this.formulario.reset();
  }


  // recibe el archivo que la persona escogio desde el input de imagen
  seleccionarImagen(event: Event): void {

    // event.target es el input que genero el evento
    // as HTMLInputElement permite usar cosas propias de un input como files
    const input = event.target as HTMLInputElement;

    // si no selecciono ningun archivo no hace nada
    if (!input.files?.length) {
      return;
    }

    // agarra el primer archivo seleccionado
    const archivo = input.files[0];

    // startsWith revisa que el tipo del archivo empiece con image/
    // asi evita aceptar un pdf o cualquier otro archivo
    if (!archivo.type.startsWith('image/')) {
      this.error = 'Debe seleccionar un archivo de imagen.';
      input.value = '';
      return;
    }

    // 5 * 1024 * 1024 representa aproximadamente 5 MB en bytes
    if (archivo.size > 5 * 1024 * 1024) {
      this.error = 'La imagen no puede superar los 5 MB.';
      input.value = '';
      return;
    }

    // guarda el archivo para subirlo despues de guardar la categoria
    this.imagenSeleccionada = archivo;
    this.eliminarImagenActual = false;

    // FileReader permite leer el archivo que se escogio desde la computadora
    const lector = new FileReader();

    // onload se ejecuta cuando FileReader termina de leer la imagen
    lector.onload = () => {

      // guarda el resultado para poder mostrarlo inmediatamente en el HTML
      this.previsualizacionImagen = lector.result as string;

      this.cdr.markForCheck();
    };

    // readAsDataURL convierte la imagen en una direccion temporal que puede usar [src]
    // esto sirve para mostrar la vista previa sin tener que subirla primero
    lector.readAsDataURL(archivo);

    this.error = '';
  }


  // quita la imagen que se esta mostrando en el formulario
  quitarImagenSeleccionada(): void {

    // si acabamos de escoger una imagen nueva primero quitamos ese archivo temporal
    if (this.imagenSeleccionada) {

      this.imagenSeleccionada = null;

      // busca la categoria que estabamos editando para saber si ya tenia una imagen guardada
      const actual = this.categorias.find(
        x => x.categoriaId === this.editandoId
      );

      // !! convierte el resultado en true o false
      // si antes tenia imagen queda marcado que tambien hay que borrarla
      this.eliminarImagenActual = !!actual?.urlImagen;

      this.previsualizacionImagen = null;

      this.cdr.markForCheck();
      return;
    }

    // si estamos editando y existe una imagen actual
    // marca que al guardar urlImagen debe quedar en null
    if (this.editandoId > 0 && this.previsualizacionImagen) {

      this.eliminarImagenActual = true;
      this.previsualizacionImagen = null;

      this.cdr.markForCheck();
    }
  }


  // guarda primero los datos de la categoria
  // si tambien escogieron una imagen la sube despues usando el id de la categoria
  guardar(): void {

    // marca todos los campos como tocados para enseñar sus errores si existen
    this.formulario.markAllAsTouched();

    // no continua si el formulario esta malo o ya hay otro guardado en proceso
    if (this.formulario.invalid || this.guardando) {
      return;
    }

    // getRawValue agarra todos los valores actuales del formulario
    const valores = this.formulario.getRawValue();

    // busca la categoria que ya existe si estamos editando
    // esto sirve para conservar cosas como estado e imagen
    const actual = this.categorias.find(
      x => x.categoriaId === this.editandoId
    );

    // arma el objeto que se va a mandar a la API
    const datos: ICategoria = {

      categoriaId: this.editandoId,

      familiaId: valores.familiaId,

      // trim quita espacios que hayan quedado al inicio o al final
      nombre: valores.nombre.trim(),

      // si despues de quitar espacios queda vacio guarda null
      descripcion: valores.descripcion.trim() || null,

      // si se pidio eliminar la imagen manda null
      // si no mantiene la URL que ya tenia la categoria
      urlImagen: this.eliminarImagenActual
        ? null
        : actual?.urlImagen ?? null,

      // si estamos creando una nueva queda activa por defecto
      activo: actual?.activo ?? true
    };

    // si editandoId es mayor a 0 significa que ya existe
    const editando = this.editandoId > 0;

    // dependiendo de editando decide si llama modificar o insertar
    const solicitud = editando
      ? this.servicio.modificar(datos)
      : this.servicio.insertar(datos);

    this.guardando = true;
    this.limpiarMensajes();

    // subscribe manda la solicitud y separa lo que pasa si responde bien o mal
    solicitud.subscribe({

      next: respuesta => {

        // intenta sacar el id que devolvio la API
        // si estamos editando y no vino usa el id que ya teniamos
        const categoriaId =
          respuesta.data?.categoriaId ?? this.editandoId;

        // sin id no podemos subir una imagen porque no sabriamos a que categoria pertenece
        if (!categoriaId) {

          this.guardando = false;

          this.error =
            'La categoría se guardó, pero no se pudo obtener su ID.';

          this.cdr.markForCheck();
          return;
        }

        // si no hay una nueva imagen el proceso termina aqui
        if (!this.imagenSeleccionada) {

          this.guardando = false;

          this.mensaje = editando
            ? 'Categoría actualizada correctamente.'
            : 'Categoría creada correctamente.';

          this.cancelar();
          this.cargarCategorias();

          this.cdr.markForCheck();
          return;
        }

        // si existe una nueva imagen la sube despues de guardar la categoria
        this.servicio
          .subirImagen(
            categoriaId,
            this.imagenSeleccionada
          )
          .subscribe({

            // si tambien se pudo subir la imagen termina todo el proceso
            next: () => {

              this.guardando = false;

              this.mensaje = editando
                ? 'Categoría e imagen actualizadas correctamente.'
                : 'Categoría e imagen creadas correctamente.';

              this.cancelar();
              this.cargarCategorias();

              this.cdr.markForCheck();
            },

            // la categoria pudo guardarse pero la subida de imagen fallo
            error: err => {

              this.guardando = false;
              this.error = this.mensajeError(err);

              this.cdr.markForCheck();
            }

          });
      },

      // entra aqui si falla el guardado de la categoria
      error: err => {

        this.guardando = false;
        this.error = this.mensajeError(err);

        this.cdr.markForCheck();
      }

    });
  }


  // cambia una categoria entre activa e inactiva
  // desactivar usa el DELETE logico del proyecto y activar vuelve a mandar activo true
  cambiarEstado(categoria: ICategoria): void {

    this.limpiarMensajes();

    // esta funcion se reutiliza tanto al activar como al desactivar
    const alCompletar = () => {

      this.mensaje = categoria.activo
        ? 'Categoría desactivada.'
        : 'Categoría activada.';

      this.cargarCategorias();
    };

    // funcion comun para manejar cualquier error de las 2 operaciones
    const alFallar = (err: any) => {
      this.error = this.mensajeError(err);
      this.cdr.markForCheck();
    };

    // si actualmente esta activa llama eliminar que en este proyecto la desactiva
    if (categoria.activo) {

      this.servicio
        .eliminar(categoria.categoriaId)
        .subscribe({
          next: alCompletar,
          error: alFallar
        });

    } else {

      // ...categoria copia todas las propiedades que ya tenia
      // y despues activo true reemplaza solamente ese valor
      this.servicio
        .modificar({
          ...categoria,
          activo: true
        })
        .subscribe({
          next: alCompletar,
          error: alFallar
        });

    }
  }


  // busca el nombre de una familia usando su id
  nombreFamilia(id: number): string {

    // ?.nombre intenta sacar el nombre solamente si find encontro la familia
    // ?? Familia usa ese texto si no encontro nada
    return this.familias.find(
      x => x.familiaId === id
    )?.nombre ?? 'Familia';
  }


  // decide si una imagen se puede seguir mostrando
  mostrarImagen(url: string | null): boolean {

    // tiene que existir una URL y ademas no estar dentro de las que ya fallaron
    return !!url && !this.imagenesConError.has(url);
  }


  // guarda una URL dentro del Set cuando el navegador no pudo cargar esa imagen
  marcarImagenConError(url: string | null): void {

    if (url) {
      this.imagenesConError.add(url);
    }
  }


  // cambia cuantas categorias se muestran por pagina
  cambiarTamano(valor: string): void {

    // el valor del select llega como texto y Number lo convierte a numero
    this.tamanoPagina = Number(valor);

    // vuelve a pagina 1 porque la cantidad de paginas acaba de cambiar
    this.pagina = 1;
  }


  // mueve la tabla una pagina hacia adelante o hacia atras
  irPagina(delta: number): void {

    // pagina + delta calcula la pagina nueva
    // Math.max evita bajar de 1
    // Math.min evita pasar de la ultima pagina
    this.pagina = Math.min(
      this.totalPaginas,
      Math.max(1, this.pagina + delta)
    );
  }


  // limpia mensajes anteriores antes de hacer otra accion
  private limpiarMensajes(): void {
    this.mensaje = '';
    this.error = '';
  }


  // intenta sacar el mensaje que mando la API
  // si no encuentra ninguno usa uno general
  private mensajeError(error: any): string {

    return error?.error?.error
      || error?.error?.title
      || 'No fue posible completar la operación.';
  }

}
