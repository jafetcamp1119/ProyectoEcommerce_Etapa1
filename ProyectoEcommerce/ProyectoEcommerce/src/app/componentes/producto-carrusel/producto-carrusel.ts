import { Component, Input, OnChanges } from '@angular/core';
import { IProductoImagen } from '../../model/IProductoImagen';

// muestra las imagenes del producto, escoge la principal y mueve sus miniaturas
@Component({
  selector: 'app-producto-carrusel',
  imports: [],
  templateUrl: './producto-carrusel.html',
  styleUrl: './producto-carrusel.css'
})
export class ProductoCarrusel implements OnChanges {
  @Input() imagenes: IProductoImagen[] = [];
  @Input() imagenPrincipal: IProductoImagen | null = null;
  @Input() textoAlternativo = 'Imagen del producto';
  @Input() cantidadVisible = 3;

  ordenadas: IProductoImagen[] = [];
  seleccionada: IProductoImagen | null = null;
  inicio = 0;
  rotas = new Set<number>();

  // slice toma solamente las miniaturas que caben desde la posicion actual
  get visibles(): IProductoImagen[] {
    return this.ordenadas.slice(this.inicio, this.inicio + this.limiteVisible);
  }

  get limiteVisible(): number { return Math.max(1, this.cantidadVisible); }
  get puedeAnterior(): boolean { return this.inicio > 0; }
  get puedeSiguiente(): boolean { return this.inicio + this.limiteVisible < this.ordenadas.length; }
  get mostrarControles(): boolean { return this.ordenadas.length > this.limiteVisible; }

  // corre cuando llegan imagenes nuevas y vuelve a ordenar y escoger la principal
  ngOnChanges(): void {
    const principal = this.imagenPrincipal?.activo === false ? null : this.imagenPrincipal;
    // Map usa imagenId como llave para quitar duplicados
    const unicas = new Map<number, IProductoImagen>();
    if (principal) unicas.set(principal.imagenId, principal);
    // filter deja imagenes activas y el for las agrega al mapa
    for (const imagen of this.imagenes.filter(x => x.activo)) unicas.set(imagen.imagenId, imagen);
    this.ordenadas = [...unicas.values()].sort((a, b) =>
      Number(b.esPrincipal) - Number(a.esPrincipal) || a.orden - b.orden || a.imagenId - b.imagenId
    );

    const idAnterior = this.seleccionada?.imagenId;
    // intenta conservar la anterior, luego usa principal, primera marcada o primera de la lista
    this.seleccionada = this.ordenadas.find(x => x.imagenId === idAnterior)
      ?? this.ordenadas.find(x => x.imagenId === principal?.imagenId)
      ?? this.ordenadas.find(x => x.esPrincipal)
      ?? this.ordenadas[0]
      ?? null;
    this.inicio = Math.min(this.inicio, Math.max(0, this.ordenadas.length - this.limiteVisible));
    this.rotas.clear();
  }

  // muestra como grande la miniatura que se acaba de escoger
  seleccionar(imagen: IProductoImagen): void { this.seleccionada = imagen; }

  // mueve una posicion hacia atras sin bajar de cero
  anterior(): void { this.inicio = Math.max(0, this.inicio - 1); }

  // mueve una posicion sin pasar el final de la lista
  siguiente(): void {
    this.inicio = Math.min(Math.max(0, this.ordenadas.length - this.limiteVisible), this.inicio + 1);
  }

  // guarda el ID que fallo para mostrar placeholder en vez de una imagen rota
  marcarRota(imagenId: number): void { this.rotas.add(imagenId); }

  // forma hasta dos iniciales a partir del texto alternativo
  iniciales(): string {
    const palabras = this.textoAlternativo
      .trim()
      .split(/\s+/)
      .filter(Boolean);

    if (!palabras.length) return 'PR';
    if (palabras.length === 1) return palabras[0].slice(0, 2).toUpperCase();
    return `${palabras[0][0]}${palabras[1][0]}`.toUpperCase();
  }
}
