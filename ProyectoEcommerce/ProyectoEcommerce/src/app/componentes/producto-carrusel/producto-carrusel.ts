import { Component, Input, OnChanges } from '@angular/core';
import { IProductoImagen } from '../../model/IProductoImagen';

/** Presenta las imágenes disponibles del producto y controla la selección del carrusel. */
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

  get visibles(): IProductoImagen[] {
    return this.ordenadas.slice(this.inicio, this.inicio + this.limiteVisible);
  }

  get limiteVisible(): number { return Math.max(1, this.cantidadVisible); }
  get puedeAnterior(): boolean { return this.inicio > 0; }
  get puedeSiguiente(): boolean { return this.inicio + this.limiteVisible < this.ordenadas.length; }
  get mostrarControles(): boolean { return this.ordenadas.length > this.limiteVisible; }

  ngOnChanges(): void {
    const principal = this.imagenPrincipal?.activo === false ? null : this.imagenPrincipal;
    const unicas = new Map<number, IProductoImagen>();
    if (principal) unicas.set(principal.imagenId, principal);
    for (const imagen of this.imagenes.filter(x => x.activo)) unicas.set(imagen.imagenId, imagen);
    this.ordenadas = [...unicas.values()].sort((a, b) =>
      Number(b.esPrincipal) - Number(a.esPrincipal) || a.orden - b.orden || a.imagenId - b.imagenId
    );

    const idAnterior = this.seleccionada?.imagenId;
    this.seleccionada = this.ordenadas.find(x => x.imagenId === idAnterior)
      ?? this.ordenadas.find(x => x.imagenId === principal?.imagenId)
      ?? this.ordenadas.find(x => x.esPrincipal)
      ?? this.ordenadas[0]
      ?? null;
    this.inicio = Math.min(this.inicio, Math.max(0, this.ordenadas.length - this.limiteVisible));
    this.rotas.clear();
  }

  seleccionar(imagen: IProductoImagen): void { this.seleccionada = imagen; }

  anterior(): void { this.inicio = Math.max(0, this.inicio - 1); }

  siguiente(): void {
    this.inicio = Math.min(Math.max(0, this.ordenadas.length - this.limiteVisible), this.inicio + 1);
  }

  marcarRota(imagenId: number): void { this.rotas.add(imagenId); }

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
