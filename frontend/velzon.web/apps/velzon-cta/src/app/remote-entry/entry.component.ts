import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NxWelcomeComponent } from './nx-welcome.component';

@Component({
  standalone: true,
  imports: [CommonModule, NxWelcomeComponent],
  selector: 'cta-velzon-cta-entry',
  template: `<cta-nx-welcome></cta-nx-welcome>`,
})
export class RemoteEntryComponent {}
