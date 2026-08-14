import { Component, CUSTOM_ELEMENTS_SCHEMA, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import {
  AvatarComponent,ContainerComponent,
  ShadowOnScrollDirective,
  SidebarBrandComponent,
  SidebarComponent,
  SidebarFooterComponent,
  SidebarHeaderComponent,
  SidebarNavComponent,
  SidebarToggleDirective,
  SidebarTogglerDirective,
  type INavData
} from '@coreui/angular';
import { NgScrollbar } from 'ngx-scrollbar';

import { DefaultFooter } from '../default-footer/default-footer';
import { DefaultHeader } from '../default-header/default-header';
import { AutenticacionService } from '../../services/autenticacion';


// esta pantalla contiene la estructura principal del dashboard despues del login
// tambien arma las opciones del menu dependiendo de los permisos del usuario actual
@Component({
  selector: 'app-dashboard-principal',

  // aqui se cargan los componentes y directivas que utiliza el HTML del dashboard
  imports: [
    AvatarComponent,
    ContainerComponent,
    DefaultFooter,
    DefaultHeader,
    NgScrollbar,
    RouterLink,
    RouterOutlet,
    ShadowOnScrollDirective,
    SidebarBrandComponent,
    SidebarComponent,
    SidebarFooterComponent,
    SidebarHeaderComponent,
    SidebarNavComponent,
    SidebarToggleDirective,
    SidebarTogglerDirective
  ],

  // permite usar elementos personalizados que Angular no reconoce directamente
  // esto se usa porque la plantilla trabaja con componentes de CoreUI
  schemas: [CUSTOM_ELEMENTS_SCHEMA],

  templateUrl: './dashboard-principal.html',
  styleUrl: './dashboard-principal.css'
})
export class DashboardPrincipal {

  // servicio que permite consultar la informacion del usuario que inicio sesion
  private readonly autenticacion = inject(AutenticacionService);


  // navItems contiene las opciones que CoreUI va a mostrar dentro del menu lateral
  // INavData define la forma que debe tener cada opcion del menu
  public navItems: INavData[] = (
    this.autenticacion.usuarioActual()?.menuOpciones ?? []
  )

    // sort acomoda las opciones usando el numero guardado en orden
    // si una opcion tiene un numero menor aparece primero en el menu
    .sort((a, b) => a.orden - b.orden)

    // map recorre las opciones del usuario y convierte cada una
    // al formato que necesita el menu de CoreUI
    .map(opcion => ({

      // texto que se muestra en el menu
      name: opcion.nombre,

      // ruta a la que manda cuando la persona presiona la opcion
      url: opcion.ruta,

      // usa el icono que tenga guardado la opcion
      // si no tiene ninguno usa cil-list como icono por defecto
      iconComponent: {
        name: opcion.icono || 'cil-list'
      }

    }));

}
