import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ICategoria } from '../../model/ICategoria';
import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import { CategoriaService } from '../../services/categoria';
import { FamiliaProductoService } from '../../services/familia-producto';
import { CatalogNavigationCard } from '../catalog-navigation-card/catalog-navigation-card';

/** Muestra categorías activas y limita la búsqueda a la familia seleccionada. */
@Component({
  selector: 'app-categorias-cliente',
  imports: [ReactiveFormsModule, RouterLink, CatalogNavigationCard],
  templateUrl: './categorias-cliente.html',
  styleUrl: '../catalog-navigation-card/catalog-navigation-page.css'
})
export class CategoriasCliente implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly categoriasServicio = inject(CategoriaService);
  private readonly familiasServicio = inject(FamiliaProductoService);
  private readonly cdr = inject(ChangeDetectorRef);

  familiaId = 0;
  familia: IFamiliaProducto | null = null;
  categorias: ICategoria[] = [];
  cargando = true;
  error = '';

  readonly busqueda = this.fb.nonNullable.group({
    texto: ['', [Validators.required, Validators.maxLength(120)]]
  });

  ngOnInit(): void {
    this.route.paramMap.subscribe(parametros => {
      const familiaId = Number(parametros.get('familiaId'));
      if (!Number.isInteger(familiaId) || familiaId <= 0) {
        this.familiaId = 0;
        this.familia = null;
        this.categorias = [];
        this.cargando = false;
        this.error = 'La familia solicitada no es válida.';
        this.cdr.markForCheck();
        return;
      }

      // El identificador de la ruta determina el breadcrumb y la consulta protegida a la API.
      this.familiaId = familiaId;
      this.cargar();
    });
  }

  buscarProductos(): void {
    this.busqueda.markAllAsTouched();
    const texto = this.busqueda.controls.texto.value.trim();
    if (!texto || this.busqueda.invalid || !this.familiaId) return;
    void this.router.navigate(
      ['/productos/familia', this.familiaId, 'buscar'],
      { queryParams: { texto } }
    );
  }

  rutaProductos(categoriaId: number): any[] {
    return ['/productos/familia', this.familiaId, 'categoria', categoriaId];
  }

  private cargar(): void {
    this.cargando = true;
    this.error = '';
    forkJoin({
      familias: this.familiasServicio.listarCliente(),
      categorias: this.categoriasServicio.listarClientePorFamilia(this.familiaId)
    }).pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => {
          this.familia = (respuesta.familias.data ?? [])
            .find(x => x.familiaId === this.familiaId) ?? null;
          if (!this.familia) {
            this.categorias = [];
            this.error = 'La familia solicitada no está disponible.';
            return;
          }
          this.categorias = respuesta.categorias.data ?? [];
        },
        error: err => {
          this.familia = null;
          this.categorias = [];
          this.error = err?.error?.error || 'No fue posible cargar las categorías de la familia.';
        }
      });
  }
}
