import { ChangeDetectorRef, Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { IImpuesto } from '../../model/IImpuesto';
import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import {
  ICategoriaOfertaProveedor,
  IProductoExistenteProveedor,
  IProductoOfertaProveedor,
  IProveedor
} from '../../model/IProveedor';
import { CategoriaService } from '../../services/categoria';
import { ProductoImagenService } from '../../services/producto-imagen';
import { ProductoService } from '../../services/producto';
import { ProveedorService } from '../../services/proveedor';
import { CatalogNavigationCard } from '../catalog-navigation-card/catalog-navigation-card';

type ModoCategoria = 'existente' | 'nueva';
type ModoProducto = 'existente' | 'nuevo';

@Component({
  selector: 'app-catalogo-proveedor',
  imports: [ReactiveFormsModule, RouterLink, CatalogNavigationCard],
  templateUrl: './catalogo-proveedor.html',
  styleUrls: [
    '../catalog-navigation-card/catalog-navigation-page.css',
    '../proveedores-inicio/proveedores.css'
  ]
})
export class CatalogoProveedor implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly servicio = inject(ProveedorService);
  private readonly categoriaServicio = inject(CategoriaService);
  private readonly productoServicio = inject(ProductoService);
  private readonly imagenServicio = inject(ProductoImagenService);
  private readonly cdr = inject(ChangeDetectorRef);
  private suscripcion?: Subscription;

  proveedores: IProveedor[] = [];
  proveedor: IProveedor | null = null;
  categorias: ICategoriaOfertaProveedor[] = [];
  categoriasDisponibles: ICategoriaOfertaProveedor[] = [];
  productos: IProductoOfertaProveedor[] = [];
  productosExistentes: IProductoExistenteProveedor[] = [];
  familias: IFamiliaProducto[] = [];
  impuestos: IImpuesto[] = [];
  seleccionada: IProductoOfertaProveedor | null = null;
  editandoOferta: IProductoOfertaProveedor | null = null;
  modoCategoria: ModoCategoria = 'existente';
  modoProducto: ModoProducto = 'existente';
  mostrarCategoria = false;
  mostrarProducto = false;
  proveedorId = 0;
  categoriaId = 0;
  archivoCategoria: File | null = null;
  imagenesProducto: File[] = [];
  cargando = true;
  guardando = false;
  error = '';
  exito = '';

  readonly categoriaExistenteForm = this.fb.nonNullable.group({
    categoriaId: [0, [Validators.required, Validators.min(1)]]
  });

  readonly categoriaNuevaForm = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(80)]],
    familiaId: [0, [Validators.required, Validators.min(1)]],
    descripcion: ['', Validators.maxLength(250)]
  });

  readonly productoExistenteForm = this.fb.nonNullable.group({
    productoId: [0, [Validators.required, Validators.min(1)]],
    precioCompra: [0, [Validators.required, Validators.min(0.01)]]
  });

  readonly productoNuevoForm = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(120)]],
    precioCompra: [0, [Validators.required, Validators.min(0.01)]],
    impuestoId: [0, [Validators.required, Validators.min(1)]],
    activo: [true]
  });

  readonly incorporacionForm = this.fb.nonNullable.group({
    descripcion: ['', Validators.maxLength(500)],
    stockMinimo: [5, [Validators.required, Validators.min(0)]]
  });

  readonly ofertaForm = this.fb.nonNullable.group({
    precioCompra: [0, [Validators.required, Validators.min(0.01)]],
    impuestoId: [0, [Validators.required, Validators.min(1)]],
    activo: [true]
  });

  ngOnInit(): void {
    this.cargarCatalogos();
    this.suscripcion = this.route.paramMap.subscribe(parametros => {
      this.proveedorId = Number(parametros.get('proveedorId') ?? 0);
      this.categoriaId = Number(parametros.get('categoriaId') ?? 0);
      this.cargarRuta();
    });
  }

  ngOnDestroy(): void {
    this.suscripcion?.unsubscribe();
  }

  abrirCategoria(modo: ModoCategoria): void {
    this.modoCategoria = modo;
    this.mostrarCategoria = true;
    this.archivoCategoria = null;
    this.categoriaExistenteForm.reset({ categoriaId: 0 });
    this.categoriaNuevaForm.reset({
      nombre: '',
      familiaId: this.familias[0]?.familiaId ?? 0,
      descripcion: ''
    });
    this.limpiarMensajes();
  }

  cancelarCategoria(): void {
    this.mostrarCategoria = false;
    this.archivoCategoria = null;
  }

  guardarCategoriaExistente(): void {
    this.categoriaExistenteForm.markAllAsTouched();
    if (this.categoriaExistenteForm.invalid || this.guardando) return;

    this.guardando = true;
    this.limpiarMensajes();
    const categoriaId = this.categoriaExistenteForm.controls.categoriaId.value;
    this.servicio
      .asociarCategoriaExistente(this.proveedorId, categoriaId)
      .subscribe({
        next: respuesta => {
          this.guardando = false;
          if (!respuesta.data) {
            this.error = respuesta.error || 'No fue posible asociar la categoría.';
            this.cdr.markForCheck();
            return;
          }

          this.exito = `${respuesta.data.nombre} quedó relacionada con el proveedor.`;
          this.cancelarCategoria();
          this.cargarCategorias();
        },
        error: err => this.finalizarError(
          err?.error?.error || 'No fue posible asociar la categoría.'
        )
      });
  }

  guardarCategoriaNueva(): void {
    this.categoriaNuevaForm.markAllAsTouched();
    if (this.categoriaNuevaForm.invalid || this.guardando) return;

    this.guardando = true;
    this.limpiarMensajes();
    const valores = this.categoriaNuevaForm.getRawValue();
    this.servicio
      .crearCategoria(this.proveedorId, {
        familiaId: valores.familiaId,
        nombre: valores.nombre.trim(),
        descripcion: valores.descripcion.trim() || null
      })
      .subscribe({
        next: respuesta => {
          if (!respuesta.data) {
            this.finalizarError(
              respuesta.error || 'No fue posible crear la categoría.'
            );
            return;
          }

          if (respuesta.data.yaExistia) {
            this.guardando = false;
            this.exito = `${respuesta.data.nombre} ya existía y fue relacionada sin duplicarla.`;
            this.cancelarCategoria();
            this.cargarCategorias();
            return;
          }

          if (this.archivoCategoria) {
            this.subirImagenCategoria(
              respuesta.data.categoriaId,
              respuesta.data.nombre,
              this.archivoCategoria
            );
            return;
          }

          this.finalizarCategoriaNueva(respuesta.data.nombre);
        },
        error: err => this.finalizarError(
          err?.error?.error || 'No fue posible crear la categoría.'
        )
      });
  }

  seleccionarArchivoCategoria(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.archivoCategoria = input.files?.[0] ?? null;
  }

  abrirProducto(modo: ModoProducto): void {
    this.modoProducto = modo;
    this.mostrarProducto = true;
    this.productoExistenteForm.reset({ productoId: 0, precioCompra: 0 });
    this.productoNuevoForm.reset({
      nombre: '',
      precioCompra: 0,
      impuestoId: this.impuestos[0]?.impuestoId ?? 0,
      activo: true
    });
    this.limpiarMensajes();
  }

  cancelarProducto(): void {
    this.mostrarProducto = false;
  }

  guardarProductoExistente(): void {
    this.productoExistenteForm.markAllAsTouched();
    if (this.productoExistenteForm.invalid || this.guardando) return;

    this.guardando = true;
    this.limpiarMensajes();
    const valores = this.productoExistenteForm.getRawValue();
    this.servicio
      .agregarProductoExistente(this.proveedorId, this.categoriaId, valores)
      .subscribe({
        next: respuesta => {
          this.guardando = false;
          if (!respuesta.data) {
            this.error = respuesta.error || 'No fue posible relacionar el producto.';
            this.cdr.markForCheck();
            return;
          }

          this.exito = `${respuesta.data.nombre} quedó relacionado sin crear otro producto.`;
          this.cancelarProducto();
          this.cargarProductos();
        },
        error: err => this.finalizarError(
          err?.error?.error || 'No fue posible relacionar el producto.'
        )
      });
  }

  guardarProductoNuevo(): void {
    this.productoNuevoForm.markAllAsTouched();
    if (this.productoNuevoForm.invalid || this.guardando) return;

    this.guardando = true;
    this.limpiarMensajes();
    const valores = this.productoNuevoForm.getRawValue();
    this.servicio
      .crearProducto(this.proveedorId, this.categoriaId, {
        nombre: valores.nombre.trim(),
        precioCompra: valores.precioCompra,
        impuestoId: valores.impuestoId,
        activo: valores.activo
      })
      .subscribe({
        next: respuesta => {
          this.guardando = false;
          if (!respuesta.data) {
            this.error = respuesta.error || 'No fue posible crear el producto.';
            this.cdr.markForCheck();
            return;
          }

          this.exito = `${respuesta.data.nombre} se agregó al catálogo del proveedor; todavía no se vende en LessPrice.`;
          this.cancelarProducto();
          this.cargarProductos();
        },
        error: err => this.finalizarError(
          err?.error?.error || 'No fue posible crear el producto.'
        )
      });
  }

  editarOferta(producto: IProductoOfertaProveedor): void {
    this.editandoOferta = producto;
    this.ofertaForm.setValue({
      precioCompra: producto.precioCompra,
      impuestoId: producto.impuestoId,
      activo: producto.activo
    });
    this.limpiarMensajes();
  }

  cancelarEdicionOferta(): void {
    this.editandoOferta = null;
  }

  guardarOferta(): void {
    this.ofertaForm.markAllAsTouched();
    if (!this.editandoOferta || this.ofertaForm.invalid || this.guardando) return;

    this.guardando = true;
    this.limpiarMensajes();
    this.servicio
      .modificarProducto(
        this.editandoOferta.productoProveedorCatalogoId,
        this.ofertaForm.getRawValue()
      )
      .subscribe({
        next: respuesta => {
          this.guardando = false;
          if (!respuesta.data) {
            this.error = respuesta.error || 'No fue posible modificar la oferta.';
            this.cdr.markForCheck();
            return;
          }

          this.exito = 'Oferta del proveedor actualizada correctamente.';
          this.editandoOferta = null;
          this.cargarProductos();
        },
        error: err => this.finalizarError(
          err?.error?.error || 'No fue posible modificar la oferta.'
        )
      });
  }

  seleccionarProducto(producto: IProductoOfertaProveedor): void {
    this.seleccionada = producto;
    this.imagenesProducto = [];
    this.incorporacionForm.reset({ descripcion: '', stockMinimo: 5 });
    this.limpiarMensajes();
  }

  cancelarIncorporacion(): void {
    this.seleccionada = null;
    this.imagenesProducto = [];
  }

  seleccionarImagenesProducto(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.imagenesProducto = Array.from(input.files ?? []).slice(0, 3);
  }

  incorporarProducto(): void {
    this.incorporacionForm.markAllAsTouched();
    if (!this.seleccionada || this.incorporacionForm.invalid || this.guardando) return;

    this.guardando = true;
    this.limpiarMensajes();
    const valores = this.incorporacionForm.getRawValue();
    this.servicio
      .incorporarProducto(this.seleccionada.productoProveedorCatalogoId, {
        descripcion: valores.descripcion.trim() || null,
        stockMinimo: valores.stockMinimo
      })
      .subscribe({
        next: respuesta => {
          if (!respuesta.data) {
            this.finalizarError(
              respuesta.error || 'No fue posible incorporar el producto.'
            );
            return;
          }

          if (this.imagenesProducto.length) {
            this.subirImagenesProducto(
              respuesta.data.productoId,
              respuesta.data.nombre,
              respuesta.data.codigo,
              respuesta.data.precioVenta
            );
            return;
          }

          this.finalizarIncorporacion(
            respuesta.data.nombre,
            respuesta.data.codigo,
            respuesta.data.precioVenta
          );
        },
        error: err => this.finalizarError(
          err?.error?.error || 'No fue posible incorporar el producto.'
        )
      });
  }

  productosDisponibles(): IProductoOfertaProveedor[] {
    return this.productos.filter(producto => !producto.incorporado && producto.activo);
  }

  categoriaActual(): ICategoriaOfertaProveedor | undefined {
    return this.categorias.find(categoria => categoria.categoriaId === this.categoriaId);
  }

  productoExistenteSeleccionado(): IProductoExistenteProveedor | undefined {
    const productoId = this.productoExistenteForm.controls.productoId.value;
    return this.productosExistentes.find(producto => producto.productoId === productoId);
  }

  moneda(valor: number): string {
    return new Intl.NumberFormat('es-CR', {
      style: 'currency',
      currency: 'CRC'
    }).format(valor);
  }

  iniciales(nombre: string): string {
    return nombre
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(parte => parte[0])
      .join('')
      .toUpperCase();
  }

  private cargarRuta(): void {
    this.cargando = true;
    this.proveedor = null;
    this.seleccionada = null;
    this.editandoOferta = null;
    this.limpiarMensajes();

    if (!this.proveedorId) {
      this.servicio.listar().subscribe({
        next: respuesta => {
          this.proveedores = respuesta.data ?? [];
          this.cargando = false;
          this.cdr.markForCheck();
        },
        error: err => this.finalizarCargaError(
          err?.error?.error || 'No fue posible cargar los proveedores.'
        )
      });
      return;
    }

    this.servicio.obtener(this.proveedorId).subscribe({
      next: respuesta => {
        if (!respuesta.data) {
          this.finalizarCargaError('El proveedor no existe.');
          return;
        }

        this.proveedor = respuesta.data;
        this.cargarCategorias();
      },
      error: err => this.finalizarCargaError(
        err?.error?.error || 'El proveedor no existe.'
      )
    });
  }

  private cargarCategorias(): void {
    this.cargando = true;
    this.servicio.categorias(this.proveedorId).subscribe({
      next: respuesta => {
        this.categorias = respuesta.data ?? [];
        if (this.categoriaId) {
          const pertenece = this.categorias.some(
            categoria => categoria.categoriaId === this.categoriaId
          );
          if (!pertenece) {
            this.finalizarCargaError('La categoría no pertenece al proveedor.');
            return;
          }
          this.cargarProductos();
        } else {
          this.cargando = false;
          this.cdr.markForCheck();
        }
      },
      error: err => this.finalizarCargaError(
        err?.error?.error || 'No fue posible cargar las categorías.'
      )
    });

    this.servicio.categorias(this.proveedorId, undefined, true).subscribe({
      next: respuesta => {
        this.categoriasDisponibles = respuesta.data ?? [];
        this.cdr.markForCheck();
      }
    });
  }

  private cargarProductos(): void {
    this.cargando = true;
    this.servicio.productos(this.proveedorId, this.categoriaId).subscribe({
      next: respuesta => {
        this.productos = respuesta.data ?? [];
        this.cargando = false;
        this.cdr.markForCheck();
      },
      error: err => this.finalizarCargaError(
        err?.error?.error || 'No fue posible cargar los productos del proveedor.'
      )
    });

    this.servicio
      .productosExistentes(this.proveedorId, this.categoriaId)
      .subscribe({
        next: respuesta => {
          this.productosExistentes = respuesta.data ?? [];
          this.cdr.markForCheck();
        }
      });
  }

  private cargarCatalogos(): void {
    this.productoServicio.catalogos().subscribe({
      next: respuesta => {
        this.familias = (respuesta.data?.familias ?? []).filter(familia => familia.activo);
        this.impuestos = respuesta.data?.impuestos ?? [];
        this.cdr.markForCheck();
      }
    });
  }

  private subirImagenCategoria(
    categoriaId: number,
    nombre: string,
    archivo: File
  ): void {
    this.categoriaServicio.subirImagen(categoriaId, archivo).subscribe({
      next: () => this.finalizarCategoriaNueva(nombre),
      error: () => {
        this.guardando = false;
        this.error = `${nombre} se creó y quedó relacionada, pero la imagen no pudo cargarse.`;
        this.cancelarCategoria();
        this.cargarCategorias();
      }
    });
  }

  private finalizarCategoriaNueva(nombre: string): void {
    this.guardando = false;
    this.exito = `${nombre} se creó y quedó relacionada con el proveedor.`;
    this.cancelarCategoria();
    this.cargarCategorias();
  }

  private subirImagenesProducto(
    productoId: number,
    nombre: string,
    codigo: string,
    precioVenta: number
  ): void {
    this.imagenServicio.subirImagenes(productoId, this.imagenesProducto).subscribe({
      next: () => this.finalizarIncorporacion(nombre, codigo, precioVenta),
      error: () => {
        this.guardando = false;
        this.error = `${nombre} se incorporó con stock 0, pero las imágenes no pudieron cargarse.`;
        this.cancelarIncorporacion();
        this.cargarProductos();
      }
    });
  }

  private finalizarIncorporacion(
    nombre: string,
    codigo: string,
    precioVenta: number
  ): void {
    this.guardando = false;
    this.exito = `${nombre} se incorporó con código ${codigo}, precio de venta ${this.moneda(precioVenta)} y stock 0.`;
    this.cancelarIncorporacion();
    this.cargarProductos();
  }

  private finalizarCargaError(mensaje: string): void {
    this.error = mensaje;
    this.cargando = false;
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
