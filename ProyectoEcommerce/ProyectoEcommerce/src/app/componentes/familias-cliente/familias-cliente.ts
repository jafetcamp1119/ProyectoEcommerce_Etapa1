import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { IFamiliaProducto } from '../../model/IFamiliaProducto';
import { FamiliaProductoService } from '../../services/familia-producto';
import { CatalogNavigationCard } from '../catalog-navigation-card/catalog-navigation-card';

/** Muestra familias activas y ofrece búsqueda de productos en todo el catálogo. */
@Component({
  selector: 'app-familias-cliente',
  imports: [ReactiveFormsModule, CatalogNavigationCard],
  templateUrl: './familias-cliente.html',
  styleUrl: '../catalog-navigation-card/catalog-navigation-page.css'
})
export class FamiliasCliente implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(FamiliaProductoService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  familias: IFamiliaProducto[] = [];
  cargando = true;
  error = '';

  readonly busqueda = this.fb.nonNullable.group({
    texto: ['', [Validators.required, Validators.maxLength(120)]]
  });

  ngOnInit(): void {
    this.servicio.listarCliente()
      .pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: respuesta => this.familias = respuesta.data ?? [],
        error: err => this.error = err?.error?.error || 'No fue posible cargar las familias de producto.'
      });
  }

  buscarProductos(): void {
    this.busqueda.markAllAsTouched();
    const texto = this.busqueda.controls.texto.value.trim();
    if (!texto || this.busqueda.invalid) return;
    void this.router.navigate(['/productos/buscar'], { queryParams: { texto } });
  }
}
