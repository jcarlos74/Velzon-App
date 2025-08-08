import { RouterModule } from '@angular/router';
import { Component, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'cta-nx-welcome',
  standalone: true,
  imports: [CommonModule,RouterModule],
  template: `
   <h2>Modulo</h2>
  <router-outlet></router-outlet>
  `,
  styles: [],
  encapsulation: ViewEncapsulation.None,
})
export class NxWelcomeComponent {}
