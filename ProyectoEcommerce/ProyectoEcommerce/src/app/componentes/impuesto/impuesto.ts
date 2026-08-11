import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { IImpuesto } from '../../model/IImpuesto';
import { ImpuestoService } from '../../services/impuesto';

/** Pantalla administrativa para consultar y mantener la configuración de impuestos. */
@Component({
  selector: 'app-impuesto',
  imports: [ReactiveFormsModule],
  templateUrl: './impuesto.html',
  styleUrl: './impuesto.css'
})
export class Impuesto implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(ImpuestoService);
  private readonly cdr = inject(ChangeDetectorRef);
  impuestos: IImpuesto[] = [];
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
    nombre: ['', [Validators.required, Validators.maxLength(80)]],
    porcentaje: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    fechaInicio: [this.hoy(), Validators.required],
    fechaFin: ['']
  }, { validators: this.fechasValidas });

  get totalPaginas(): number { return Math.max(1, Math.ceil(this.impuestos.length / this.tamanoPagina)); }
  get impuestosPagina(): IImpuesto[] { const inicio = (this.pagina - 1) * this.tamanoPagina; return this.impuestos.slice(inicio, inicio + this.tamanoPagina); }

  ngOnInit(): void { this.cargar(); }
  cargar(): void {
    this.cargando = true; this.error = '';
    this.servicio.listar().pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); })).subscribe({
      next: respuesta => this.impuestos = (respuesta.data ?? []).sort((a, b) => a.nombre.localeCompare(b.nombre)),
      error: err => this.error = this.mensajeError(err)
    });
  }
  nuevo(): void { this.editandoId = 0; this.formulario.reset({ nombre: '', porcentaje: 0, fechaInicio: this.hoy(), fechaFin: '' }); this.mostrarFormulario = true; this.limpiarMensajes(); }
  editar(item: IImpuesto): void { this.editandoId = item.impuestoId; this.formulario.reset({ nombre: item.nombre, porcentaje: item.porcentaje, fechaInicio: item.fechaInicio, fechaFin: item.fechaFin ?? '' }); this.mostrarFormulario = true; this.limpiarMensajes(); }
  cancelar(): void { this.mostrarFormulario = false; this.editandoId = 0; this.formulario.reset(); }
  guardar(): void {
    this.formulario.markAllAsTouched(); if (this.formulario.invalid || this.guardando) return;
    const valor = this.formulario.getRawValue();
    const actual = this.impuestos.find(x => x.impuestoId === this.editandoId);
    const datos: IImpuesto = { impuestoId: this.editandoId, nombre: valor.nombre.trim(), porcentaje: Number(valor.porcentaje), fechaInicio: valor.fechaInicio, fechaFin: valor.fechaFin || null, activo: actual?.activo ?? true };
    const solicitud = this.editandoId ? this.servicio.modificar(datos) : this.servicio.insertar(datos);
    this.guardando = true; this.limpiarMensajes();
    solicitud.pipe(finalize(() => { this.guardando = false; this.cdr.markForCheck(); })).subscribe({
      next: () => { this.mensaje = this.editandoId ? 'Impuesto actualizado correctamente.' : 'Impuesto creado correctamente.'; this.cancelar(); this.cargar(); },
      error: err => this.error = this.mensajeError(err)
    });
  }
  cambiarEstado(item: IImpuesto): void {
    this.limpiarMensajes();
    const alCompletar = () => { this.mensaje = item.activo ? 'Impuesto desactivado.' : 'Impuesto activado.'; this.cargar(); };
    const alFallar = (err: any) => { this.error = this.mensajeError(err); this.cdr.markForCheck(); };
    if (item.activo) {
      this.servicio.eliminar(item.impuestoId).subscribe({ next: alCompletar, error: alFallar });
    } else {
      this.servicio.modificar({ ...item, activo: true }).subscribe({ next: alCompletar, error: alFallar });
    }
  }
  cambiarTamano(valor: string): void { this.tamanoPagina = Number(valor); this.pagina = 1; }
  irPagina(delta: number): void { this.pagina = Math.min(this.totalPaginas, Math.max(1, this.pagina + delta)); }
  private hoy(): string { return new Date().toISOString().slice(0, 10); }
  private fechasValidas(control: AbstractControl): ValidationErrors | null { const inicio = control.get('fechaInicio')?.value; const fin = control.get('fechaFin')?.value; return inicio && fin && fin < inicio ? { fechasInvalidas: true } : null; }
  private limpiarMensajes(): void { this.mensaje = ''; this.error = ''; }
  private mensajeError(error: any): string { return error?.error?.error || error?.error?.title || 'No fue posible completar la operación.'; }
}
