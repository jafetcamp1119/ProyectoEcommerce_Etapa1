import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';
import { IProductoImagen } from '../model/IProductoImagen';

@Injectable({ providedIn: 'root' })
export class ProductoImagenService {

  // Permite realizar las peticiones HTTP hacia el backend.
  private readonly http = inject(HttpClient);

  // Obtiene todas las imágenes que tiene un producto.
  listarPorProducto(
    productoId: number
  ): Observable<IRespuesta<IProductoImagen[]>> {

    // Envía el ID del producto a la API.
    return this.http.get<IRespuesta<IProductoImagen[]>>(
      `${environment.baseUrl}/ProductoImagen/ListarPorProducto/${productoId}`
    );
  }

  // Obtiene solamente la imagen principal del producto.
  obtenerPrincipal(
    productoId: number
  ): Observable<IRespuesta<IProductoImagen | null>> {

    return this.http.get<IRespuesta<IProductoImagen | null>>(
      `${environment.baseUrl}/ProductoImagen/Principal/${productoId}`
    );
  }

  // Envía al backend las imágenes seleccionadas
  // por el administrador desde su computadora.
  subirImagenes(
    productoId: number,
    archivos: File[]
  ): Observable<any> {

    // FormData permite enviar archivos reales hacia la API.
    const formData = new FormData();

    // Agrega cada imagen seleccionada al FormData.
    archivos.forEach(archivo => {
      formData.append('archivos', archivo);
    });

    // Envía las imágenes al producto correspondiente.
    return this.http.post(
      `${environment.baseUrl}/ProductoImagen/Subir/${productoId}`,
      formData
    );
  }

  // Cambia cuál imagen será la principal del producto.
  establecerPrincipal(
    imagenId: number
  ): Observable<IRespuesta<boolean>> {

    // Envía el ID de la imagen que se quiere colocar como principal.
    return this.http.put<IRespuesta<boolean>>(
      `${environment.baseUrl}/ProductoImagen/Principal/${imagenId}`,
      {}
    );
  }

  // Elimina una imagen del producto.
  eliminar(
    imagenId: number
  ): Observable<any> {

    // Envía a la API el ID de la imagen que se quiere eliminar.
    return this.http.delete(
      `${environment.baseUrl}/ProductoImagen/Eliminar/${imagenId}`
    );
  }
}
