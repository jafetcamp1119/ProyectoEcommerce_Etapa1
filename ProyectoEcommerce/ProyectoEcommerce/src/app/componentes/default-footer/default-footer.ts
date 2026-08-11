import { Component } from '@angular/core';
import { FooterComponent } from '@coreui/angular';

/** Pie de página comercial compartido por las pantallas autenticadas. */
@Component({
  selector: 'app-default-footer',
  imports: [],
  templateUrl: './default-footer.html',
  styleUrl: './default-footer.css',
})
export class DefaultFooter  extends FooterComponent {

  constructor () { 
  super();
}

}
