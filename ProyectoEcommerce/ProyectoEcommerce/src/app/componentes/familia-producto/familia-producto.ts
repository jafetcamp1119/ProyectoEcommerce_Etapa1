import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import { FamiliaProductoService } from '../../services/familia-producto';

/** Pantalla administrativa para listar, crear, editar y desactivar familias de producto. */
@Component({
  selector: 'app-familia-producto',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './familia-producto.html',
  styleUrl: './familia-producto.css'
})
export class FamiliaProducto implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(FamiliaProductoService);
  private readonly cdr = inject(ChangeDetectorRef);

  familias: IFamiliaProducto[] = [];
  cargando = true;
  guardando = false;
  mostrarFormulario = false;
  mensaje = '';
  error = '';
  editandoId = 0;
  eliminarImagenActual = false;

  // Guarda temporalmente la imagen seleccionada para la familia.
  imagenSeleccionada: File | null = null;

  // Guarda la vista previa de la imagen antes de subirla.
  previsualizacionImagen: string | null = null;

  readonly formulario = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(80)]],
    descripcion: ['', Validators.maxLength(250)]
  });

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.cargando = true;
    this.error = '';
    this.servicio.listar().pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); })).subscribe({
      next: respuesta => this.familias = (respuesta.data ?? []).sort((a, b) => a.nombre.localeCompare(b.nombre)),
      error: err => this.error = this.mensajeError(err)
    });
  }

  nuevo(): void {
    this.editandoId = 0;
    this.eliminarImagenActual = false;
    // Limpia cualquier imagen seleccionada anteriormente.
    this.imagenSeleccionada = null;
    this.previsualizacionImagen = null;
    this.formulario.reset({
      nombre: '',
      descripcion: ''
    });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  editar(familia: IFamiliaProducto): void {
    this.editandoId = familia.familiaId;
    this.eliminarImagenActual = false;
    // Todavía no hay una imagen nueva seleccionada.
    this.imagenSeleccionada = null;
    // Muestra la imagen que ya tiene guardada la familia.
    this.previsualizacionImagen = familia.urlImagen ?? null;
    this.formulario.reset({
      nombre: familia.nombre,
      descripcion: familia.descripcion ?? ''
    });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }


  cancelar(): void {
    this.mostrarFormulario = false;
    this.editandoId = 0;
    this.formulario.reset();

    // Limpia la imagen seleccionada y su vista previa.
    this.imagenSeleccionada = null;
    this.previsualizacionImagen = null;
  }

 
  seleccionarImagen(event: Event): void {
    const input = event.target as HTMLInputElement;

    // Verifica que se haya seleccionado una imagen.
    if (!input.files?.length) {
      return;
    }

    const archivo = input.files[0];

    // Verifica que el archivo sea una imagen.
    if (!archivo.type.startsWith('image/')) {
      this.error = 'Debe seleccionar un archivo de imagen.';
      input.value = '';
      return;
    }

    // No permite imágenes mayores a 5 MB.
    if (archivo.size > 5 * 1024 * 1024) {
      this.error = 'La imagen no puede superar los 5 MB.';
      input.value = '';
      return;
    }

    // Guarda temporalmente la imagen seleccionada.
    this.imagenSeleccionada = archivo;

    // Como ya se seleccionó una imagen nueva,
    // ya no queda pendiente eliminar la anterior sin reemplazo.
    this.eliminarImagenActual = false;

    // Crea la vista previa de la imagen.
    const lector = new FileReader();

    lector.onload = () => {
      this.previsualizacionImagen = lector.result as string;
      this.cdr.markForCheck();
    };

    lector.readAsDataURL(archivo);

    this.error = '';
  }

  quitarImagenSeleccionada(): void {
    // Si había una imagen nueva seleccionada,
    // solamente elimina esa selección.
    if (this.imagenSeleccionada) {
      this.imagenSeleccionada = null;
      this.previsualizacionImagen = null;
      this.cdr.markForCheck();
      return;
    }
    // Si estamos editando una familia que ya tenía imagen,
    // marca que la imagen actual fue quitada.
    if (this.editandoId > 0 && this.previsualizacionImagen) {
      this.eliminarImagenActual = true;
      this.previsualizacionImagen = null;
      this.cdr.markForCheck();
    }
  }

  


  guardar(): void {

    // Valida los campos del formulario.
    this.formulario.markAllAsTouched();

    if (this.formulario.invalid || this.guardando) {
      return;
    }

    // Si se quitó la imagen actual al editar,
    // obliga a seleccionar una nueva antes de guardar.
    if (
      this.editandoId > 0 &&
      this.eliminarImagenActual &&
      !this.imagenSeleccionada
    ) {
      this.error =
        'Debes seleccionar una nueva imagen antes de guardar la familia.';

      this.cdr.markForCheck();
      return;
    }

    // Obtiene los valores escritos en el formulario.
    const valores = this.formulario.getRawValue();

    // Busca la familia actual cuando se está editando.
    const actual = this.familias.find(
      x => x.familiaId === this.editandoId
    );

    // Prepara los datos que se enviarán a la API.
    const datos: IFamiliaProducto = {
      familiaId: this.editandoId,
      nombre: valores.nombre.trim(),
      descripcion: valores.descripcion.trim() || null,

      // Si se quitó la imagen actual, envía null.
      // Si no, conserva la imagen que ya tenía.
      urlImagen: this.eliminarImagenActual
        ? null
        : actual?.urlImagen ?? null,

      activo: actual?.activo ?? true
    };

    // Determina si estamos creando o editando una familia.
    const editando = this.editandoId > 0;

    const solicitud = editando
      ? this.servicio.modificar(datos)
      : this.servicio.insertar(datos);

    this.guardando = true;
    this.limpiarMensajes();

    solicitud.subscribe({
      next: respuesta => {

        // Obtiene el ID de la familia creada o editada.
        const familiaId =
          respuesta.data?.familiaId ?? this.editandoId;

        if (!familiaId) {
          this.guardando = false;
          this.error =
            'La familia se guardó, pero no se pudo obtener su ID.';

          this.cdr.markForCheck();
          return;
        }

        // Si no se seleccionó una imagen nueva,
        // termina normalmente.
        if (!this.imagenSeleccionada) {
          this.guardando = false;

          this.mensaje = editando
            ? 'Familia actualizada correctamente.'
            : 'Familia creada correctamente.';

          this.cancelar();
          this.cargar();
          this.cdr.markForCheck();
          return;
        }

        // Después de guardar la familia,
        // sube la imagen nueva seleccionada.
        this.servicio
          .subirImagen(
            familiaId,
            this.imagenSeleccionada
          )
          .subscribe({

            next: () => {
              this.guardando = false;

              this.mensaje = editando
                ? 'Familia e imagen actualizadas correctamente.'
                : 'Familia e imagen creadas correctamente.';

              this.cancelar();
              this.cargar();
              this.cdr.markForCheck();
            },

            error: err => {
              this.guardando = false;
              this.error = this.mensajeError(err);
              this.cdr.markForCheck();
            }
          });
      },

      error: err => {
        this.guardando = false;
        this.error = this.mensajeError(err);
        this.cdr.markForCheck();
      }
    });
  }

  cambiarEstado(familia: IFamiliaProducto): void {
    this.limpiarMensajes();
    const alCompletar = () => {
      this.mensaje = familia.activo ? 'Familia desactivada.' : 'Familia activada.';
      this.cargar();
    };
    const alFallar = (err: any) => { this.error = this.mensajeError(err); this.cdr.markForCheck(); };
    if (familia.activo) {
      this.servicio.eliminar(familia.familiaId).subscribe({ next: alCompletar, error: alFallar });
    } else {
      this.servicio.modificar({ ...familia, activo: true }).subscribe({ next: alCompletar, error: alFallar });
    }
  }

  iniciales(nombre: string): string {
    return nombre.split(' ').slice(0, 2).map(x => x[0]).join('').toUpperCase();
  }

  private limpiarMensajes(): void { this.mensaje = ''; this.error = ''; }
  private mensajeError(error: any): string {
    return error?.error?.error || error?.error?.title || 'No fue posible completar la operación.';
  }
}
