import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { IProveedor, IProveedorGuardar } from '../../model/IProveedor';
import { ProveedorService } from '../../services/proveedor';

@Component({
  selector: 'app-proveedor-gestion',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './proveedor-gestion.html',
  styleUrls: [
    '../catalog-navigation-card/catalog-navigation-page.css',
    '../proveedores-inicio/proveedores.css'
  ]
})
export class ProveedorGestion implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(ProveedorService);
  private readonly cdr = inject(ChangeDetectorRef);

  proveedores: IProveedor[] = [];
  editando: IProveedor | null = null;
  archivo: File | null = null;
  mostrandoFormulario = false;
  cargando = true;
  guardando = false;
  error = '';
  exito = '';

  readonly formulario = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    correo: ['', [Validators.email, Validators.maxLength(150)]],
    telefono: ['', Validators.maxLength(30)],
    direccion: ['', Validators.maxLength(250)]
  });

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this.error = '';
    this.servicio
      .listar()
      .pipe(
        finalize(() => {
          this.cargando = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: respuesta => {
          this.proveedores = respuesta.data ?? [];
        },
        error: err => {
          this.error = err?.error?.error || 'No fue posible cargar los proveedores.';
        }
      });
  }

  nuevo(): void {
    this.editando = null;
    this.archivo = null;
    this.formulario.reset();
    this.mostrandoFormulario = true;
    this.limpiarMensajes();
  }

  editar(proveedor: IProveedor): void {
    this.editando = proveedor;
    this.archivo = null;
    this.formulario.setValue({
      nombre: proveedor.nombre,
      correo: proveedor.correo ?? '',
      telefono: proveedor.telefono ?? '',
      direccion: proveedor.direccion ?? ''
    });
    this.mostrandoFormulario = true;
    this.limpiarMensajes();
  }

  cancelar(): void {
    this.mostrandoFormulario = false;
    this.editando = null;
    this.archivo = null;
    this.formulario.reset();
  }

  seleccionarArchivo(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.archivo = input.files?.[0] ?? null;
  }

  guardar(): void {
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.guardando) return;

    const valores = this.formulario.getRawValue();
    const datos: IProveedorGuardar = {
      proveedorId: this.editando?.proveedorId ?? 0,
      nombre: valores.nombre.trim(),
      correo: valores.correo.trim() || null,
      telefono: valores.telefono.trim() || null,
      direccion: valores.direccion.trim() || null,
      urlImagen: this.editando?.urlImagen ?? null,
      activo: this.editando?.activo ?? true
    };
    this.guardando = true;
    this.limpiarMensajes();
    const solicitud = this.editando
      ? this.servicio.modificar(datos)
      : this.servicio.insertar(datos);
    solicitud.subscribe({
      next: respuesta => {
        if (!respuesta.data) {
          this.finalizarError(respuesta.error || 'No fue posible guardar el proveedor.');
          return;
        }
        if (this.archivo) {
          this.servicio
            .subirImagen(respuesta.data.proveedorId, this.archivo)
            .subscribe({
              next: () => this.finalizarGuardado(),
              error: err => this.finalizarError(
                err?.error?.error ||
                err?.error ||
                'El proveedor se guardó, pero la imagen no pudo cargarse.'
              )
            });
          return;
        }
        this.finalizarGuardado();
      },
      error: err => this.finalizarError(err?.error?.error || 'No fue posible guardar el proveedor.')
    });
  }

  cambiarEstado(proveedor: IProveedor): void {
    this.limpiarMensajes();
    this.servicio.cambiarEstado(proveedor.proveedorId, !proveedor.activo).subscribe({
      next: respuesta => {
        if (!respuesta.data) {
          this.error = respuesta.error || 'No fue posible cambiar el estado.';
        } else {
          this.exito = respuesta.data.activo
            ? 'Proveedor activado correctamente.'
            : 'Proveedor desactivado. Sus productos e inventario se conservaron.';
          this.cargar();
        }
        this.cdr.markForCheck();
      },
      error: err => {
        this.error = err?.error?.error || 'No fue posible cambiar el estado.';
        this.cdr.markForCheck();
      }
    });
  }

  iniciales(nombre: string): string {
    return nombre.split(/\s+/).filter(Boolean).slice(0, 2).map(x => x[0]).join('').toUpperCase();
  }

  private finalizarGuardado(): void {
    this.guardando = false;
    this.exito = 'Proveedor guardado correctamente.';
    this.cancelar();
    this.cargar();
    this.cdr.markForCheck();
  }

  private finalizarError(mensaje: string): void {
    this.guardando = false;
    this.error = mensaje;
    this.cdr.markForCheck();
  }

  private limpiarMensajes(): void {
    this.error = '';
    this.exito = '';
  }
}
