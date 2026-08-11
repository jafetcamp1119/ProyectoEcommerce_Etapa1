import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';
import { IProductoImagen } from '../model/IProductoImagen';

/** Consulta las imágenes ya vinculadas a un producto y su imagen principal. */
@Injectable({ providedIn: 'root' })
export class ProductoImagenService {
  private readonly http = inject(HttpClient);
  listarPorProducto(productoId: number): Observable<IRespuesta<IProductoImagen[]>> {
    return this.http.get<IRespuesta<IProductoImagen[]>>(
      `${environment.baseUrl}/ProductoImagen/ListarPorProducto/${productoId}`
    );
  }

  obtenerPrincipal(productoId: number): Observable<IRespuesta<IProductoImagen | null>> {
    return this.http.get<IRespuesta<IProductoImagen | null>>(
      `${environment.baseUrl}/ProductoImagen/Principal/${productoId}`
    );
  }
}
