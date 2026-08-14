import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ICatalogosProducto,
  IFiltroProductos,
  IPaginaProductos,
  IProducto,
  IProductoCatalogo,
  IProductoGuardar
} from '../model/IProducto';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';

const baseUrl = `${environment.baseUrl}/Producto`;

// consume el catalogo filtrado y las operaciones administrativas de productos
@Injectable({ providedIn: 'root' })
export class ProductoService {
  private readonly http = inject(HttpClient);

  // manda filtros opcionales y recibe una pagina lista para el Cliente
  catalogo(filtro: IFiltroProductos = {}): Observable<IRespuesta<IPaginaProductos<IProductoCatalogo>>> {
    return this.http.get<IRespuesta<IPaginaProductos<IProductoCatalogo>>>(`${baseUrl}/Catalogo`, {
      params: this.parametros(filtro)
    });
  }

  // usa la ruta administrativa que tambien devuelve productos inactivos
  administracion(filtro: IFiltroProductos = {}): Observable<IRespuesta<IPaginaProductos<IProducto>>> {
    return this.http.get<IRespuesta<IPaginaProductos<IProducto>>>(`${baseUrl}/Administracion`, {
      params: this.parametros(filtro)
    });
  }

  // trae el detalle publico de un producto activo
  detalle(id: number): Observable<IRespuesta<IProductoCatalogo>> {
    return this.http.get<IRespuesta<IProductoCatalogo>>(`${baseUrl}/Detalle/${id}`);
  }

  // trae los datos completos que necesita el formulario de edicion
  detalleAdministracion(id: number): Observable<IRespuesta<IProducto>> {
    return this.http.get<IRespuesta<IProducto>>(`${baseUrl}/DetalleAdministracion/${id}`);
  }

  // pide familias, categorias e impuestos para llenar los select
  catalogos(): Observable<IRespuesta<ICatalogosProducto>> {
    return this.http.get<IRespuesta<ICatalogosProducto>>(`${baseUrl}/Catalogos`);
  }

  // manda los datos de un producto nuevo
  insertar(datos: IProductoGuardar): Observable<IRespuesta<IProducto>> {
    return this.http.post<IRespuesta<IProducto>>(`${baseUrl}/Insertar`, datos);
  }

  // manda los cambios de un producto existente
  modificar(datos: IProductoGuardar): Observable<IRespuesta<IProducto>> {
    return this.http.put<IRespuesta<IProducto>>(`${baseUrl}/Modificar`, datos);
  }

  // activa o desactiva sin borrar el registro
  cambiarEstado(id: number, activo: boolean): Observable<IRespuesta<boolean>> {
    return this.http.put<IRespuesta<boolean>>(`${baseUrl}/CambiarEstado/${id}`, { activo });
  }

  // convierte filtros con valor a parametros de la URL
  private parametros(filtro: IFiltroProductos): HttpParams {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '') params = params.set(clave, String(valor));
    }
    return params;
  }
}
