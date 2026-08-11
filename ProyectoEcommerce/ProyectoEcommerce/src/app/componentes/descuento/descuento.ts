import { DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import {
  ICatalogosDescuento,
  IDescuento,
  IProductoSelectorDescuento,
  TipoDescuento
} from '../../model/IDescuento';
import { DescuentoService } from '../../services/descuento';

@Component({
  selector: 'app-descuento',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './descuento.html',
  styleUrl: './descuento.css'
})
export class Descuento implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(DescuentoService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  descuentos: IDescuento[] = [];
  catalogos: ICatalogosDescuento = { familias: [], categorias: [], productos: [] };
  cargando = true;
  guardando = false;
  mostrarFormulario = false;
  editandoId = 0;
  mensaje = '';
  error = '';
  errorFechas = '';

  readonly tipos: { valor: TipoDescuento; etiqueta: string }[] = [
    { valor: 'FAMILIA', etiqueta: 'Familia' },
    { valor: 'CATEGORIA', etiqueta: 'Categoría' },
    { valor: 'PRODUCTO', etiqueta: 'Producto' },
    { valor: 'PROMOCIONAL', etiqueta: 'Promocional' }
  ];

  readonly formulario = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    tipoDescuento: ['FAMILIA' as TipoDescuento, Validators.required],
    porcentaje: [0, [Validators.required, Validators.min(0.01), Validators.max(100)]],
    fechaInicio: ['', Validators.required],
    fechaFin: ['', Validators.required],
    activo: [true],
    familiaId: [0],
    categoriaId: [0],
    productoId: [0],
    filtroFamiliaId: [0],
    filtroCategoriaId: [0]
  });

  get tipo(): TipoDescuento { return this.formulario.controls.tipoDescuento.value; }

  get categoriasFiltradas() {
    const familiaId = this.formulario.controls.filtroFamiliaId.value;
    return this.catalogos.categorias.filter(x => !familiaId || x.familiaId === familiaId);
  }

  get productosFiltrados(): IProductoSelectorDescuento[] {
    const familiaId = this.formulario.controls.filtroFamiliaId.value;
    const categoriaId = this.formulario.controls.filtroCategoriaId.value;
    return this.catalogos.productos.filter(x =>
      (!familiaId || x.familiaId === familiaId) && (!categoriaId || x.categoriaId === categoriaId));
  }

  ngOnInit(): void {
    forkJoin({ descuentos: this.servicio.listar(), catalogos: this.servicio.catalogos() })
      .pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => {
          this.descuentos = respuesta.descuentos.data ?? [];
          this.catalogos = respuesta.catalogos.data ?? this.catalogos;
          this.aplicarPreseleccion();
        },
        error: err => this.error = this.mensajeError(err)
      });
  }

  nuevo(): void {
    const inicio = new Date();
    inicio.setSeconds(0, 0);
    const fin = new Date(inicio);
    fin.setDate(fin.getDate() + 7);
    this.editandoId = 0;
    this.formulario.reset({
      nombre: '', tipoDescuento: 'FAMILIA', porcentaje: 0,
      fechaInicio: this.fechaLocal(inicio), fechaFin: this.fechaLocal(fin), activo: true,
      familiaId: 0, categoriaId: 0, productoId: 0, filtroFamiliaId: 0, filtroCategoriaId: 0
    });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  editar(descuento: IDescuento): void {
    this.editandoId = descuento.descuentoId;
    const categoria = descuento.categoriaId
      ? this.catalogos.categorias.find(x => x.categoriaId === descuento.categoriaId) : undefined;
    const producto = descuento.productoId
      ? this.catalogos.productos.find(x => x.productoId === descuento.productoId) : undefined;
    this.formulario.reset({
      nombre: descuento.nombre,
      tipoDescuento: descuento.tipoDescuento,
      porcentaje: descuento.porcentaje,
      fechaInicio: this.fechaLocal(new Date(descuento.fechaInicio)),
      fechaFin: this.fechaLocal(new Date(descuento.fechaFin)),
      activo: descuento.activo,
      familiaId: descuento.familiaId ?? 0,
      categoriaId: descuento.categoriaId ?? 0,
      productoId: descuento.productoId ?? 0,
      filtroFamiliaId: descuento.familiaId ?? categoria?.familiaId ?? producto?.familiaId ?? 0,
      filtroCategoriaId: producto?.categoriaId ?? 0
    });
    this.mostrarFormulario = true;
    this.limpiarMensajes();
  }

  cancelar(): void {
    this.mostrarFormulario = false;
    this.editandoId = 0;
    this.errorFechas = '';
  }

  cambiarTipo(): void {
    this.formulario.patchValue({ familiaId: 0, categoriaId: 0, productoId: 0, filtroFamiliaId: 0, filtroCategoriaId: 0 });
  }

  cambiarFiltroFamilia(): void {
    this.formulario.patchValue({ categoriaId: 0, productoId: 0, filtroCategoriaId: 0 });
  }

  cambiarFiltroCategoria(): void {
    this.formulario.controls.productoId.setValue(0);
  }

  guardar(): void {
    this.formulario.markAllAsTouched();
    this.errorFechas = '';
    const valor = this.formulario.getRawValue();
    if (this.formulario.invalid || this.guardando) return;
    if (new Date(valor.fechaFin).getTime() < new Date(valor.fechaInicio).getTime()) {
      this.errorFechas = 'La fecha de fin debe ser igual o posterior a la fecha de inicio.';
      return;
    }
    const destino = valor.tipoDescuento === 'FAMILIA' ? valor.familiaId
      : valor.tipoDescuento === 'CATEGORIA' ? valor.categoriaId : valor.productoId;
    if (!destino) {
      this.error = 'Selecciona el destino obligatorio del descuento.';
      return;
    }

    const datos: IDescuento = {
      descuentoId: this.editandoId,
      nombre: valor.nombre.trim(),
      tipoDescuento: valor.tipoDescuento,
      porcentaje: Number(valor.porcentaje),
      fechaInicio: valor.fechaInicio,
      fechaFin: valor.fechaFin,
      activo: valor.activo,
      familiaId: valor.tipoDescuento === 'FAMILIA' ? valor.familiaId : null,
      categoriaId: valor.tipoDescuento === 'CATEGORIA' ? valor.categoriaId : null,
      productoId: valor.tipoDescuento === 'PRODUCTO' || valor.tipoDescuento === 'PROMOCIONAL' ? valor.productoId : null,
      destino: '',
      vigenciaActual: 'Inactivo'
    };
    const solicitud = this.editandoId ? this.servicio.modificar(datos) : this.servicio.insertar(datos);
    this.guardando = true;
    this.limpiarMensajes();
    solicitud.pipe(finalize(() => { this.guardando = false; this.cdr.markForCheck(); })).subscribe({
      next: () => {
        this.mensaje = this.editandoId ? 'Descuento actualizado correctamente.' : 'Descuento creado correctamente.';
        this.cancelar();
        this.recargar();
      },
      error: err => this.error = this.mensajeError(err)
    });
  }

  cambiarEstado(descuento: IDescuento): void {
    this.servicio.cambiarEstado(descuento.descuentoId, !descuento.activo).subscribe({
      next: () => {
        this.mensaje = descuento.activo ? 'Descuento desactivado.' : 'Descuento activado.';
        this.recargar();
      },
      error: err => this.error = this.mensajeError(err)
    });
  }

  etiquetaTipo(tipo: TipoDescuento): string {
    return this.tipos.find(x => x.valor === tipo)?.etiqueta ?? tipo;
  }

  claseVigencia(vigencia: string): string {
    return `status-${vigencia.toLowerCase()}`;
  }

  private recargar(): void {
    this.servicio.listar().subscribe({
      next: respuesta => { this.descuentos = respuesta.data ?? []; this.cdr.markForCheck(); },
      error: err => this.error = this.mensajeError(err)
    });
  }

  private aplicarPreseleccion(): void {
    const tipo = this.route.snapshot.queryParamMap.get('tipo') as TipoDescuento | null;
    if (!tipo || !this.tipos.some(x => x.valor === tipo)) return;
    this.nuevo();
    const familiaId = Number(this.route.snapshot.queryParamMap.get('familiaId')) || 0;
    const categoriaId = Number(this.route.snapshot.queryParamMap.get('categoriaId')) || 0;
    const productoId = Number(this.route.snapshot.queryParamMap.get('productoId')) || 0;
    const categoria = this.catalogos.categorias.find(x => x.categoriaId === categoriaId);
    const producto = this.catalogos.productos.find(x => x.productoId === productoId);
    this.formulario.patchValue({
      tipoDescuento: tipo,
      familiaId: tipo === 'FAMILIA' ? familiaId : 0,
      categoriaId: tipo === 'CATEGORIA' ? categoriaId : 0,
      productoId: tipo === 'PRODUCTO' || tipo === 'PROMOCIONAL' ? productoId : 0,
      filtroFamiliaId: familiaId || categoria?.familiaId || producto?.familiaId || 0,
      filtroCategoriaId: producto?.categoriaId || 0
    });
  }

  private fechaLocal(fecha: Date): string {
    const compensada = new Date(fecha.getTime() - fecha.getTimezoneOffset() * 60000);
    return compensada.toISOString().slice(0, 16);
  }

  private limpiarMensajes(): void { this.mensaje = ''; this.error = ''; this.errorFechas = ''; }
  private mensajeError(error: any): string {
    return error?.error?.error || error?.error?.title || 'No fue posible completar la operación.';
  }
}
