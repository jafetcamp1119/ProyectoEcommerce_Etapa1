import { CurrencyPipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ICarritoActual, ICarritoItem } from '../../model/ICarrito';
import { CarritoService } from '../../services/carrito';

/** Muestra el carrito, permite cambiar cantidades y controla el acceso al checkout. */
@Component({
  selector: 'app-carrito',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './carrito.html',
  styleUrl: './carrito.css'
})
export class Carrito implements OnInit {
  private readonly servicio = inject(CarritoService);
  private readonly cdr = inject(ChangeDetectorRef);
  carrito: ICarritoActual | null = null;
  cargando = true;
  error = '';
  mensaje = '';
  procesando = new Set<number>();

  get puedeContinuar(): boolean {
    return !!this.carrito?.items.length &&
      this.carrito.items.every(x => x.productoActivo && x.stockSuficiente && x.cantidad >= 1);
  }

  ngOnInit(): void {
    this.servicio.obtenerActual()
      .pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => this.carrito = respuesta.data,
        error: err => this.error = err?.error?.error || 'No fue posible cargar el carrito.'
      });
  }

  cambiarCantidad(item: ICarritoItem, cantidad: number): void {
    if (!Number.isInteger(cantidad) || cantidad < 1 || cantidad > item.stockDisponible) {
      this.error = cantidad > item.stockDisponible
        ? 'La cantidad no puede superar el stock disponible.'
        : 'La cantidad debe ser un número entero mayor o igual a 1.';
      return;
    }
    if (cantidad === item.cantidad || this.procesando.has(item.carritoDetalleId)) return;

    this.limpiarMensajes();
    this.procesando.add(item.carritoDetalleId);
    this.servicio.actualizarCantidad(item.carritoDetalleId, cantidad)
      .pipe(finalize(() => { this.procesando.delete(item.carritoDetalleId); this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => {
          this.carrito = respuesta.data;
          this.mensaje = 'Cantidad actualizada.';
        },
        error: err => this.error = err?.error?.error || 'No fue posible actualizar la cantidad.'
      });
  }

  escribirCantidad(item: ICarritoItem, input: HTMLInputElement): void {
    const texto = input.value.trim();
    if (!/^\d+$/.test(texto)) {
      input.value = String(item.cantidad);
      this.error = 'Ingresa una cantidad entera válida.';
      return;
    }
    const cantidad = Number(texto);
    if (cantidad < 1 || cantidad > item.stockDisponible) input.value = String(item.cantidad);
    this.cambiarCantidad(item, cantidad);
  }

  eliminar(item: ICarritoItem): void {
    if (this.procesando.has(item.carritoDetalleId)) return;
    this.limpiarMensajes();
    this.procesando.add(item.carritoDetalleId);
    this.servicio.eliminar(item.carritoDetalleId)
      .pipe(finalize(() => { this.procesando.delete(item.carritoDetalleId); this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => {
          this.carrito = respuesta.data;
          this.mensaje = 'Producto eliminado del carrito.';
        },
        error: err => this.error = err?.error?.error || 'No fue posible eliminar el producto.'
      });
  }

  iniciales(nombre: string): string {
    const palabras = nombre.trim().split(/\s+/).filter(Boolean);
    if (!palabras.length) return 'PR';
    if (palabras.length === 1) return palabras[0].slice(0, 2).toUpperCase();
    return `${palabras[0][0]}${palabras[1][0]}`.toUpperCase();
  }

  private limpiarMensajes(): void { this.error = ''; this.mensaje = ''; }
}
