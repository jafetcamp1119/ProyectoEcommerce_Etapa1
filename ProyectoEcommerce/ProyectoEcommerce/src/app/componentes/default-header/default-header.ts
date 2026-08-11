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

/** Encabezado con navegación, usuario actual, indicador del carrito y cierre de sesión. */
@Component({
  selector: 'app-default-header',
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
  readonly #colorModeService = inject(ColorModeService);
  readonly #router = inject(Router);
  readonly autenticacion = inject(AutenticacionService);
  readonly carrito = inject(CarritoService);
  readonly colorMode = this.#colorModeService.colorMode;

  readonly colorModes = [
    { name: 'light', text: 'Light', icon: 'cilSun' },
    { name: 'dark', text: 'Dark', icon: 'cilMoon' },
    { name: 'auto', text: 'Auto', icon: 'cilContrast' }
  ];

  readonly icons = computed(() => {
    const currentMode = this.colorMode();
    return this.colorModes.find(mode => mode.name === currentMode)?.icon ?? 'cilSun';
  });

  constructor() {
    super();
    if (this.autenticacion.esCliente()) {
      queueMicrotask(() => this.carrito.obtenerActual().subscribe({ error: () => undefined }));
    }
  }

  cerrarSesion(): void {
    this.carrito.reiniciarIndicador();
    this.autenticacion.cerrarSesion();
    void this.#router.navigate(['/auth']);
  }

  sidebarId = input('sidebar1');
}
