import { Component, Input, input, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnimationSettingsModel, DialogComponent, DialogModule, PositionDataModel, ResizeDirections } from '@syncfusion/ej2-angular-popups';


@Component({
    selector: 'sdk-dialog-crud',
    standalone: true,
    imports: [CommonModule,DialogModule],
    templateUrl: './dialog-crud.component.html',
    styleUrl: './dialog-crud.component.scss',
})
export class DialogCrudComponent {

    private _width: string = '800px';

    @ViewChild('defaultDialog')
    public defaultDialog?: DialogComponent;

    @Input() header: string;

    @Input('width')
    get width(): string {
        return this._width;
    }
    set width(value: string) {
        this._width = value;
    }

    public content: string = 'This is a modal dialog box.';
    public dialogVisible: boolean = false;
    public position: PositionDataModel = { X: 'center', Y: 'center' };
    public target: string = '#modalTarget';

    public animationSettings: AnimationSettingsModel = { effect: 'Zoom' };
    public dialogdragging: Boolean = true;
    public resizeHandleDirection: ResizeDirections[] = ['All'];
    public hide: any;
    public dialogResize: Boolean = true;

    showDialog(): void
    {
        this.dialogVisible = true;
    }

    onClose(): void
    {
        this.dialogVisible = false;
    }
}
