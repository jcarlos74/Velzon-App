import { ComponentFixture, TestBed } from '@angular/core/testing';
import { VelzonSdkComponent } from './velzon-sdk.component';

describe('VelzonSdkComponent', () => {
  let component: VelzonSdkComponent;
  let fixture: ComponentFixture<VelzonSdkComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VelzonSdkComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(VelzonSdkComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
