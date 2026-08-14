import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DefaultFooter } from './default-footer';


// este archivo prueba que el componente DefaultFooter se pueda crear correctamente
describe('DefaultFooter', () => {

  // fixture representa el componente dentro del ambiente de prueba de Angular
  let fixture: ComponentFixture<DefaultFooter>;

  // aqui se guarda la instancia del componente que se va a probar
  let component: DefaultFooter;


  // beforeEach se ejecuta antes de cada prueba que haya dentro de este describe
  beforeEach(async () => {

    // TestBed prepara el ambiente necesario para poder probar el componente
    await TestBed.configureTestingModule({

      // carga el componente DefaultFooter dentro de la prueba
      imports: [DefaultFooter]

    })
      // termina de preparar y compilar el componente para poder usarlo en el test
      .compileComponents();


    // crea el componente dentro del ambiente de prueba
    fixture = TestBed.createComponent(DefaultFooter);

    // saca la instancia real de la clase DefaultFooter
    component = fixture.componentInstance;

    // hace que Angular procese el componente y actualice su vista
    fixture.detectChanges();

  });


  // esta prueba revisa solamente que el componente se haya creado sin errores
  it('should create', () => {

    // toBeTruthy espera que component exista y tenga un valor valido
    expect(component).toBeTruthy();

  });

});
