import { Component, ContentChild, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'sdk-card-footer',
  standalone: true,
  exportAs: 'cardFooter',
  imports: [CommonModule],
  templateUrl: './card-footer.component.html',
  styleUrl: './card-footer.component.scss',
})
export class CardFooterComponent {

    @ContentChild('footerTemplate') footerTemplate: TemplateRef<{}>;
}
