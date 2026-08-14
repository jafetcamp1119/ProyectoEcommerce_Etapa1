

import { Component, DestroyRef, inject, OnInit } from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Title } from '@angular/platform-browser';

import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';

import { delay, filter, map, tap } from 'rxjs/operators';

import { ColorModeService } from '@coreui/angular';

import { IconSetService } from '@coreui/icons-angular';

import { iconSubset } from './icons/icon-subset';


// este es el componente raiz que prepara titulo, tema e iconos antes de mostrar las rutas
@Component({

  selector: 'app-root',

  imports: [RouterOutlet],

  templateUrl: './app.html',

  styleUrl: './app.css'

})

export class App implements OnInit {

  protected title = 'LessPrice';

  readonly #destroyRef: DestroyRef = inject(DestroyRef);

  readonly #activatedRoute: ActivatedRoute = inject(ActivatedRoute);

  readonly #router = inject(Router);

  readonly #titleService = inject(Title);

  readonly #colorModeService = inject(ColorModeService);

  readonly #iconSetService = inject(IconSetService);

  constructor() {

    this.#titleService.setTitle(this.title);

    // carga una sola vez los iconos que usa CoreUI en el menu

    this.#iconSetService.icons = { ...iconSubset };

    this.#colorModeService.localStorageItemName.set('coreui-free-angular-admin-template-theme-default');

    this.#colorModeService.eventName.set('ColorSchemeChange');

  }

  // escucha cambios de ruta y tambien permite escoger tema con ?theme=dark, light o auto
  ngOnInit(): void {

    this.#router.events.pipe(

      takeUntilDestroyed(this.#destroyRef)

    ).subscribe((evt) => {

      if (!(evt instanceof NavigationEnd)) {

        return;

      }

    });

    this.#activatedRoute.queryParams

      .pipe(

        delay(1),

        // map transforma el parametro, filter deja solo temas validos y tap aplica el valor
        map(params => <string>params['theme']?.match(/^[A-Za-z0-9\s]+/)?.[0]),

        filter(theme => ['dark', 'light', 'auto'].includes(theme)),

        tap(theme => {

          this.#colorModeService.colorMode.set(theme);

        }),

        takeUntilDestroyed(this.#destroyRef)

      )

      // subscribe pone en marcha el observable y takeUntilDestroyed lo cierra con el componente
      .subscribe();

  }

}


