import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IFamiliaProducto } from '../model/IFamiliaProducto';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';

const baseUrl = environment.baseUrl;

// junta las llamadas de familias administrativas y la lista activa del Cliente
@Injectable({ providedIn: 'root' })
export class FamiliaProductoService {
  private http = inject(HttpClient);

  // trae todas las familias para mantenimiento, incluida UrlImagen
  listar(): Observable<IRespuesta<IFamiliaProducto[]>> {
    return this.http.get<IRespuesta<IFamiliaProducto[]>>(
      `${baseUrl}/FamiliaProducto/Listar`
    );
  }

  // trae solamente familias activas para las tarjetas del Cliente
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

  // guarda primero los datos de la familia y devuelve su ID
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

  // sube o cambia la imagen despues de tener el ID de la familia
  subirImagen(
    familiaId: number,
    archivo: File
  ): Observable<IRespuesta<IFamiliaProducto>> {

    const formData = new FormData();

    // "archivo" debe coincidir con el nombre que recibe el controller
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
