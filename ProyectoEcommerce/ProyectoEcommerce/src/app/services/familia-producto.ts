import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IFamiliaProducto } from '../model/IFamiliaProducto';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';

const baseUrl = environment.baseUrl;

/**
 * Proporciona consultas y mantenimiento de familias, incluida la lista activa del catálogo.
 */
@Injectable({ providedIn: 'root' })
export class FamiliaProductoService {
  private http = inject(HttpClient);

  listar(): Observable<IRespuesta<IFamiliaProducto[]>> {
    return this.http.get<IRespuesta<IFamiliaProducto[]>>(
      `${baseUrl}/FamiliaProducto/Listar`
    );
  }

  listarCliente(): Observable<IRespuesta<IFamiliaProducto[]>> {
    return this.http.get<IRespuesta<IFamiliaProducto[]>>(
      `${baseUrl}/FamiliaProducto/Cliente`
    );
  }

  obtener(id: number): Observable<IRespuesta<IFamiliaProducto>> {
    return this.http.get<IRespuesta<IFamiliaProducto>>(
      `${baseUrl}/FamiliaProducto/Obtener/${id}`
    );
  }

  buscar(nombre: string): Observable<IRespuesta<IFamiliaProducto[]>> {
    return this.http.get<IRespuesta<IFamiliaProducto[]>>(
      `${baseUrl}/FamiliaProducto/Buscar`,
      { params: { nombre } }
    );
  }

  insertar(datos: IFamiliaProducto): Observable<IRespuesta<IFamiliaProducto>> {
    return this.http.post<IRespuesta<IFamiliaProducto>>(
      `${baseUrl}/FamiliaProducto/Insertar`,
      datos
    );
  }

  modificar(datos: IFamiliaProducto): Observable<IRespuesta<IFamiliaProducto>> {
    return this.http.put<IRespuesta<IFamiliaProducto>>(
      `${baseUrl}/FamiliaProducto/Modificar`,
      datos
    );
  }

  // Sube o cambia la imagen de una familia.
  subirImagen(
    familiaId: number,
    archivo: File
  ): Observable<IRespuesta<IFamiliaProducto>> {

    const formData = new FormData();

    // "archivo" debe coincidir con el nombre usado en el controlador.
    formData.append('archivo', archivo);

    return this.http.post<IRespuesta<IFamiliaProducto>>(
      `${baseUrl}/FamiliaProducto/SubirImagen/${familiaId}`,
      formData
    );
  }

  eliminar(id: number): Observable<IRespuesta<boolean>> {
    return this.http.delete<IRespuesta<boolean>>(
      `${baseUrl}/FamiliaProducto/Eliminar/${id}`
    );
  }
}
