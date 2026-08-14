import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { IRespuesta } from '../model/IAuth';
import {
  IAgregarProductoExistenteProveedor,
  ICategoriaOfertaProveedor,
  ICategoriaNuevaProveedor,
  ICategoriaProveedorResultado,
  ICrearProductoProveedor,
  IFamiliaOfertaProveedor,
  IIncorporarProductoProveedor,
  IModificarProductoProveedor,
  IProductoExistenteProveedor,
  IProductoIncorporadoProveedor,
  IProductoOfertaProveedor,
  IProveedor,
  IProveedorGuardar
} from '../model/IProveedor';
import { environment } from '../../environments/environment';

const baseUrl = `${environment.baseUrl}/Proveedor`;

@Injectable({ providedIn: 'root' })
export class ProveedorService {
  private readonly http = inject(HttpClient);

  listar(soloActivos = false): Observable<IRespuesta<IProveedor[]>> {
    return this.http.get<IRespuesta<IProveedor[]>>(`${baseUrl}/Listar`, {
      params: { soloActivos }
    });
  }

  obtener(id: number): Observable<IRespuesta<IProveedor>> {
    return this.http.get<IRespuesta<IProveedor>>(`${baseUrl}/Obtener/${id}`);
  }

  insertar(datos: IProveedorGuardar): Observable<IRespuesta<IProveedor>> {
    return this.http.post<IRespuesta<IProveedor>>(`${baseUrl}/Insertar`, datos);
  }

  modificar(datos: IProveedorGuardar): Observable<IRespuesta<IProveedor>> {
    return this.http.put<IRespuesta<IProveedor>>(`${baseUrl}/Modificar`, datos);
  }

  cambiarEstado(id: number, activo: boolean): Observable<IRespuesta<IProveedor>> {
    return this.http.put<IRespuesta<IProveedor>>(`${baseUrl}/CambiarEstado/${id}`, { activo });
  }

  subirImagen(id: number, archivo: File): Observable<IRespuesta<IProveedor>> {
    const formulario = new FormData();
    formulario.append('archivo', archivo);
    return this.http.post<IRespuesta<IProveedor>>(`${baseUrl}/SubirImagen/${id}`, formulario);
  }

  familias(proveedorId: number, soloDisponibles = false): Observable<IRespuesta<IFamiliaOfertaProveedor[]>> {
    return this.http.get<IRespuesta<IFamiliaOfertaProveedor[]>>(`${baseUrl}/${proveedorId}/Familias`, {
      params: { soloDisponibles }
    });
  }

  categorias(
    proveedorId: number,
    familiaId?: number,
    soloDisponibles = false
  ): Observable<IRespuesta<ICategoriaOfertaProveedor[]>> {
    let params = new HttpParams().set('soloDisponibles', soloDisponibles);
    if (familiaId) params = params.set('familiaId', familiaId);
    return this.http.get<IRespuesta<ICategoriaOfertaProveedor[]>>(
      `${baseUrl}/${proveedorId}/Categorias`,
      { params }
    );
  }

  productos(
    proveedorId: number,
    categoriaId?: number,
    soloDisponibles = false
  ): Observable<IRespuesta<IProductoOfertaProveedor[]>> {
    let params = new HttpParams().set('soloDisponibles', soloDisponibles);
    if (categoriaId) params = params.set('categoriaId', categoriaId);
    return this.http.get<IRespuesta<IProductoOfertaProveedor[]>>(
      `${baseUrl}/${proveedorId}/Productos`,
      { params }
    );
  }

  productosExistentes(
    proveedorId: number,
    categoriaId: number
  ): Observable<IRespuesta<IProductoExistenteProveedor[]>> {
    return this.http.get<IRespuesta<IProductoExistenteProveedor[]>>(
      `${baseUrl}/${proveedorId}/Categorias/${categoriaId}/ProductosExistentes`
    );
  }

  asociarCategoriaExistente(
    proveedorId: number,
    categoriaId: number
  ): Observable<IRespuesta<ICategoriaProveedorResultado>> {
    return this.http.post<IRespuesta<ICategoriaProveedorResultado>>(
      `${baseUrl}/${proveedorId}/Categorias/Existente/${categoriaId}`,
      {}
    );
  }

  crearCategoria(
    proveedorId: number,
    datos: ICategoriaNuevaProveedor
  ): Observable<IRespuesta<ICategoriaProveedorResultado>> {
    return this.http.post<IRespuesta<ICategoriaProveedorResultado>>(
      `${baseUrl}/${proveedorId}/Categorias/Nueva`,
      datos
    );
  }

  agregarProductoExistente(
    proveedorId: number,
    categoriaId: number,
    datos: IAgregarProductoExistenteProveedor
  ): Observable<IRespuesta<IProductoOfertaProveedor>> {
    return this.http.post<IRespuesta<IProductoOfertaProveedor>>(
      `${baseUrl}/${proveedorId}/Categorias/${categoriaId}/Productos/Existente`,
      datos
    );
  }

  crearProducto(
    proveedorId: number,
    categoriaId: number,
    datos: ICrearProductoProveedor
  ): Observable<IRespuesta<IProductoOfertaProveedor>> {
    return this.http.post<IRespuesta<IProductoOfertaProveedor>>(
      `${baseUrl}/${proveedorId}/Categorias/${categoriaId}/Productos/Nuevo`,
      datos
    );
  }

  modificarProducto(
    ofertaId: number,
    datos: IModificarProductoProveedor
  ): Observable<IRespuesta<IProductoOfertaProveedor>> {
    return this.http.put<IRespuesta<IProductoOfertaProveedor>>(
      `${baseUrl}/Productos/${ofertaId}`,
      datos
    );
  }

  incorporarFamilia(proveedorId: number, familiaId: number): Observable<IRespuesta<boolean>> {
    return this.http.post<IRespuesta<boolean>>(
      `${baseUrl}/${proveedorId}/Familias/${familiaId}/Incorporar`,
      {}
    );
  }

  incorporarCategoria(proveedorId: number, categoriaId: number): Observable<IRespuesta<boolean>> {
    return this.http.post<IRespuesta<boolean>>(
      `${baseUrl}/${proveedorId}/Categorias/${categoriaId}/Incorporar`,
      {}
    );
  }

  incorporarProducto(
    ofertaId: number,
    datos: IIncorporarProductoProveedor
  ): Observable<IRespuesta<IProductoIncorporadoProveedor>> {
    return this.http.post<IRespuesta<IProductoIncorporadoProveedor>>(
      `${baseUrl}/Productos/${ofertaId}/Incorporar`,
      datos
    );
  }
}
