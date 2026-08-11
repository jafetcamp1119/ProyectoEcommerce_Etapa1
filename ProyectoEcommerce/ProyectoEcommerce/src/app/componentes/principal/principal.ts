import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { AutenticacionService } from '../../services/autenticacion';
import { CategoriaService } from '../../services/categoria';
import { FamiliaProductoService } from '../../services/familia-producto';
import { ImpuestoService } from '../../services/impuesto';
import { ProductoService } from '../../services/producto';

/** Muestra el inicio del dashboard y carga indicadores disponibles para el rol actual. */
@Component({
  selector: 'app-principal',
  imports: [RouterLink],
  templateUrl: './principal.html',
  styleUrl: './principal.css'
})
export class Principal implements OnInit {
  readonly autenticacion = inject(AutenticacionService);
  private readonly familias = inject(FamiliaProductoService);
  private readonly categorias = inject(CategoriaService);
  private readonly impuestos = inject(ImpuestoService);
  private readonly productos = inject(ProductoService);
  private readonly cdr = inject(ChangeDetectorRef);
  cargando = true;
  readonly esAdmin = this.autenticacion.esAdministrador();
  resumen = { familias: 0, categorias: 0, impuestos: 0, productos: 0 };

  ngOnInit(): void {
    if (!this.esAdmin) { this.cargando = false; return; }
    forkJoin({
      familias: this.familias.listar().pipe(map(r => (r.data ?? []).filter(x => x.activo).length), catchError(() => of(0))),
      categorias: this.categorias.listar().pipe(map(r => (r.data ?? []).filter(x => x.activo).length), catchError(() => of(0))),
      impuestos: this.impuestos.listar().pipe(map(r => (r.data ?? []).filter(x => x.activo).length), catchError(() => of(0))),
      productos: this.productos.administracion({ activo: true, pagina: 1, tamanoPagina: 25 }).pipe(
        map(r => r.data?.total ?? 0),
        catchError(() => of(0))
      )
    }).subscribe(resumen => { this.resumen = resumen; this.cargando = false; this.cdr.markForCheck(); });
  }
}
