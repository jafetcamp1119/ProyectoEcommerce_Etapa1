import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Tarjeta reutilizada por familias y categorías para mantener el mismo patrón visual. */
@Component({
  selector: 'app-catalog-navigation-card',
  imports: [RouterLink],
  templateUrl: './catalog-navigation-card.html',
  styleUrl: './catalog-navigation-card.css'
})
export class CatalogNavigationCard {
  readonly titulo = input.required<string>();
  readonly descripcion = input<string | null>(null);
  readonly textoBoton = input.required<string>();
  readonly ruta = input.required<any[]>();
  readonly imagen = input<string | null>(null);

  iniciales(): string {
    const palabras = this.titulo().trim().split(/\s+/).filter(Boolean);
    if (!palabras.length) return 'LP';
    if (palabras.length === 1) return palabras[0].slice(0, 2).toUpperCase();
    return `${palabras[0][0]}${palabras[1][0]}`.toUpperCase();
  }
}
