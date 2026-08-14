import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  ICompraProveedorDetalle,
  ICompraProveedorResumen,
  IFiltroComprasProveedor
} from '../../model/ICompraProveedor';
import { IProveedor } from '../../model/IProveedor';
import { CompraProveedorService } from '../../services/compra-proveedor';
import { ProveedorService } from '../../services/proveedor';

@Component({
  selector: 'app-historial-compras-proveedor',
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './historial-compras-proveedor.html',
  styleUrls: [
    '../catalog-navigation-card/catalog-navigation-page.css',
    '../proveedores-inicio/proveedores.css'
  ]
})
export class HistorialComprasProveedor implements OnInit {
  private readonly servicio = inject(CompraProveedorService);
  private readonly proveedorServicio = inject(ProveedorService);
  private readonly cdr = inject(ChangeDetectorRef);

  proveedores: IProveedor[] = [];
  compras: ICompraProveedorResumen[] = [];
  detalle: ICompraProveedorDetalle | null = null;
  pagina = 1;
  tamanoPagina: 25 | 50 | 75 | 100 = 25;
  total = 0;
  proveedorId: number | undefined;
  estado = '';
  fechaDesde = '';
  fechaHasta = '';
  cargando = true;
  error = '';

  ngOnInit(): void {
    this.proveedorServicio.listar().subscribe({
      next: respuesta => {
        this.proveedores = respuesta.data ?? [];
        this.cdr.markForCheck();
      }
    });
    this.cargar();
  }

  cargar(pagina = this.pagina): void {
    this.pagina = pagina;
    this.cargando = true;
    this.error = '';
    this.detalle = null;
    const filtro: IFiltroComprasProveedor = {
      proveedorId: this.proveedorId || undefined,
      estado: this.estado || undefined,
      fechaDesde: this.fechaDesde || undefined,
      fechaHasta: this.fechaHasta || undefined,
      pagina: this.pagina,
      tamanoPagina: this.tamanoPagina
    };
    this.servicio.historial(filtro).subscribe({
      next: respuesta => {
        this.compras = respuesta.data?.items ?? [];
        this.total = respuesta.data?.total ?? 0;
        this.cargando = false;
        this.cdr.markForCheck();
      },
      error: err => {
        this.error = err?.error?.error || 'No fue posible cargar el historial.';
        this.cargando = false;
        this.cdr.markForCheck();
      }
    });
  }

  verDetalle(compra: ICompraProveedorResumen): void {
    this.error = '';
    this.servicio.detalle(compra.compraProveedorId).subscribe({
      next: respuesta => {
        this.detalle = respuesta.data;
        this.cdr.markForCheck();
      },
      error: err => {
        this.error = err?.error?.error || 'No fue posible cargar el detalle.';
        this.cdr.markForCheck();
      }
    });
  }

  descargarPdf(compra: ICompraProveedorResumen | ICompraProveedorDetalle): void {
    this.servicio.pdf(compra.compraProveedorId).subscribe({
      next: archivo => {
        const url = URL.createObjectURL(archivo);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = `Compra-${compra.numero}.pdf`;
        enlace.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.error = 'El PDF de esta compra no está disponible.';
        this.cdr.markForCheck();
      }
    });
  }

  limpiar(): void {
    this.proveedorId = undefined;
    this.estado = '';
    this.fechaDesde = '';
    this.fechaHasta = '';
    this.cargar(1);
  }

  paginas(): number {
    return Math.max(1, Math.ceil(this.total / this.tamanoPagina));
  }

  moneda(valor: number): string {
    return new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'CRC' }).format(valor);
  }
}
