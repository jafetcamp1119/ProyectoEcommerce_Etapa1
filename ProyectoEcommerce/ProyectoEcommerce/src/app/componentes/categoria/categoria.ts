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
    this.formulario.reset({ familiaId: this.familiaId, nombre: '', descripcion: '' });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  editar(categoria: ICategoria): void {
    this.editandoId = categoria.categoriaId;
    this.formulario.reset({
      familiaId: categoria.familiaId,
      nombre: categoria.nombre,
      descripcion: categoria.descripcion ?? ''
    });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  cancelar(): void { this.mostrarFormulario = false; this.editandoId = 0; this.formulario.reset(); }

  guardar(): void {
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.guardando) return;
    const valores = this.formulario.getRawValue();
    const actual = this.categorias.find(x => x.categoriaId === this.editandoId);
    const datos: ICategoria = {
      categoriaId: this.editandoId,
      familiaId: valores.familiaId,
      nombre: valores.nombre.trim(),
      descripcion: valores.descripcion.trim() || null,
      activo: actual?.activo ?? true
    };
    const solicitud = this.editandoId ? this.servicio.modificar(datos) : this.servicio.insertar(datos);
    this.guardando = true;
    this.limpiarMensajes();
    solicitud.pipe(finalize(() => { this.guardando = false; this.cdr.markForCheck(); })).subscribe({
      next: () => {
        this.mensaje = this.editandoId ? 'Categoría actualizada correctamente.' : 'Categoría creada correctamente.';
        this.cancelar();
        this.cargarCategorias();
      },
      error: err => this.error = this.mensajeError(err)
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
