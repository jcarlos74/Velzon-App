import { Component, ElementRef, ViewChild } from "@angular/core";

@Component({template: ''})
export class ComponentBase {

    breadCrumbItems!: Array<object>;

     @ViewChild('frm') frm: ElementRef;

    showAreaGrid:string = "block";
    showAreaForm:string = "none";


    showForm(value: boolean)
    {
       if ( value )
       {
          this.showAreaForm = "block";
          this.showAreaGrid = "none";
       }
       else
       {
          this.showAreaForm = "none";
          this.showAreaGrid = "block";
       }

    }

    setFocus(idControl: string)
    {
        const ele = this.frm.nativeElement[idControl];
        if (ele)
        {
          ele.focus();
        }
    }

    
}
