import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import {
  provideRouter,
  withEnabledBlockingInitialNavigation,
  withHashLocation,
  withInMemoryScrolling,
  withRouterConfig,
  withViewTransitions
} from '@angular/router';
import { IconSetService } from '@coreui/icons-angular';
import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';

// aqui se registran las piezas globales que Angular necesita antes de mostrar la app
export const appConfig: ApplicationConfig = {
  providers: [
    // escucha errores globales del navegador y actualiza vistas sin depender de zone.js
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),

    // todas las llamadas HTTP pasan por el interceptor que agrega el JWT
    provideHttpClient(withInterceptors([authInterceptor])),

    // estas opciones cargan las rutas, restauran el scroll y usan # en la URL
    provideRouter(
      routes,
      withRouterConfig({ onSameUrlNavigation: 'reload' }),
      withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' }),
      withEnabledBlockingInitialNavigation(),
      withViewTransitions(),
      withHashLocation()
    ),
    // deja disponibles los iconos de CoreUI y las animaciones usadas por los componentes
    IconSetService,
    provideAnimationsAsync()
  ]
};
