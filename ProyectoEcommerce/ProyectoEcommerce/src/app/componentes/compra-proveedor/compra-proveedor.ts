import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ISolicitudCompraProveedor } from '../../model/ICompraProveedor';
import { ICategoriaOfertaProveedor, IProductoOfertaProveedor, IProveedor } from '../../model/IProveedor';
import { CompraProveedorService } from '../../services/compra-proveedor';
import { ProveedorService } from '../../services/proveedor';

interface ILineaCarritoProveedor {
  producto: IProductoOfertaProveedor;
  cantidad: number;
}

@Component({
  selector: 'app-compra-proveedor',
  imports: [FormsModule, RouterLink],
  templateUrl: './compra-proveedor.html',
  styleUrls: [
    '../catalog-navigation-card/catalog-navigation-page.css',
    '../proveedores-inicio/proveedores.css'
  ]
})
export class CompraProveedor implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly proveedorServicio = inject(ProveedorService);
  private readonly compraServicio = inject(CompraProveedorService);
  private readonly cdr = inject(ChangeDetectorRef);

  proveedores: IProveedor[] = [];
  proveedor: IProveedor | null = null;
  categorias: ICategoriaOfertaProveedor[] = [];
  productos: IProductoOfertaProveedor[] = [];
  carrito: ILineaCarritoProveedor[] = [];
  cantidades: Record<number, number> = {};
  proveedorId = 0;
  categoriaId = 0;
  claveConfirmacion = crypto.randomUUID();
  cargando = true;
  procesando = false;
  error = '';
  exito = '';

  ngOnInit(): void {
    this.proveedorId = Number(this.route.snapshot.paramMap.get('proveedorId') ?? 0);
    this.cargarProveedores();
  }

  seleccionarCategoria(categoriaId: number): void {
    this.categoriaId = Number(categoriaId);
    this.cargarProductos();
  }

  cambiarCantidad(ofertaId: number, valor: number): void {
    this.cantidades[ofertaId] = Number(valor);
  }

  agregar(producto: IProductoOfertaProveedor): void {
    this.limpiarMensajes();
    if (!producto.incorporado) {
      this.error = 'Primero debes incorporar este producto al catálogo de LessPrice.';
      return;
    }
    const cantidad = Number(this.cantidades[producto.productoProveedorCatalogoId] ?? 1);
    if (!Number.isInteger(cantidad) || cantidad <= 0) {
      this.error = 'La cantidad debe ser un número entero mayor que cero.';
      return;
    }
    const existente = this.carrito.find(
      x => x.producto.productoProveedorCatalogoId === producto.productoProveedorCatalogoId
    );
    if (existente) existente.cantidad += cantidad;
    else this.carrito.push({ producto, cantidad });
    this.exito = 'Producto agregado a la compra. El inventario todavía no cambió.';
  }

  quitar(ofertaId: number): void {
    this.carrito = this.carrito.filter(
      x => x.producto.productoProveedorCatalogoId !== ofertaId
    );
  }

  generarProforma(): void {
    const solicitud = this.solicitud();
    if (!solicitud || this.procesando) return;
    this.procesando = true;
    this.limpiarMensajes();
    this.compraServicio.proforma(solicitud).subscribe({
      next: archivo => {
        this.procesando = false;
        this.descargar(archivo, `Proforma-${this.proveedor?.nombre ?? 'proveedor'}.pdf`);
        this.exito = 'Proforma generada. El stock no fue modificado.';
        this.cdr.markForCheck();
      },
      error: () => this.finalizarError('No fue posible generar la proforma.')
    });
  }

  enviarProforma(): void {
    const solicitud = this.solicitud();
    if (!solicitud || this.procesando) return;
    this.procesando = true;
    this.limpiarMensajes();
    this.compraServicio.enviarProforma(solicitud).subscribe({
      next: respuesta => {
        this.procesando = false;
        if (!respuesta.data) this.error = respuesta.error || 'No fue posible enviar la proforma.';
        else this.exito = `Proforma enviada a ${this.proveedor?.correo}. El stock no fue modificado.`;
        this.cdr.markForCheck();
      },
      error: err => this.finalizarError(err?.error?.error || 'No fue posible enviar la proforma.')
    });
  }

  confirmar(): void {
    const solicitud = this.solicitud();
    if (!solicitud || this.procesando) return;
    this.procesando = true;
    this.limpiarMensajes();
    this.compraServicio.confirmar(solicitud).subscribe({
      next: respuesta => {
        this.procesando = false;
        if (!respuesta.data) {
          this.error = respuesta.error || 'No fue posible confirmar la compra.';
        } else {
          this.exito = `${respuesta.data.numero}: ${respuesta.data.mensaje}`;
          this.carrito = [];
          this.claveConfirmacion = crypto.randomUUID();
          this.cargarProductos();
        }
        this.cdr.markForCheck();
      },
      error: err => this.finalizarError(err?.error?.error || 'No fue posible confirmar la compra.')
    });
  }

  total(): number {
    return this.carrito.reduce(
      (suma, linea) => suma + linea.producto.precioCompra * linea.cantidad,
      0
    );
  }

  moneda(valor: number): string {
    return new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'CRC' }).format(valor);
  }

  iniciales(nombre: string): string {
    return nombre.split(/\s+/).filter(Boolean).slice(0, 2).map(x => x[0]).join('').toUpperCase();
  }

  private cargarProveedores(): void {
    this.proveedorServicio.listar(true).subscribe({
      next: respuesta => {
        this.proveedores = respuesta.data ?? [];
        if (this.proveedorId) {
          this.proveedor = this.proveedores.find(x => x.proveedorId === this.proveedorId) ?? null;
          if (!this.proveedor) {
            this.finalizarError('El proveedor está inactivo o no existe.');
            return;
          }
          this.cargarCategorias();
        } else {
          this.cargando = false;
        }
        this.cdr.markForCheck();
      },
      error: err => this.finalizarError(err?.error?.error || 'No fue posible cargar los proveedores.')
    });
  }

  private cargarCategorias(): void {
    this.proveedorServicio.categorias(this.proveedorId).subscribe({
      next: respuesta => {
        this.categorias = (respuesta.data ?? []).filter(x => x.incorporada);
        this.categoriaId = this.categorias[0]?.categoriaId ?? 0;
        if (this.categoriaId) this.cargarProductos();
        else this.cargando = false;
        this.cdr.markForCheck();
      },
      error: err => this.finalizarError(err?.error?.error || 'No fue posible cargar las categorías.')
    });
  }

  private cargarProductos(): void {
    if (!this.categoriaId) return;
    this.cargando = true;
    this.proveedorServicio.productos(this.proveedorId, this.categoriaId).subscribe({
      next: respuesta => {
        this.productos = (respuesta.data ?? []).filter(producto => producto.activo);
        for (const producto of this.productos) {
          if (!this.cantidades[producto.productoProveedorCatalogoId]) {
            this.cantidades[producto.productoProveedorCatalogoId] = 1;
          }
        }
        this.cargando = false;
        this.cdr.markForCheck();
      },
      error: err => this.finalizarError(err?.error?.error || 'No fue posible cargar los productos.')
    });
  }

  private solicitud(): ISolicitudCompraProveedor | null {
    this.limpiarMensajes();
    if (!this.proveedor || !this.carrito.length) {
      this.error = 'Agrega al menos un producto a la compra.';
      return null;
    }
    return {
      proveedorId: this.proveedor.proveedorId,
      claveConfirmacion: this.claveConfirmacion,
      productos: this.carrito.map(x => ({
        productoProveedorCatalogoId: x.producto.productoProveedorCatalogoId,
        cantidad: x.cantidad
      }))
    };
  }

  private descargar(archivo: Blob, nombre: string): void {
    const url = URL.createObjectURL(archivo);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    enlace.click();
    URL.revokeObjectURL(url);
  }

  private finalizarError(mensaje: string): void {
    this.procesando = false;
    this.cargando = false;
    this.error = mensaje;
    this.cdr.markForCheck();
  }

  private limpiarMensajes(): void {
    this.error = '';
    this.exito = '';
  }
}
