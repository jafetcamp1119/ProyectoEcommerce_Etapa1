import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { Title } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should use LessPrice as the document title', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect(TestBed.inject(Title).getTitle()).toBe('LessPrice');
  });
});

