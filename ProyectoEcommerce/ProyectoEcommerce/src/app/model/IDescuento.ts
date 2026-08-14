import { ICategoria } from './ICategoria';
import { IFamiliaProducto } from './IFamiliaProducto';

// limita el tipo a los cuatro valores que tambien acepta la API
export type TipoDescuento = 'FAMILIA' | 'CATEGORIA' | 'PRODUCTO' | 'PROMOCIONAL';

// descuento administrativo con un solo destino segun su tipo
export interface IDescuento {
  descuentoId: number;
  nombre: string;
  tipoDescuento: TipoDescuento;
  productoId: number | null;
  categoriaId: number | null;
  familiaId: number | null;
  porcentaje: number;
  fechaInicio: string;
  fechaFin: string;
  activo: boolean;
  destino: string;
  vigenciaActual: 'Programado' | 'Vigente' | 'Finalizado' | 'Inactivo';
}

export interface IDescuentoAplicado {
  productoId: number;
  tieneDescuento: boolean;
  descuentoId: number | null;
  tipoDescuento: TipoDescuento | null;
  nombre: string | null;
  porcentaje: number;
  precioOriginal: number;
  montoDescuento: number;
  precioFinal: number;
}

export interface IProductoSelectorDescuento {
  productoId: number;
  categoriaId: number;
  familiaId: number;
  nombre: string;
  categoriaNombre: string;
  familiaNombre: string;
}

export interface ICatalogosDescuento {
  familias: IFamiliaProducto[];
  categorias: ICategoria[];
  productos: IProductoSelectorDescuento[];
}
