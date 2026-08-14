import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import { FamiliaProductoService } from '../../services/familia-producto';
import { CatalogNavigationCard } from '../catalog-navigation-card/catalog-navigation-card';


// esta pantalla muestra las familias activas disponibles para el Cliente
// tambien permite buscar un producto en todo el catalogo
@Component({
  selector: 'app-familias-cliente',
  imports: [
    ReactiveFormsModule,
    CatalogNavigationCard
  ],
  templateUrl: './familias-cliente.html',
  styleUrl: '../catalog-navigation-card/catalog-navigation-page.css'
})
export class FamiliasCliente implements OnInit {

  // herramientas y servicios que se usan dentro de este componente
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(FamiliaProductoService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);


  // aqui se guardan las familias que vienen desde la API
  familias: IFamiliaProducto[] = [];

  // controla si la lista todavia se esta cargando
  cargando = true;

  // guarda cualquier mensaje de error que se tenga que mostrar
  error = '';


  // formulario pequeño para controlar el texto del buscador
  readonly busqueda = this.fb.nonNullable.group({

    texto: [
      '',
      [
        Validators.required,
        Validators.maxLength(120)
      ]
    ]

  });


  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  // aqui se piden las familias disponibles para el Cliente
  ngOnInit(): void {

    this.servicio
      .listarCliente()

      // finalize se ejecuta cuando termina la solicitud
      // tanto si la API respondio bien como si dio error
      .pipe(
        finalize(() => {

          this.cargando = false;

          // le avisa a Angular que vuelva a revisar la pantalla
          this.cdr.markForCheck();

        })
      )

      .subscribe({

        // si la API responde correctamente guarda las familias
        // ?? [] usa un arreglo vacio si data viene null o undefined
        next: respuesta => {
          this.familias = respuesta.data ?? [];
        },

        // si falla intenta mostrar el error que mando la API
        // si no viene ninguno usa este mensaje general
        error: err => {
          this.error =
            err?.error?.error ||
            'No fue posible cargar las familias de producto.';
        }

      });
  }


  // valida el texto escrito y manda a la busqueda global de productos
  buscarProductos(): void {

    // marca el campo como tocado para que se puedan mostrar sus errores
    this.busqueda.markAllAsTouched();


    // agarra el texto escrito y trim quita espacios del inicio y del final
    const texto =
      this.busqueda.controls.texto.value.trim();


    // no continua si no hay texto o si el formulario tiene algun error
    if (
      !texto ||
      this.busqueda.invalid
    ) {
      return;
    }


    // navigate cambia de ruta desde TypeScript
    // queryParams manda el texto dentro de la URL
    // por ejemplo /productos/buscar?texto=baguette
    void this.router.navigate(
      ['/productos/buscar'],
      {
        queryParams: {
          texto
        }
      }
    );

  }

}
