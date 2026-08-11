import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { IProducto, IProductoCatalogo } from '../../model/IProducto';
import { IProductoImagen } from '../../model/IProductoImagen';
import { AutenticacionService } from '../../services/autenticacion';
import { ProductoService } from '../../services/producto';
import { ProductoImagenService } from '../../services/producto-imagen';
import { ProductoCarrusel } from '../producto-carrusel/producto-carrusel';
import { CarritoService } from '../../services/carrito';

type ProductoDetalleVista = IProductoCatalogo & Partial<IProducto>;

/** Muestra un producto y permite al Cliente agregarlo al carrito cuando existe stock. */
@Component({
  selector: 'app-producto-detalle',
  imports: [RouterLink, CurrencyPipe, DatePipe, ProductoCarrusel],
  templateUrl: './producto-detalle.html',
  styleUrl: './producto-detalle.css'
})
export class ProductoDetalle implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly servicio = inject(ProductoService);
  private readonly imagenesServicio = inject(ProductoImagenService);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly carrito = inject(CarritoService);

  readonly esAdmin = this.autenticacion.esAdministrador();
  readonly esCliente = this.autenticacion.esCliente();
  producto: ProductoDetalleVista | null = null;
  imagenes: IProductoImagen[] = [];
  cargando = true;
  error = '';
  mensajeCarrito = '';
  errorCarrito = '';
  procesandoCarrito = false;

  get rutaRegreso(): any[] {
    return this.esCliente && this.producto
      ? ['/productos/familia', this.producto.familiaId, 'categoria', this.producto.categoriaId]
      : ['/productos'];
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(parametros => {
      const productoId = Number(parametros.get('productoId'));
      queueMicrotask(() => {
        if (!Number.isInteger(productoId) || productoId <= 0) {
          this.cargando = false;
          this.error = 'El producto solicitado no es válido.';
          this.cdr.markForCheck();
          return;
        }
        this.cargar(productoId);
      });
    });
  }

  private cargar(productoId: number): void {
    this.cargando = true;
    this.error = '';
    this.producto = null;
    const productoSolicitud = this.esAdmin
      ? this.servicio.detalleAdministracion(productoId)
      : this.servicio.detalle(productoId);
    forkJoin({
      producto: productoSolicitud,
      imagenes: this.imagenesServicio.listarPorProducto(productoId).pipe(
        catchError(() => of({ success: false, data: [] as IProductoImagen[], error: '' }))
      )
    }).pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); })).subscribe({
      next: resultado => {
        this.producto = resultado.producto.data;
        this.imagenes = resultado.imagenes.data ?? [];
        if (!this.producto) this.error = 'No se encontró el producto solicitado.';
      },
      error: err => this.error = err?.status === 404
        ? 'No se encontró el producto solicitado.'
        : (err?.error?.error || 'No fue posible cargar el detalle del producto.')
    });
  }

  agregarAlCarrito(): void {
    if (!this.esCliente || !this.producto?.disponible || this.procesandoCarrito) return;
    this.mensajeCarrito = '';
    this.errorCarrito = '';
    this.procesandoCarrito = true;
    this.carrito.agregar(this.producto.productoId, 1)
      .pipe(finalize(() => { this.procesandoCarrito = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: () => this.mensajeCarrito = 'El producto fue agregado al carrito.',
        error: err => this.errorCarrito = err?.error?.error || 'No fue posible agregar el producto al carrito.'
      });
  }
}
