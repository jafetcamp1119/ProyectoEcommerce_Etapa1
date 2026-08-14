export interface IProveedor {
  proveedorId: number;
  nombre: string;
  correo: string | null;
  telefono: string | null;
  direccion: string | null;
  urlImagen: string | null;
  activo: boolean;
  fechaRegistro: string;
}

export interface IFamiliaOfertaProveedor {
  proveedorId: number;
  familiaId: number;
  nombre: string;
  descripcion: string | null;
  urlImagen: string | null;
  incorporada: boolean;
}

export interface ICategoriaOfertaProveedor {
  proveedorId: number;
  familiaId: number;
  categoriaId: number;
  familiaNombre: string;
  nombre: string;
  descripcion: string | null;
  urlImagen: string | null;
  incorporada: boolean;
}

export interface IProductoOfertaProveedor {
  productoProveedorCatalogoId: number;
  proveedorId: number;
  proveedorNombre: string;
  familiaId: number;
  familiaNombre: string;
  categoriaId: number;
  categoriaNombre: string;
  nombre: string;
  precioCompra: number;
  impuestoId: number;
  impuestoNombre: string;
  impuestoPorcentaje: number;
  productoId: number | null;
  codigo: string | null;
  stock: number;
  activo: boolean;
  incorporado: boolean;
}

export interface IIncorporarProductoProveedor {
  descripcion: string | null;
  stockMinimo: number;
}

export interface IProductoIncorporadoProveedor {
  productoId: number;
  codigo: string;
  nombre: string;
  precioCompra: number;
  precioVenta: number;
  stock: number;
}

export interface ICategoriaNuevaProveedor {
  familiaId: number;
  nombre: string;
  descripcion: string | null;
}

export interface ICategoriaProveedorResultado {
  categoriaId: number;
  familiaId: number;
  nombre: string;
  yaExistia: boolean;
}

export interface IProductoExistenteProveedor {
  productoId: number;
  codigo: string;
  nombre: string;
  impuestoId: number;
  impuestoNombre: string;
  activo: boolean;
}

export interface IAgregarProductoExistenteProveedor {
  productoId: number;
  precioCompra: number;
}

export interface ICrearProductoProveedor {
  nombre: string;
  precioCompra: number;
  impuestoId: number;
  activo: boolean;
}

export interface IModificarProductoProveedor {
  precioCompra: number;
  impuestoId: number;
  activo: boolean;
}

export type IProveedorGuardar = Pick<IProveedor,
  'proveedorId' | 'nombre' | 'correo' | 'telefono' | 'direccion' | 'urlImagen' | 'activo'
>;
