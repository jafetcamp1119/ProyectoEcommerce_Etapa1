import { CurrencyPipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ICheckoutPreparacion, ICompraCompletada, IConfirmarCompraSolicitud } from '../../model/ICheckout';
import { CarritoService } from '../../services/carrito';
import { OrdenService } from '../../services/orden';

/** Recoge entrega y método de pago y solicita a la API la confirmación definitiva de la compra. */
@Component({
  selector: 'app-checkout',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css'
})
export class Checkout implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ordenes = inject(OrdenService);
  private readonly carritoServicio = inject(CarritoService);
  private readonly cdr = inject(ChangeDetectorRef);

  preparacion: ICheckoutPreparacion | null = null;
  completada: ICompraCompletada | null = null;
  paso: 1 | 2 | 3 = 1;
  cargando = true;
  procesando = false;
  error = '';

  readonly entrega = this.fb.nonNullable.group({
    nombreCompleto: [''],
    correo: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    telefono: [''],
    direccion: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]],
    confirmarCorreo: [false, Validators.requiredTrue]
  });

  readonly pago = this.fb.nonNullable.group({
    metodoPago: ['', Validators.required]
  });

  ngOnInit(): void {
    this.ordenes.prepararCheckout()
      .pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => {
          this.preparacion = respuesta.data;
          const cliente = respuesta.data?.cliente;
          if (cliente) {
            this.entrega.reset({
              nombreCompleto: cliente.nombreCompleto,
              correo: cliente.correo,
              telefono: cliente.telefono,
              direccion: cliente.direccion ?? '',
              confirmarCorreo: false
            });
          }
        },
        error: err => this.error = err?.error?.error || 'No fue posible preparar la compra.'
      });
  }

  continuarEntrega(): void {
    this.entrega.markAllAsTouched();
    if (this.entrega.invalid) return;
    this.entrega.controls.direccion.setValue(this.normalizarEspacios(this.entrega.controls.direccion.value));
    if (this.entrega.controls.direccion.invalid) return;
    this.paso = 2;
  }

  seleccionarPago(metodo: 'TARJETA' | 'EFECTIVO'): void {
    this.pago.controls.metodoPago.setValue(metodo);
  }

  continuarPago(): void {
    this.pago.markAllAsTouched();
    if (this.pago.invalid) return;
    this.paso = 3;
  }

  volver(): void {
    this.paso = this.paso === 3 ? 2 : 1;
    this.error = '';
  }

  confirmarCompra(): void {
    if (this.procesando || this.entrega.invalid || this.pago.invalid) return;
    const entrega = this.entrega.getRawValue();
    const metodo = this.pago.controls.metodoPago.value as 'TARJETA' | 'EFECTIVO';
    const solicitud: IConfirmarCompraSolicitud = {
      correoDestino: entrega.correo.trim().toLowerCase(),
      direccionEnvio: this.normalizarEspacios(entrega.direccion),
      metodoPago: metodo,
      correoConfirmado: entrega.confirmarCorreo
    };

    this.error = '';
    this.procesando = true;
    this.ordenes.confirmarCompra(solicitud)
      .pipe(finalize(() => { this.procesando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => {
          this.completada = respuesta.data;
          this.carritoServicio.reiniciarIndicador();
        },
        error: err => this.error = err?.error?.error || 'No fue posible confirmar la compra.'
      });
  }

  etiquetaMetodo(): string {
    return this.pago.controls.metodoPago.value === 'TARJETA' ? 'Tarjeta' : 'Efectivo';
  }

  private normalizarEspacios(valor: string): string {
    return valor.trim().replace(/\s+/g, ' ');
  }
}
