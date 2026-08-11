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
    this.formulario.reset({ nombre: '', descripcion: '' });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  editar(familia: IFamiliaProducto): void {
    this.editandoId = familia.familiaId;
    this.formulario.reset({ nombre: familia.nombre, descripcion: familia.descripcion ?? '' });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  cancelar(): void {
    this.mostrarFormulario = false;
    this.editandoId = 0;
    this.formulario.reset();
  }

  guardar(): void {
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.guardando) return;

    const valores = this.formulario.getRawValue();
    const actual = this.familias.find(x => x.familiaId === this.editandoId);
    const datos: IFamiliaProducto = {
      familiaId: this.editandoId,
      nombre: valores.nombre.trim(),
      descripcion: valores.descripcion.trim() || null,
      activo: actual?.activo ?? true
    };
    const solicitud = this.editandoId ? this.servicio.modificar(datos) : this.servicio.insertar(datos);
    this.guardando = true;
    this.limpiarMensajes();
    solicitud.pipe(finalize(() => { this.guardando = false; this.cdr.markForCheck(); })).subscribe({
      next: () => {
        this.mensaje = this.editandoId ? 'Familia actualizada correctamente.' : 'Familia creada correctamente.';
        this.cancelar();
        this.cargar();
      },
      error: err => this.error = this.mensajeError(err)
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
