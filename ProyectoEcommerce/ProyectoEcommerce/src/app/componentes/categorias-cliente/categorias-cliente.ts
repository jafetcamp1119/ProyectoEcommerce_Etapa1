import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ICategoria } from '../../model/ICategoria';
import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import { CategoriaService } from '../../services/categoria';
import { FamiliaProductoService } from '../../services/familia-producto';
import { CatalogNavigationCard } from '../catalog-navigation-card/catalog-navigation-card';


// esta pantalla muestra las categorias activas de una familia
// tambien permite buscar productos sin salirse de la familia que viene en la URL
@Component({
  selector: 'app-categorias-cliente',
  imports: [ReactiveFormsModule, RouterLink, CatalogNavigationCard],
  templateUrl: './categorias-cliente.html',
  styleUrl: '../catalog-navigation-card/catalog-navigation-page.css'
})
export class CategoriasCliente implements OnInit {

  // inject permite usar estas herramientas y servicios dentro del componente
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly categoriasServicio = inject(CategoriaService);
  private readonly familiasServicio = inject(FamiliaProductoService);
  private readonly cdr = inject(ChangeDetectorRef);


  // familiaId guarda el id que viene en la ruta
  familiaId = 0;

  // aqui se guarda la familia que estamos viendo actualmente
  familia: IFamiliaProducto | null = null;

  // lista de categorias activas que pertenecen a esa familia
  categorias: ICategoria[] = [];

  cargando = true;
  error = '';


  // formulario pequeño que controla el texto de busqueda
  // required obliga a escribir algo y maxLength limita el texto a 120 caracteres
  readonly busqueda = this.fb.nonNullable.group({
    texto: ['', [Validators.required, Validators.maxLength(120)]]
  });


  // ngOnInit se ejecuta cuando se abre el componente
  // paramMap escucha los parametros de la URL incluso si cambian sin destruir la pantalla
  ngOnInit(): void {

    this.route.paramMap.subscribe(parametros => {

      // busca familiaId dentro de la ruta y lo convierte a numero
      const familiaId = Number(
        parametros.get('familiaId')
      );

      // Number.isInteger revisa que realmente sea un numero entero
      // tambien evitamos 0 o numeros negativos porque no representan una familia valida
      if (
        !Number.isInteger(familiaId) ||
        familiaId <= 0
      ) {

        // limpia cualquier informacion anterior para no dejar datos viejos en pantalla
        this.familiaId = 0;
        this.familia = null;
        this.categorias = [];
        this.cargando = false;

        this.error = 'La familia solicitada no es válida.';

        // avisa a Angular que debe revisar la vista porque cambiaron varias variables
        this.cdr.markForCheck();

        return;
      }

      // guarda el id correcto de la ruta
      // este id se usa despues para buscar la familia y sus categorias
      this.familiaId = familiaId;

      this.cargar();
    });
  }


  // toma el texto del buscador y manda a la pantalla de resultados
  // manteniendo la familia que estamos viendo actualmente
  buscarProductos(): void {

    // hace que se muestren los errores del formulario si los hubiera
    this.busqueda.markAllAsTouched();

    // trim quita espacios al inicio y al final
    const texto = this.busqueda.controls.texto.value.trim();

    // no continua si no hay texto si el formulario esta malo o si no tenemos familia
    if (
      !texto ||
      this.busqueda.invalid ||
      !this.familiaId
    ) {
      return;
    }

    // navigate cambia de pagina desde TypeScript
    // primero se arma la ruta y queryParams agrega el texto despues del signo ?
    // por ejemplo /productos/familia/2/buscar?texto=pan
    void this.router.navigate(
      [
        '/productos/familia',
        this.familiaId,
        'buscar'
      ],
      {
        queryParams: {
          texto
        }
      }
    );
  }


  // arma la ruta completa para entrar a los productos de una categoria
  rutaProductos(categoriaId: number): any[] {

    // devuelve la ruta como arreglo porque Angular Router acepta este formato
    return [
      '/productos/familia',
      this.familiaId,
      'categoria',
      categoriaId
    ];
  }


  // trae al mismo tiempo la familia actual y sus categorias
  private cargar(): void {

    this.cargando = true;
    this.error = '';

    // forkJoin ejecuta las 2 solicitudes y espera que ambas terminen bien
    // hasta que las 2 responden entra al next
    forkJoin({

      // trae las familias que puede ver el Cliente
      familias: this.familiasServicio.listarCliente(),

      // trae solamente las categorias activas de la familia actual
      categorias: this.categoriasServicio
        .listarClientePorFamilia(this.familiaId)

    })

      // finalize se ejecuta tanto si las solicitudes salieron bien como si fallaron
      .pipe(
        finalize(() => {

          this.cargando = false;

          // hace que Angular actualice la pantalla despues de terminar las solicitudes
          this.cdr.markForCheck();
        })
      )

      .subscribe({

        // respuesta contiene el resultado de familias y categorias
        next: respuesta => {

          // ?? [] usa un arreglo vacio si la API no mando datos
          // find busca la familia que tenga el mismo id que viene en la URL
          // si no la encuentra queda en null
          this.familia = (respuesta.familias.data ?? [])
            .find(
              x => x.familiaId === this.familiaId
            ) ?? null;

          // si la familia ya no existe o no esta disponible para Cliente
          // no dejamos mostrar categorias sueltas
          if (!this.familia) {

            this.categorias = [];

            this.error =
              'La familia solicitada no está disponible.';

            return;
          }

          // si la familia si existe guarda sus categorias
          // si la API no mando ninguna usa una lista vacia
          this.categorias =
            respuesta.categorias.data ?? [];
        },

        // si cualquiera de las 2 solicitudes falla forkJoin entra aqui
        error: err => {

          this.familia = null;
          this.categorias = [];

          // primero intenta usar el mensaje que mando la API
          // y si no viene ninguno usa este mensaje general
          this.error =
            err?.error?.error ||
            'No fue posible cargar las categorías de la familia.';
        }

      });
  }

}
