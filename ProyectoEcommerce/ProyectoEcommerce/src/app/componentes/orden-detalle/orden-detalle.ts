import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { IOrdenDetalleConsulta } from '../../model/IOrden';
import { OrdenService } from '../../services/orden';

/** Muestra productos, importes, estado y descarga autorizada de la factura de una orden. */
@Component({
  selector: 'app-orden-detalle',
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './orden-detalle.html',
  styleUrl: './orden-detalle.css'
})
export class OrdenDetalle implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly servicio = inject(OrdenService);
  private readonly cdr = inject(ChangeDetectorRef);

  orden: IOrdenDetalleConsulta | null = null;
  cargando = true;
  descargando = false;
  error = '';

  ngOnInit(): void {
    const ordenId = Number(this.route.snapshot.paramMap.get('ordenId'));
    if (!Number.isInteger(ordenId) || ordenId <= 0) {
      this.cargando = false;
      this.error = 'La orden solicitada no es válida.';
      return;
    }
    this.servicio.detalle(ordenId)
      .pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => this.orden = respuesta.data,
        error: err => this.error = err?.status === 404 ? 'No se encontró la orden o no tienes permiso para consultarla.' : 'No fue posible cargar la orden.'
      });
  }

  descargarFactura(): void {
    if (!this.orden?.facturaDisponible || this.descargando) return;
    this.error = '';
    this.descargando = true;
    this.servicio.descargarFactura(this.orden.ordenId)
      .pipe(finalize(() => { this.descargando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: archivo => {
          const url = URL.createObjectURL(archivo);
          const enlace = document.createElement('a');
          enlace.href = url;
          enlace.download = `Factura-${this.orden!.numeroOrden}.pdf`;
          enlace.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.error = 'No fue posible descargar la factura.'
      });
  }

  etiquetaMetodo(metodo: string | null): string {
    return metodo === 'TARJETA' ? 'Tarjeta' : metodo === 'EFECTIVO' ? 'Efectivo' : metodo ?? 'No registrado';
  }
}
