import { Component } from '@angular/core';
import { CatalogNavigationCard } from '../catalog-navigation-card/catalog-navigation-card';

@Component({
  selector: 'app-proveedores-inicio',
  imports: [CatalogNavigationCard],
  templateUrl: './proveedores-inicio.html',
  styleUrls: [
    '../catalog-navigation-card/catalog-navigation-page.css',
    './proveedores.css'
  ]
})
export class ProveedoresInicio {}
