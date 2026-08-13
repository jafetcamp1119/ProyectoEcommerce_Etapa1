import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ICategoria } from '../../model/ICategoria';
import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import { CategoriaService } from '../../services/categoria';
import { FamiliaProductoService } from '../../services/familia-producto';

/** Pantalla administrativa de categorías y de su relación obligatoria con una familia. */
@Component({
  selector: 'app-categoria',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './categoria.html',
  styleUrl: './categoria.css'
})
export class Categoria implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly servicio = inject(CategoriaService);
  private readonly familiasServicio = inject(FamiliaProductoService);
  private readonly cdr = inject(ChangeDetectorRef);

  categorias: ICategoria[] = [];
  familias: IFamiliaProducto[] = [];
  familiaId = 0;
  cargando = true;
  guardando = false;
  mostrarFormulario = false;
  editandoId = 0;
  imagenSeleccionada: File | null = null;
  previsualizacionImagen: string | null = null;
  eliminarImagenActual = false;
  mensaje = '';
  error = '';
  pagina = 1;
  tamanoPagina = 25;
  readonly tamanos = [25, 50, 75, 100];

  readonly formulario = this.fb.nonNullable.group({
    familiaId: [0, Validators.min(1)],
    nombre: ['', [Validators.required, Validators.maxLength(80)]],
    descripcion: ['', Validators.maxLength(250)]
  });

  get familiaActual(): IFamiliaProducto | undefined {
    return this.familias.find(x => x.familiaId === this.familiaId);
  }
  get totalPaginas(): number { return Math.max(1, Math.ceil(this.categorias.length / this.tamanoPagina)); }
  get categoriasPagina(): ICategoria[] {
    const inicio = (this.pagina - 1) * this.tamanoPagina;
    return this.categorias.slice(inicio, inicio + this.tamanoPagina);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.familiaId = Number(params.get('familiaId')) || 0;
      this.pagina = 1;
      this.cargarFamilias();
      this.cargarCategorias();
    });
  }

  cargarFamilias(): void {
    this.familiasServicio.listar().subscribe({
      next: respuesta => { this.familias = respuesta.data ?? []; this.cdr.markForCheck(); },
      error: err => { this.error = this.mensajeError(err); this.cdr.markForCheck(); }
    });
  }

  cargarCategorias(): void {
    this.cargando = true;
    this.error = '';
    const solicitud = this.familiaId
      ? this.servicio.listarPorFamilia(this.familiaId)
      : this.servicio.listar();
    solicitud.pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); })).subscribe({
      next: respuesta => this.categorias = (respuesta.data ?? []).sort((a, b) => a.nombre.localeCompare(b.nombre)),
      error: err => this.error = this.mensajeError(err)
    });
  }

  nueva(): void {
    this.editandoId = 0;
    this.eliminarImagenActual = false;
    this.imagenSeleccionada = null;
    this.previsualizacionImagen = null;

    this.formulario.reset({
      familiaId: this.familiaId,
      nombre: '',
      descripcion: ''
    });

    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  editar(categoria: ICategoria): void {
    this.editandoId = categoria.categoriaId;
    this.eliminarImagenActual = false;
    this.imagenSeleccionada = null;
    this.previsualizacionImagen = categoria.urlImagen ?? null;

    this.formulario.reset({
      familiaId: categoria.familiaId,
      nombre: categoria.nombre,
      descripcion: categoria.descripcion ?? ''
    });

    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  cancelar(): void {
    this.mostrarFormulario = false;
    this.editandoId = 0;
    this.eliminarImagenActual = false;
    this.imagenSeleccionada = null;
    this.previsualizacionImagen = null;
    this.formulario.reset();
  }


  seleccionarImagen(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files?.length) {
      return;
    }

    const archivo = input.files[0];

    if (!archivo.type.startsWith('image/')) {
      this.error = 'Debe seleccionar un archivo de imagen.';
      input.value = '';
      return;
    }

    if (archivo.size > 5 * 1024 * 1024) {
      this.error = 'La imagen no puede superar los 5 MB.';
      input.value = '';
      return;
    }

    this.imagenSeleccionada = archivo;
    this.eliminarImagenActual = false;

    const lector = new FileReader();

    lector.onload = () => {
      this.previsualizacionImagen = lector.result as string;
      this.cdr.markForCheck();
    };

    lector.readAsDataURL(archivo);

    this.error = '';
  }

  quitarImagenSeleccionada(): void {
    if (this.imagenSeleccionada) {
      this.imagenSeleccionada = null;
      this.previsualizacionImagen = null;
      this.cdr.markForCheck();
      return;
    }

    if (this.editandoId > 0 && this.previsualizacionImagen) {
      this.eliminarImagenActual = true;
      this.previsualizacionImagen = null;
      this.cdr.markForCheck();
    }
  }



  guardar(): void {
    this.formulario.markAllAsTouched();

    if (this.formulario.invalid || this.guardando) {
      return;
    }

    if (
      this.editandoId > 0 &&
      this.eliminarImagenActual &&
      !this.imagenSeleccionada
    ) {
      this.error =
        'Debes seleccionar una nueva imagen antes de guardar la categoría.';
      this.cdr.markForCheck();
      return;
    }

    const valores = this.formulario.getRawValue();

    const actual = this.categorias.find(
      x => x.categoriaId === this.editandoId
    );

    const datos: ICategoria = {
      categoriaId: this.editandoId,
      familiaId: valores.familiaId,
      nombre: valores.nombre.trim(),
      descripcion: valores.descripcion.trim() || null,
      urlImagen: this.eliminarImagenActual
        ? null
        : actual?.urlImagen ?? null,
      activo: actual?.activo ?? true
    };

    const editando = this.editandoId > 0;

    const solicitud = editando
      ? this.servicio.modificar(datos)
      : this.servicio.insertar(datos);

    this.guardando = true;
    this.limpiarMensajes();

    solicitud.subscribe({
      next: respuesta => {
        const categoriaId =
          respuesta.data?.categoriaId ?? this.editandoId;

        if (!categoriaId) {
          this.guardando = false;
          this.error =
            'La categoría se guardó, pero no se pudo obtener su ID.';
          this.cdr.markForCheck();
          return;
        }

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

        this.servicio
          .subirImagen(
            categoriaId,
            this.imagenSeleccionada
          )
          .subscribe({
            next: () => {
              this.guardando = false;

              this.mensaje = editando
                ? 'Categoría e imagen actualizadas correctamente.'
                : 'Categoría e imagen creadas correctamente.';

              this.cancelar();
              this.cargarCategorias();
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

  cambiarEstado(categoria: ICategoria): void {
    this.limpiarMensajes();
    const alCompletar = () => {
      this.mensaje = categoria.activo ? 'Categoría desactivada.' : 'Categoría activada.';
      this.cargarCategorias();
    };
    const alFallar = (err: any) => { this.error = this.mensajeError(err); this.cdr.markForCheck(); };
    if (categoria.activo) {
      this.servicio.eliminar(categoria.categoriaId).subscribe({ next: alCompletar, error: alFallar });
    } else {
      this.servicio.modificar({ ...categoria, activo: true }).subscribe({ next: alCompletar, error: alFallar });
    }
  }

  nombreFamilia(id: number): string { return this.familias.find(x => x.familiaId === id)?.nombre ?? 'Familia'; }
  cambiarTamano(valor: string): void { this.tamanoPagina = Number(valor); this.pagina = 1; }
  irPagina(delta: number): void { this.pagina = Math.min(this.totalPaginas, Math.max(1, this.pagina + delta)); }
  private limpiarMensajes(): void { this.mensaje = ''; this.error = ''; }
  private mensajeError(error: any): string { return error?.error?.error || error?.error?.title || 'No fue posible completar la operación.'; }
}
