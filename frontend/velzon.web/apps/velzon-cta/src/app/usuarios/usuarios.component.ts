import { DataManager, WebMethodAdaptor } from '@syncfusion/ej2-data';
/* eslint-disable @typescript-eslint/no-empty-function */
import { Component, OnInit, ViewChild,ElementRef  } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BreadcrumbsComponent, ComponentBase, DataGridComponent, DynamicFormBuilder, FormCardComponent, FormFieldOptions, SmaCidade } from '@velzon.web/velzon-sdk';
import { Column, ColumnModel, GridModule, PagerModule } from '@syncfusion/ej2-angular-grids';
import { AnimationSettingsModel, DialogComponent, DialogModule, PositionDataModel, ResizeDirections } from '@syncfusion/ej2-angular-popups';
import { FormBuilder, FormGroup,ReactiveFormsModule } from '@angular/forms';
import { DialogCrudComponent } from "@velzon.web/velzon-sdk";
import { TextBoxModule } from '@syncfusion/ej2-angular-inputs';

@Component({
  selector: 'cta-usuarios',
  standalone: true,
    imports: [CommonModule, BreadcrumbsComponent,ReactiveFormsModule,TextBoxModule, FormCardComponent, GridModule, PagerModule, DataGridComponent, DialogModule, DialogCrudComponent],
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.scss',
})
export class UsuariosComponent extends ComponentBase implements OnInit{


    dataSource?: DataManager;
    columns: ColumnModel[] = [];

    @ViewChild('dialogCrud')
    public dialogCrud?: DialogCrudComponent;

    form!: FormGroup;
    metadata!: Record<string, FormFieldOptions>;
    fieldKeys: string[] = [];
    width: string = '60%';
    header: string = 'Cadastro de Cidades';

    constructor(private fb: FormBuilder)
    {

        super();

        const builder = new DynamicFormBuilder(SmaCidade, this.fb);

        this.form = builder.buildForm();
        this.metadata = builder.getMetadata();
        this.fieldKeys = Object.keys(this.metadata);
    }

    ngOnInit(): void {

      this.breadCrumbItems = [
        { label: 'Segurança' },
        { label: 'Gestão de Acesso' },
        { label: 'Usuários', active: true }
      ];

        this.columns = [
            { field: 'idCidade', headerText: 'ID', isPrimaryKey: true, textAlign: 'Right', width: 90, visible: false },
            { field: 'nomeCidade', headerText: 'Nome', width: 250 },
            { field: 'idUf', headerText: 'Código UF', width: 90 },
            { field: 'capital', headerText: 'Captial', textAlign: 'Center', width: 40 }
        ];

        this.dataSource = new DataManager({
            url: 'api/velzon/teste/lista-cidades3',
            adaptor: new WebMethodAdaptor()
        });
    

    }

    onSubmit(): void
    {
        if (this.form.valid) {
            console.log(this.form.value);
            this.dialogCrud.hide();
        }
    }

    onAddRecord(args: any)
    {
        this.dialogCrud.showDialog();
    }

    onOpen(): void
    {
    // Action to perform when dialog opens
    }
}
