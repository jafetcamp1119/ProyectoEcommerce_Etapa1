import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { IFiltroOrdenes, IOrdenResumen } from '../../model/IOrden';
import { AutenticacionService } from '../../services/autenticacion';
import { OrdenService } from '../../services/orden';

/**
 * Lista las órdenes del Cliente o la consulta administrativa según el rol de la sesión.
 */
@Component({
  selector: 'app-orden',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './orden.html',
  styleUrl: './orden.css'
})
export class Orden implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(OrdenService);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly esAdmin = this.autenticacion.esAdministrador();
  readonly tamanos = [25, 50, 75, 100] as const;
  readonly estados = ['PROFORMA', 'PENDIENTE', 'CONFIRMADA', 'FACTURADA', 'CANCELADA'];
  ordenes: IOrdenResumen[] = [];
  cargando = true;
  pagina = 1;
  tamanoPagina: 25 | 50 | 75 | 100 = 25;
  total = 0;
  error = '';
  mensaje = '';
  procesando = new Set<number>();

  readonly filtros = this.fb.nonNullable.group({
    numero: [''],
    cliente: [''],
    estado: [''],
    fechaDesde: [''],
    fechaHasta: ['']
  });

  get totalPaginas(): number { return Math.max(1, Math.ceil(this.total / this.tamanoPagina)); }

  ngOnInit(): void { this.cargar(); }
  buscar(): void { this.pagina = 1; this.cargar(); }

  limpiar(): void {
    this.filtros.reset({ numero: '', cliente: '', estado: '', fechaDesde: '', fechaHasta: '' });
    this.pagina = 1;
    this.cargar();
  }

  cambiarTamano(valor: string): void {
    const tamano = Number(valor) as 25 | 50 | 75 | 100;
    if (!this.tamanos.includes(tamano)) return;
    this.tamanoPagina = tamano;
    this.pagina = 1;
    this.cargar();
  }

  irPagina(delta: number): void {
    const destino = Math.min(this.totalPaginas, Math.max(1, this.pagina + delta));
    if (destino === this.pagina) return;
    this.pagina = destino;
    this.cargar();
  }

  cancelar(orden: IOrdenResumen): void {
    if (orden.estado !== 'PENDIENTE' || this.esAdmin || this.procesando.has(orden.ordenId)) return;
    if (!confirm(`¿Deseas cancelar la orden #${orden.numeroOrden}?`)) return;
    this.limpiarMensajes();
    this.procesando.add(orden.ordenId);
    this.servicio.cancelar(orden.ordenId)
      .pipe(finalize(() => { this.procesando.delete(orden.ordenId); this.cdr.markForCheck(); }))
      .subscribe({
        next: () => { this.mensaje = 'Orden cancelada correctamente.'; this.cargar(false); },
        error: err => this.error = err?.error?.error || 'No fue posible cancelar la orden.'
      });
  }

  descargarFactura(orden: IOrdenResumen): void {
    if (!orden.facturaDisponible || this.procesando.has(orden.ordenId)) return;
    this.limpiarMensajes();
    this.procesando.add(orden.ordenId);
    this.servicio.descargarFactura(orden.ordenId)
      .pipe(finalize(() => { this.procesando.delete(orden.ordenId); this.cdr.markForCheck(); }))
      .subscribe({
        next: archivo => this.guardarArchivo(archivo, `Factura-${orden.numeroOrden}.pdf`),
        error: err => this.error = err?.status === 404 ? 'La factura no está disponible.' : 'No fue posible descargar la factura.'
      });
  }

  etiquetaMetodo(metodo: string | null): string {
    if (!metodo) return 'No registrado';
    return metodo === 'TARJETA' ? 'Tarjeta' : metodo === 'EFECTIVO' ? 'Efectivo' : metodo;
  }

  private cargar(limpiarMensajes = true): void {
    if (limpiarMensajes) this.limpiarMensajes();
    this.cargando = true;
    const valores = this.filtros.getRawValue();
    const filtro: IFiltroOrdenes = {
      numero: valores.numero.trim() || undefined,
      cliente: this.esAdmin ? valores.cliente.trim() || undefined : undefined,
      estado: valores.estado || undefined,
      fechaDesde: valores.fechaDesde || undefined,
      fechaHasta: valores.fechaHasta || undefined,
      pagina: this.pagina,
      tamanoPagina: this.tamanoPagina
    };
    const solicitud = this.esAdmin ? this.servicio.administracion(filtro) : this.servicio.misOrdenes(filtro);
    solicitud.pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); })).subscribe({
      next: respuesta => {
        this.ordenes = respuesta.data?.items ?? [];
        this.total = respuesta.data?.total ?? 0;
        this.pagina = respuesta.data?.pagina ?? this.pagina;
      },
      error: err => { this.ordenes = []; this.total = 0; this.error = err?.error?.error || 'No fue posible cargar las órdenes.'; }
    });
  }

  private guardarArchivo(archivo: Blob, nombre: string): void {
    const url = URL.createObjectURL(archivo);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    enlace.click();
    URL.revokeObjectURL(url);
  }

  private limpiarMensajes(): void { this.error = ''; this.mensaje = ''; }
}
