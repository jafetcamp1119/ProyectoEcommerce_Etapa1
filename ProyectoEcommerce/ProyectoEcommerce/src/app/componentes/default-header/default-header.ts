import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import {
  ColorModeService,
  ContainerComponent,
  DropdownComponent,
  DropdownItemDirective,
  DropdownMenuDirective,
  DropdownToggleDirective,
  HeaderComponent,
  HeaderNavComponent,
  HeaderTogglerDirective,
  SidebarToggleDirective
} from '@coreui/angular';

import { IconDirective } from '@coreui/icons-angular';

import { AutenticacionService } from '../../services/autenticacion';
import { CarritoService } from '../../services/carrito';


// este componente representa el encabezado principal de las pantallas autenticadas
// muestra el usuario actual el carrito el selector de tema y permite cerrar sesion
@Component({
  selector: 'app-default-header',

  // aqui se cargan los componentes y directivas que usa el HTML del encabezado
  imports: [
    ContainerComponent,
    HeaderTogglerDirective,
    SidebarToggleDirective,
    IconDirective,
    HeaderNavComponent,
    NgTemplateOutlet,
    DropdownComponent,
    DropdownToggleDirective,
    DropdownMenuDirective,
    DropdownItemDirective,
    RouterLink
  ],

  templateUrl: './default-header.html',
  styleUrl: './default-header.css',
})
export class DefaultHeader extends HeaderComponent {

  // servicio de CoreUI que controla el tema claro oscuro o automatico
  // el # hace que estas propiedades sean privadas dentro de la clase
  readonly #colorModeService = inject(ColorModeService);

  // Router permite cambiar de ruta desde TypeScript
  readonly #router = inject(Router);

  // servicio que guarda y consulta la informacion del usuario autenticado
  readonly autenticacion = inject(AutenticacionService);

  // servicio que maneja el carrito y su contador
  readonly carrito = inject(CarritoService);


  // toma directamente el modo de color que maneja CoreUI
  readonly colorMode = this.#colorModeService.colorMode;


  // opciones disponibles dentro del selector de tema
  // cada una tiene su nombre interno texto e icono
  readonly colorModes = [
    {
      name: 'light',
      text: 'Light',
      icon: 'cilSun'
    },
    {
      name: 'dark',
      text: 'Dark',
      icon: 'cilMoon'
    },
    {
      name: 'auto',
      text: 'Auto',
      icon: 'cilContrast'
    }
  ];


  // computed calcula automaticamente cual icono debe mostrar el boton del tema
  // si colorMode cambia Angular vuelve a calcular este valor
  readonly icons = computed(() => {

    // obtiene el modo que esta activo actualmente
    const currentMode = this.colorMode();

    // find busca dentro de colorModes la opcion que tenga el mismo nombre
    // ?.icon intenta sacar su icono y ?? usa cilSun si no encuentra ninguno
    return this.colorModes
      .find(mode => mode.name === currentMode)
      ?.icon ?? 'cilSun';

  });


  // el constructor se ejecuta cuando Angular crea este encabezado
  constructor() {

    // DefaultHeader hereda de HeaderComponent
    // super llama primero al constructor del HeaderComponent de CoreUI
    super();


    // el carrito solamente necesita cargarse si el usuario actual es Cliente
    if (this.autenticacion.esCliente()) {

      // queueMicrotask deja esta accion para despues de terminar el ciclo actual
      // esto evita cambiar una signal mientras Angular todavia esta construyendo la vista
      queueMicrotask(() =>
        this.carrito
          .obtenerActual()
          .subscribe({

            // si falla esta carga inicial no muestra ningun error desde el header
            error: () => undefined

          })
      );

    }
  }


  // cierra la sesion actual y devuelve a la pantalla de login
  cerrarSesion(): void {

    // limpia el contador del carrito que se muestra en el encabezado
    this.carrito.reiniciarIndicador();

    // borra la informacion de autenticacion y la sesion actual
    this.autenticacion.cerrarSesion();

    // navigate cambia la ruta hacia la pantalla de autenticacion
    void this.#router.navigate(['/auth']);

  }


  // input permite recibir desde el componente padre cual sidebar debe controlar este header
  // si no se manda ningun valor usa sidebar1 por defecto
  sidebarId = input('sidebar1');

}
