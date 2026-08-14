import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DefaultHeader } from './default-header';


// este archivo prueba que el componente DefaultHeader se pueda crear correctamente
describe('DefaultHeader', () => {

  // fixture representa el componente dentro del ambiente de prueba de Angular
  let fixture: ComponentFixture<DefaultHeader>;

  // aqui se guarda la instancia del componente que se va a probar
  let component: DefaultHeader;


  // beforeEach se ejecuta antes de cada prueba dentro de este describe
  beforeEach(async () => {

    // TestBed prepara el ambiente necesario para crear y probar el componente
    await TestBed.configureTestingModule({

      // carga DefaultHeader dentro del ambiente de prueba
      imports: [DefaultHeader]

    })
      // termina de preparar y compilar el componente
      .compileComponents();


    // crea una instancia del componente dentro del test
    fixture = TestBed.createComponent(DefaultHeader);

    // agarra la instancia real de la clase DefaultHeader
    component = fixture.componentInstance;

    // hace que Angular procese el componente y actualice su vista
    fixture.detectChanges();

  });


  // esta prueba revisa que el componente se pueda crear sin errores
  it('should create', () => {

    // toBeTruthy espera que component exista y tenga un valor valido
    expect(component).toBeTruthy();

  });

});
