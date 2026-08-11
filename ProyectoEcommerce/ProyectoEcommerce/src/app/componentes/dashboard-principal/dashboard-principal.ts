import { Component, CUSTOM_ELEMENTS_SCHEMA, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import {
  AvatarComponent,
  ContainerComponent,
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

/** Estructura el dashboard, arma el menú según el rol y contiene las rutas autenticadas. */
@Component({
  selector: 'app-dashboard-principal',
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
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './dashboard-principal.html',
  styleUrl: './dashboard-principal.css'
})
export class DashboardPrincipal {
  private readonly autenticacion = inject(AutenticacionService);
  public navItems: INavData[] = (this.autenticacion.usuarioActual()?.menuOpciones ?? [])
    .sort((a, b) => a.orden - b.orden)
    .map(opcion => ({
      name: opcion.nombre,
      url: opcion.ruta,
      iconComponent: { name: opcion.icono || 'cil-list' }
    }));
}
