/* eslint-disable @typescript-eslint/no-empty-function */
/* eslint-disable @angular-eslint/no-empty-lifecycle-method */
/* eslint-disable @angular-eslint/component-selector */
import { Component, ContentChild, Input, OnChanges, OnInit, SimpleChanges, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'sdk-form-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './form-card.component.html',
  styleUrl: './form-card.component.scss',
})
export class FormCardComponent implements OnInit,OnChanges {


   @Input() title: string;
   @Input() buttonAddTitle = 'Novo';
   @Input() buttonSaveTitle = 'Salvar';
   @Input() buttonCancelTitle= 'Cancelar'
   @Input() buttonAddEnabled = true;
   @Input() buttonSaveEnabled = false;
   @Input() buttonCancelEnabled= false;
   @Input() hasHeader: boolean = true;
   @Input() hasFooter: boolean = true;

   ngOnInit(): void{

   }
   ngOnChanges(changes: SimpleChanges): void
   {
   }

}
