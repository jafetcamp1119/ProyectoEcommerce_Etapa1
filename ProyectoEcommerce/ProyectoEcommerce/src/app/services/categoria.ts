import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ICategoria } from '../model/ICategoria';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';

const baseUrl = environment.baseUrl;

// junta el mantenimiento de categorias y la consulta activa por familia del Cliente
@Injectable({ providedIn: 'root' })
export class CategoriaService {

  private http = inject(HttpClient);

  // trae todas las categorias para administracion, incluida UrlImagen
  listar(): Observable<IRespuesta<ICategoria[]>> {
    return this.http.get<IRespuesta<ICategoria[]>>(
      `${baseUrl}/Categoria/Listar`
    );
  }

  // limita la lista administrativa a una familia
  listarPorFamilia(
    familiaId: number
  ): Observable<IRespuesta<ICategoria[]>> {
    return this.http.get<IRespuesta<ICategoria[]>>(
      `${baseUrl}/Categoria/ListarPorFamilia/${familiaId}`
    );
  }

  // trae las categorias activas que alimentan las tarjetas del Cliente
  listarClientePorFamilia(
    familiaId: number
  ): Observable<IRespuesta<ICategoria[]>> {
    return this.http.get<IRespuesta<ICategoria[]>>(
      `${baseUrl}/Categoria/Cliente/${familiaId}`
    );
  }

  obtener(
    id: number
  ): Observable<IRespuesta<ICategoria>> {
    return this.http.get<IRespuesta<ICategoria>>(
      `${baseUrl}/Categoria/Obtener/${id}`
    );
  }

  buscar(
    nombre: string
  ): Observable<IRespuesta<ICategoria[]>> {
    return this.http.get<IRespuesta<ICategoria[]>>(
      `${baseUrl}/Categoria/Buscar`,
      {
        params: { nombre }
      }
    );
  }

  // guarda primero los datos y devuelve el ID necesario para subir la imagen
  insertar(
    datos: ICategoria
  ): Observable<IRespuesta<ICategoria>> {
    return this.http.post<IRespuesta<ICategoria>>(
      `${baseUrl}/Categoria/Insertar`,
      datos
    );
  }

  modificar(
    datos: ICategoria
  ): Observable<IRespuesta<ICategoria>> {
    return this.http.put<IRespuesta<ICategoria>>(
      `${baseUrl}/Categoria/Modificar`,
      datos
    );
  }

  // sube o cambia la imagen despues de que la categoria ya tiene ID
  subirImagen(
    categoriaId: number,
    archivo: File
  ): Observable<IRespuesta<ICategoria>> {

    const formData = new FormData();

    // FormData manda el archivo como multipart/form-data al controller
    formData.append(
      'archivo',
      archivo
    );

    return this.http.post<IRespuesta<ICategoria>>(
      `${baseUrl}/Categoria/SubirImagen/${categoriaId}`,
      formData
    );
  }

  eliminar(
    id: number
  ): Observable<IRespuesta<boolean>> {
    return this.http.delete<IRespuesta<boolean>>(
      `${baseUrl}/Categoria/Eliminar/${id}`
    );
  }
}
