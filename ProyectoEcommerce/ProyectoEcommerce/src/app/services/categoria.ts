import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ICategoria } from '../model/ICategoria';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';

const baseUrl = environment.baseUrl;

/**
 * Consume los endpoints administrativos de categorías y la consulta activa por familia del Cliente.
 */
@Injectable({ providedIn: 'root' })
export class CategoriaService {
  private http = inject(HttpClient);
  listar(): Observable<IRespuesta<ICategoria[]>> { return this.http.get<IRespuesta<ICategoria[]>>(`${baseUrl}/Categoria/Listar`); }
  listarPorFamilia(familiaId: number): Observable<IRespuesta<ICategoria[]>> { return this.http.get<IRespuesta<ICategoria[]>>(`${baseUrl}/Categoria/ListarPorFamilia/${familiaId}`); }
  listarClientePorFamilia(familiaId: number): Observable<IRespuesta<ICategoria[]>> { return this.http.get<IRespuesta<ICategoria[]>>(`${baseUrl}/Categoria/Cliente/${familiaId}`); }
  obtener(id: number): Observable<IRespuesta<ICategoria>> { return this.http.get<IRespuesta<ICategoria>>(`${baseUrl}/Categoria/Obtener/${id}`); }
  buscar(nombre: string): Observable<IRespuesta<ICategoria[]>> { return this.http.get<IRespuesta<ICategoria[]>>(`${baseUrl}/Categoria/Buscar`, { params: { nombre } }); }
  insertar(datos: ICategoria): Observable<IRespuesta<ICategoria>> { return this.http.post<IRespuesta<ICategoria>>(`${baseUrl}/Categoria/Insertar`, datos); }
  modificar(datos: ICategoria): Observable<IRespuesta<ICategoria>> { return this.http.put<IRespuesta<ICategoria>>(`${baseUrl}/Categoria/Modificar`, datos); }
  eliminar(id: number): Observable<IRespuesta<boolean>> { return this.http.delete<IRespuesta<boolean>>(`${baseUrl}/Categoria/Eliminar/${id}`); }
}
