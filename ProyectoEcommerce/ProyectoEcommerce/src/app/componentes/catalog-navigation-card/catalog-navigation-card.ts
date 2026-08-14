import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

// esta tarjeta se usa tanto para familias como para categorias
// recibe los textos, la ruta y la imagen para no repetir el mismo diseño en las dos pantallas
@Component({
  selector: 'app-catalog-navigation-card',
  imports: [RouterLink],
  templateUrl: './catalog-navigation-card.html',
  styleUrl: './catalog-navigation-card.css'
})
export class CatalogNavigationCard {
  // input.required obliga a que la pantalla que usa la tarjeta mande este dato
  readonly titulo = input.required<string>();
  readonly descripcion = input<string | null>(null);
  readonly textoBoton = input.required<string>();
  readonly ruta = input.required<any[]>();

  // la imagen si puede venir vacia porque en ese caso se muestran las iniciales
  readonly imagen = input<string | null>(null);

  // guarda la URL que fallo para no intentar mostrarla otra vez en cada deteccion de cambios
  private imagenFallida: string | null = null;

  // aqui revisa que haya una URL y que esa misma URL no haya dado error al cargar
  mostrarImagen(): boolean {
    return !!this.imagen() && this.imagen() !== this.imagenFallida;
  }

  // el evento error del img entra aqui y hace que la tarjeta vuelva al placeholder
  usarPlaceholder(): void {
    this.imagenFallida = this.imagen();
  }

  // toma como maximo las dos primeras palabras para formar el placeholder
  // filter quita espacios vacios y toUpperCase convierte el resultado a mayusculas
  iniciales(): string {
    const palabras = this.titulo().trim().split(/\s+/).filter(Boolean);
    if (!palabras.length) return 'LP';
    if (palabras.length === 1) return palabras[0].slice(0, 2).toUpperCase();
    return `${palabras[0][0]}${palabras[1][0]}`.toUpperCase();
  }
}
