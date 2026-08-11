import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ICategoria } from '../../model/ICategoria';
import { IFiltroProductos, IProducto, IProductoCatalogo, IProductoGuardar } from '../../model/IProducto';
import { AutenticacionService } from '../../services/autenticacion';
import { ProductoService } from '../../services/producto';
import { CarritoService } from '../../services/carrito';
import { FamiliasCliente } from '../familias-cliente/familias-cliente';

type ProductoVista = IProductoCatalogo & Partial<IProducto>;

/** Presenta el catálogo por alcance al Cliente y el mantenimiento completo al Administrador. */
@Component({
  selector: 'app-producto',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe, FamiliasCliente],
  templateUrl: './producto.html',
  styleUrl: './producto.css'
})
export class Producto implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(ProductoService);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly carrito = inject(CarritoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly esAdmin = this.autenticacion.esAdministrador();
  readonly esCliente = this.autenticacion.esCliente();
  readonly tamanos = [25, 50, 75, 100] as const;
  productos: ProductoVista[] = [];
  familias: { familiaId: number; nombre: string }[] = [];
  categorias: ICategoria[] = [];
  impuestos: { impuestoId: number; nombre: string; porcentaje: number }[] = [];
  imagenesFallidas = new Set<number>();
  procesandoCarrito = new Set<number>();
  cargando = true;
  guardando = false;
  mostrarFormulario = false;
  editandoId = 0;
  pagina = 1;
  tamanoPagina: 25 | 50 | 75 | 100 = 25;
  total = 0;
  mensaje = '';
  error = '';
  familiaRutaId = 0;
  familiaSeleccionadaNombre = '';
  categoriaRutaId = 0;
  categoriaSeleccionadaNombre = '';
  alcanceCliente: '' | 'global' | 'familia' | 'categoria' = '';

  readonly filtros = this.fb.nonNullable.group({
    texto: ['', Validators.maxLength(120)],
    familiaId: [''],
    categoriaId: [''],
    precioMinimo: ['', Validators.min(0)],
    precioMaximo: ['', Validators.min(0)],
    disponibilidad: [''],
    estado: [''],
    orden: ['nombre_asc']
  });

  readonly formulario = this.fb.nonNullable.group({
    codigo: ['', [Validators.required, Validators.maxLength(50)]],
    nombre: ['', [Validators.required, Validators.maxLength(120)]],
    descripcion: ['', Validators.maxLength(500)],
    categoriaId: [0, Validators.min(1)],
    impuestoId: [0, Validators.min(1)],
    precioVenta: [0, [Validators.required, Validators.min(0)]],
    costo: [0, [Validators.required, Validators.min(0)]],
    stock: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)]],
    stockMinimo: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)]],
    activo: [true]
  });

  get categoriasFiltradas(): ICategoria[] {
    const familiaId = Number(this.filtros.controls.familiaId.value) || 0;
    return familiaId ? this.categorias.filter(x => x.familiaId === familiaId) : this.categorias;
  }

  get totalPaginas(): number { return Math.max(1, Math.ceil(this.total / this.tamanoPagina)); }

  get mostrarFamiliasCliente(): boolean {
    return this.esCliente && !this.alcanceCliente;
  }

  get tituloCliente(): string {
    if (this.alcanceCliente === 'global') return 'Resultados en todo el catálogo';
    if (this.alcanceCliente === 'familia') return `Resultados en ${this.familiaSeleccionadaNombre}`;
    return `Productos de ${this.categoriaSeleccionadaNombre}`;
  }

  get descripcionCliente(): string {
    if (this.alcanceCliente === 'global') return 'Busca y filtra productos activos de todas las familias.';
    if (this.alcanceCliente === 'familia') return 'La búsqueda está limitada a los productos de esta familia.';
    return 'Busca y filtra los productos disponibles en esta categoría.';
  }

  get rutaRegresoCliente(): any[] {
    return this.familiaRutaId ? ['/productos/familia', this.familiaRutaId] : ['/productos'];
  }

  get textoRegresoCliente(): string {
    return this.familiaRutaId ? 'Volver a categorías' : 'Volver a familias';
  }

  ngOnInit(): void {
    if (this.esCliente) {
      // Los datos de la ruta distinguen búsqueda global, dentro de familia o dentro de categoría.
      this.alcanceCliente = (this.route.snapshot.data['alcanceCliente'] ?? '') as typeof this.alcanceCliente;
      if (!this.alcanceCliente) {
        this.cargando = false;
        return;
      }

      const textoInicial = this.route.snapshot.queryParamMap.get('texto')?.trim() ?? '';
      this.filtros.controls.texto.setValue(textoInicial);

      if (this.alcanceCliente !== 'global') {
        const familiaId = Number(this.route.snapshot.paramMap.get('familiaId'));
        if (!Number.isInteger(familiaId) || familiaId <= 0) {
          this.cargando = false;
          this.error = 'La familia solicitada no es válida.';
          return;
        }
        this.familiaRutaId = familiaId;
        this.filtros.controls.familiaId.setValue(String(familiaId));
      }

      if (this.alcanceCliente === 'categoria') {
        const categoriaId = Number(this.route.snapshot.paramMap.get('categoriaId'));
        if (!Number.isInteger(categoriaId) || categoriaId <= 0) {
          this.cargando = false;
          this.error = 'La categoría solicitada no es válida.';
          return;
        }
        this.categoriaRutaId = categoriaId;
        this.filtros.controls.categoriaId.setValue(String(categoriaId));
      }

      if (!['global', 'familia', 'categoria'].includes(this.alcanceCliente)) {
        this.cargando = false;
        this.error = 'El alcance del catálogo no es válido.';
        return;
      }
    }

    this.servicio.catalogos().subscribe({
      next: respuesta => {
        this.familias = respuesta.data?.familias ?? [];
        this.categorias = respuesta.data?.categorias ?? [];
        this.impuestos = respuesta.data?.impuestos ?? [];
        if (this.esCliente && this.familiaRutaId) {
          const familia = this.familias.find(x => x.familiaId === this.familiaRutaId);
          if (!familia) {
            this.cargando = false;
            this.error = 'La familia solicitada no está disponible.';
            this.cdr.markForCheck();
            return;
          }
          this.familiaSeleccionadaNombre = familia.nombre;
        }
        if (this.esCliente && this.categoriaRutaId) {
          const categoria = this.categorias.find(
            x => x.categoriaId === this.categoriaRutaId && x.familiaId === this.familiaRutaId
          );
          if (!categoria) {
            this.cargando = false;
            this.error = 'La categoría solicitada no pertenece a la familia o no está disponible.';
            this.cdr.markForCheck();
            return;
          }
          this.categoriaSeleccionadaNombre = categoria.nombre;
        }
        if (this.esCliente && this.alcanceCliente !== 'categoria' && !this.filtros.controls.texto.value.trim()) {
          this.cargando = false;
          this.cdr.markForCheck();
          return;
        }
        this.cargar();
        this.cdr.markForCheck();
      },
      error: err => { this.cargando = false; this.error = this.mensajeError(err); this.cdr.markForCheck(); }
    });
  }

  buscar(): void {
    const texto = this.filtros.controls.texto.value.trim();
    if (this.esCliente && this.alcanceCliente !== 'categoria' && !texto) {
      void this.router.navigate(this.rutaRegresoCliente);
      return;
    }
    this.pagina = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    if (this.esCliente && this.alcanceCliente !== 'categoria') {
      void this.router.navigate(this.rutaRegresoCliente);
      return;
    }
    this.filtros.reset({
      texto: '', familiaId: this.esCliente ? String(this.familiaRutaId) : '',
      categoriaId: this.esCliente ? String(this.categoriaRutaId) : '', precioMinimo: '', precioMaximo: '',
      disponibilidad: '', estado: '', orden: 'nombre_asc'
    });
    this.pagina = 1;
    this.cargar();
  }

  cambiarFamilia(): void {
    const categoriaId = Number(this.filtros.controls.categoriaId.value) || 0;
    if (categoriaId && !this.categoriasFiltradas.some(x => x.categoriaId === categoriaId)) {
      this.filtros.controls.categoriaId.setValue('');
    }
  }

  cambiarTamano(valor: string): void {
    const tamano = Number(valor);
    if (this.tamanos.includes(tamano as 25 | 50 | 75 | 100)) {
      this.tamanoPagina = tamano as 25 | 50 | 75 | 100;
      this.pagina = 1;
      this.cargar();
    }
  }

  irPagina(delta: number): void {
    const nuevaPagina = Math.min(this.totalPaginas, Math.max(1, this.pagina + delta));
    if (nuevaPagina !== this.pagina) { this.pagina = nuevaPagina; this.cargar(); }
  }

  nuevo(): void {
    this.editandoId = 0;
    this.formulario.reset({
      codigo: '', nombre: '', descripcion: '', categoriaId: 0, impuestoId: 0,
      precioVenta: 0, costo: 0, stock: 0, stockMinimo: 0, activo: true
    });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  editar(producto: ProductoVista): void {
    if (!this.esAdmin || producto.codigo === undefined) return;
    this.editandoId = producto.productoId;
    this.formulario.reset({
      codigo: producto.codigo,
      nombre: producto.nombre,
      descripcion: producto.descripcion ?? '',
      categoriaId: producto.categoriaId,
      impuestoId: producto.impuestoId,
      precioVenta: producto.precioVenta,
      costo: producto.costo ?? 0,
      stock: producto.stock ?? 0,
      stockMinimo: producto.stockMinimo ?? 0,
      activo: producto.activo ?? true
    });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
    queueMicrotask(() => document.getElementById('formulario-producto')?.scrollIntoView({ behavior: 'smooth' }));
  }

  cancelarFormulario(): void {
    this.mostrarFormulario = false;
    this.editandoId = 0;
  }

  guardar(): void {
    this.formulario.markAllAsTouched();
    if (!this.esAdmin || this.formulario.invalid || this.guardando) return;
    const valores = this.formulario.getRawValue();
    const datos: IProductoGuardar = {
      productoId: this.editandoId,
      categoriaId: valores.categoriaId,
      impuestoId: valores.impuestoId,
      codigo: valores.codigo.trim(),
      nombre: valores.nombre.trim(),
      descripcion: valores.descripcion.trim() || null,
      precioVenta: Number(valores.precioVenta),
      costo: Number(valores.costo),
      stock: Number(valores.stock),
      stockMinimo: Number(valores.stockMinimo),
      activo: valores.activo
    };
    const editando = this.editandoId > 0;
    this.guardando = true;
    this.limpiarMensajes();
    const solicitud = editando ? this.servicio.modificar(datos) : this.servicio.insertar(datos);
    solicitud.pipe(finalize(() => { this.guardando = false; this.cdr.markForCheck(); })).subscribe({
      next: () => {
        this.mensaje = editando ? 'Producto actualizado correctamente.' : 'Producto creado correctamente.';
        this.cancelarFormulario();
        this.cargar(false);
      },
      error: err => this.error = this.mensajeError(err)
    });
  }

  cambiarEstado(producto: ProductoVista): void {
    if (!this.esAdmin || producto.activo === undefined || this.guardando) return;
    this.guardando = true;
    this.limpiarMensajes();
    this.servicio.cambiarEstado(producto.productoId, !producto.activo)
      .pipe(finalize(() => { this.guardando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: () => {
          this.mensaje = producto.activo ? 'Producto desactivado.' : 'Producto activado.';
          this.cargar(false);
        },
        error: err => this.error = this.mensajeError(err)
      });
  }

  agregarAlCarrito(producto: ProductoVista): void {
    if (!this.esCliente || !producto.disponible || this.procesandoCarrito.has(producto.productoId)) return;
    this.limpiarMensajes();
    this.procesandoCarrito.add(producto.productoId);
    this.carrito.agregar(producto.productoId, 1)
      .pipe(finalize(() => {
        this.procesandoCarrito.delete(producto.productoId);
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => this.mensaje = 'El producto fue agregado al carrito.',
        error: err => this.error = this.mensajeError(err)
      });
  }

  marcarImagenError(productoId: number): void {
    this.imagenesFallidas.add(productoId);
  }

  iniciales(nombre: string): string {
    const palabras = nombre.trim().split(/\s+/).filter(Boolean);
    if (!palabras.length) return 'PR';
    if (palabras.length === 1) return palabras[0].slice(0, 2).toUpperCase();
    return `${palabras[0][0]}${palabras[1][0]}`.toUpperCase();
  }

  nombreFamilia(familiaId: number): string {
    return this.familias.find(x => x.familiaId === familiaId)?.nombre ?? 'Familia';
  }

  claseStock(estado: string): string {
    if (estado === 'Disponible') return 'stock-ok';
    if (estado === 'Stock bajo') return 'stock-low';
    return 'stock-out';
  }

  private cargar(limpiarMensajes = true): void {
    if (this.filtros.invalid) return;
    const valores = this.filtros.getRawValue();
    // Para el Cliente, familia y categoría provienen de la ruta y no de selectores editables.
    const filtro: IFiltroProductos = {
      texto: valores.texto.trim() || undefined,
      familiaId: this.esCliente ? (this.familiaRutaId || undefined) : (Number(valores.familiaId) || undefined),
      categoriaId: this.esCliente ? (this.categoriaRutaId || undefined) : (Number(valores.categoriaId) || undefined),
      precioMinimo: valores.precioMinimo === '' ? undefined : Number(valores.precioMinimo),
      precioMaximo: valores.precioMaximo === '' ? undefined : Number(valores.precioMaximo),
      disponibilidad: (valores.disponibilidad || undefined) as IFiltroProductos['disponibilidad'],
      activo: valores.estado === '' ? undefined : valores.estado === 'activo',
      pagina: this.pagina,
      tamanoPagina: this.tamanoPagina,
      orden: valores.orden as IFiltroProductos['orden']
    };
    if (limpiarMensajes) this.limpiarMensajes();
    this.cargando = true;
    const solicitud = this.esAdmin ? this.servicio.administracion(filtro) : this.servicio.catalogo(filtro);
    solicitud.pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); })).subscribe({
      next: respuesta => {
        this.productos = respuesta.data?.items ?? [];
        this.total = respuesta.data?.total ?? 0;
        this.pagina = respuesta.data?.pagina ?? this.pagina;
        this.imagenesFallidas.clear();
      },
      error: err => { this.productos = []; this.total = 0; this.error = this.mensajeError(err); }
    });
  }

  private limpiarMensajes(): void { this.mensaje = ''; this.error = ''; }

  private mensajeError(error: any): string {
    const validaciones = error?.error?.errors as Record<string, string[]> | undefined;
    if (validaciones) return Object.values(validaciones).flat()[0] ?? 'Revisa los datos ingresados.';
    return error?.error?.error || error?.error?.title || 'No fue posible completar la operación.';
  }
}
