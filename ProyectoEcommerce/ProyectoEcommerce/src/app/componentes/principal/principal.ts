import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';

import { AutenticacionService } from '../../services/autenticacion';
import { CategoriaService } from '../../services/categoria';
import { FamiliaProductoService } from '../../services/familia-producto';
import { ImpuestoService } from '../../services/impuesto';
import { ProductoService } from '../../services/producto';


// esta pantalla muestra el inicio del dashboard
// si entra un Administrador tambien carga los numeros que aparecen en las tarjetas
@Component({
  selector: 'app-principal',
  imports: [RouterLink],
  templateUrl: './principal.html',
  styleUrl: './principal.css'
})
export class Principal implements OnInit {

  // servicio de autenticacion
  // queda publico porque tambien se usa directamente desde el HTML
  readonly autenticacion = inject(AutenticacionService);


  // servicios que se usan para sacar los datos del resumen
  private readonly familias = inject(FamiliaProductoService);
  private readonly categorias = inject(CategoriaService);
  private readonly impuestos = inject(ImpuestoService);
  private readonly productos = inject(ProductoService);

  private readonly cdr = inject(ChangeDetectorRef);


  // controla si todavia se estan cargando los datos del inicio
  cargando = true;


  // guarda si la sesion actual pertenece a un Administrador
  readonly esAdmin =
    this.autenticacion.esAdministrador();


  // aqui se guardan los numeros que se muestran en las tarjetas
  resumen = {
    familias: 0,
    categorias: 0,
    impuestos: 0,
    productos: 0
  };


  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  // si es Administrador carga en paralelo los datos para las tarjetas del inicio
  ngOnInit(): void {

    // los Clientes no necesitan cargar las estadisticas administrativas
    if (!this.esAdmin) {

      this.cargando = false;

      return;
    }


    // forkJoin ejecuta todas estas solicitudes
    // y espera hasta que todas terminen para entregar un solo resultado
    forkJoin({

      // trae las familias
      // map transforma la respuesta y deja solamente la cantidad de familias activas
      familias: this.familias
        .listar()
        .pipe(

          map(
            r =>
              (r.data ?? [])
                .filter(x => x.activo)
                .length
          ),

          // si esta solicitud falla devuelve 0
          // asi no hace fallar todo el forkJoin
          catchError(() => of(0))

        ),


      // trae las categorias y cuenta solamente las activas
      categorias: this.categorias
        .listar()
        .pipe(

          map(
            r =>
              (r.data ?? [])
                .filter(x => x.activo)
                .length
          ),

          catchError(() => of(0))

        ),


      // trae los impuestos y cuenta solamente los activos
      impuestos: this.impuestos
        .listar()
        .pipe(

          map(
            r =>
              (r.data ?? [])
                .filter(x => x.activo)
                .length
          ),

          catchError(() => of(0))

        ),


      // para productos ya se manda activo true a la API
      // entonces se usa directamente el total que viene en la respuesta
      productos: this.productos
        .administracion({
          activo: true,
          pagina: 1,
          tamanoPagina: 25
        })
        .pipe(

          // ?. permite leer total aunque data pueda venir null o undefined
          // ?? 0 usa 0 si no viene ningun total
          map(
            r => r.data?.total ?? 0
          ),

          // si falla la consulta de productos tambien usa 0
          catchError(() => of(0))

        )

    })

      // cuando todas las solicitudes terminan
      // resumen trae los 4 numeros juntos
      .subscribe(resumen => {

        this.resumen = resumen;

        this.cargando = false;

        // le avisa a Angular que vuelva a revisar la pantalla
        this.cdr.markForCheck();

      });
  }

}
