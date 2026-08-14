import { Component } from '@angular/core';
import { FooterComponent } from '@coreui/angular';


// este componente representa el pie de pagina que se comparte
// entre las diferentes pantallas autenticadas
@Component({
  selector: 'app-default-footer',
  imports: [],
  templateUrl: './default-footer.html',
  styleUrl: './default-footer.css',
})
export class DefaultFooter extends FooterComponent {

  // el constructor se ejecuta cuando se crea este componente
  constructor() {

    // super llama al constructor de FooterComponent
    // esto es necesario porque DefaultFooter hereda de ese componente de CoreUI
    super();

  }

}
