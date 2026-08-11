import { ICategoria } from './ICategoria';
import { IFamiliaProducto } from './IFamiliaProducto';
import { IImpuesto } from './IImpuesto';
import { IProductoImagen } from './IProductoImagen';

/** Información de producto visible en catálogo y detalle. */
export interface IProductoCatalogo {
  productoId: number;
  codigo: string;
  nombre: string;
  descripcion: string | null;
  precioVenta: number;
  familiaId: number;
  familiaNombre: string;
  categoriaId: number;
  categoriaNombre: string;
  impuestoId: number;
  impuestoNombre: string;
  impuestoPorcentaje: number;
  disponible: boolean;
  estadoStock: string;
  imagenPrincipal: IProductoImagen | null;
}

export interface IProducto extends IProductoCatalogo {
  codigo: string;
  costo: number;
  stock: number;
  stockMinimo: number;
  activo: boolean;
  fechaCreacion: string;
}

/** Filtros opcionales que se convierten en parámetros de la consulta de productos. */
export interface IFiltroProductos {
  texto?: string;
  familiaId?: number;
  categoriaId?: number;
  precioMinimo?: number;
  precioMaximo?: number;
  disponibilidad?: 'disponible' | 'bajo' | 'agotado';
  activo?: boolean;
  pagina?: number;
  tamanoPagina?: 25 | 50 | 75 | 100;
  orden?: 'nombre_asc' | 'nombre_desc' | 'precio_asc' | 'precio_desc' | 'fecha_asc' | 'fecha_desc';
}

export interface IPaginaProductos<T> {
  items: T[];
  pagina: number;
  tamanoPagina: number;
  total: number;
}

export interface ICatalogosProducto {
  familias: IFamiliaProducto[];
  categorias: ICategoria[];
  impuestos: IImpuesto[];
}

export type IProductoGuardar = Pick<IProducto,
  'productoId' | 'categoriaId' | 'impuestoId' | 'codigo' | 'nombre' | 'descripcion' |
  'precioVenta' | 'costo' | 'stock' | 'stockMinimo' | 'activo'
>;
