/* eslint-disable @typescript-eslint/no-empty-function */
/* eslint-disable @angular-eslint/no-empty-lifecycle-method */
/* eslint-disable @angular-eslint/component-selector */
import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'sdk-breadcrumbs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './breadcrumbs.component.html',
  styleUrl: './breadcrumbs.component.scss',
})
export class BreadcrumbsComponent  implements OnInit {

    @Input() title: string | undefined;
    @Input()
    breadcrumbItems!: Array<{
      active?: boolean;
      label?: string;
    }>;

    Item!: Array<{
      label?: string;
    }>;

    constructor() {}

    ngOnInit(): void {
    }

}
