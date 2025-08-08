/* eslint-disable @typescript-eslint/no-empty-function */
import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ColumnModel, GridModule, PagerModule } from '@syncfusion/ej2-angular-grids';
import { TextBoxModule } from '@syncfusion/ej2-angular-inputs';
import { DialogModule } from '@syncfusion/ej2-angular-popups';
import { DataManager, WebMethodAdaptor } from '@syncfusion/ej2-data';
import { BreadcrumbsComponent, ComponentBase, DataGridComponent, DialogCrudComponent, FormCardComponent } from '@velzon.web/velzon-sdk';


@Component({
  selector: 'cta-grupo-acesso',
  standalone: true,
    imports: [CommonModule, BreadcrumbsComponent, ReactiveFormsModule, TextBoxModule, FormCardComponent, GridModule, PagerModule, DataGridComponent, DialogModule, DialogCrudComponent],
  templateUrl: './grupo-acesso.component.html',
  styleUrl: './grupo-acesso.component.scss',

})
export class GrupoAcessoComponent extends ComponentBase implements OnInit
{

    constructor() { super() }

    dataSource?: DataManager;
    columns: ColumnModel[] = [];

    @ViewChild('dialogCrud')
    public dialogCrud?: DialogCrudComponent;

    ngOnInit(): void {
        /**
        * BreadCrumb
        */
        this.breadCrumbItems = [
            { label: 'Segurança' },
            { label: 'Gestão de Acesso' },
            { label: 'Grupos', active: true }
        ];

        this.columns = [
            { field: 'id', headerText: 'ID', isPrimaryKey: true, textAlign: 'Right', width: 90, visible: false },
            { field: 'name', headerText: 'Nome do Grupo', width: 350 }
        ];

        this.dataSource = new DataManager({
                    url: 'api/cta/GrupoAcesso/lista-grupos-acesso',
                    adaptor: new WebMethodAdaptor()
                });
    }

    onAddRecord(args: any)
    {
        this.dialogCrud.showDialog();
    }

}
