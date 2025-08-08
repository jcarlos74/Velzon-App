import { DataGridService } from 'libs/velzon-sdk/src/lib/core/services/data-grid.service';
import { AsyncPipe, CommonModule } from "@angular/common";
import { AfterViewInit, Component, EventEmitter, OnInit, Output, Query, ViewChild } from "@angular/core";
import { DatagridToolbarComponent, Localization, OperationResult, SmaCidade } from "@velzon.web/velzon-sdk";
import { GrupoAcessoService } from "../services/grupo-acesso.http-service";
import { FilterModel } from "libs/velzon-sdk/src/lib/core/models/filter-model";
import { DataStateChangeEventArgs, GridComponent,  GridModule,  ToolbarItems, ToolbarService } from '@syncfusion/ej2-angular-grids'
import { PageService, SortService, FilterService, GroupService,ReorderService } from '@syncfusion/ej2-angular-grids'
import { PageSettingsModel } from '@syncfusion/ej2-angular-grids';
import { map, Observable } from "rxjs";
import { GridService } from "../services/grupo-acesso.service";
import { DataManager, UrlAdaptor, WebMethodAdaptor } from '@syncfusion/ej2-data';

Localization.ptBR();

@Component({
  selector: 'cta-grupo-acesso-list',
  standalone: true,
  imports: [CommonModule, DatagridToolbarComponent,GridModule ],
  template:  ` <!--Toolbar Grid-->
          <div id="areaGrid" > <!--
          <div id="areaGrid" > -->
             <!-- <smart-toolbar-grid TitleButtonNew="Nova Empresa"
                  (onNewRegistry)="inserirNovo($event)"
                  (onRefreshData)="atualizarDados()"
                  (onToggleFilterRow)="alternaLinhaDeFiltros()"
                  (onTogglePanelGroup)="alternaPainelDeGrupo()"
                  (onGlobalFilterChanged)="globalfilterChanged($event)"
                  (onExportToFile)="exportGridToFile()"
              ></smart-toolbar-grid>-->
               <!--toolbar DataGrid-->
              <div class="row">

                  <sdk-datagrid-toolbar >
                  </sdk-datagrid-toolbar>

              </div>
              <!--conteúdo do Grid-->
              <div class="row container-grid">


                 <ejs-grid #grid [dataSource]='data' (click)="pageClick($event)" locale="pt-BR" allowPaging= 'true' allowReordering='true' [pageSettings]='pageOptions' allowResizing = 'true' showColumnMenu = 'true' allowSorting= 'true'  [showColumnChooser]='true' [allowGrouping]='false'   height="320">
                    <e-columns>
                        <e-column field='idCidade' headerText='ID' isPrimaryKey=true textAlign='Right' width=90></e-column>
                        <e-column field='nomeCidade' headerText='Nome Cidade' width=120></e-column>
                        <e-column field='idUf' headerText='Código UF' textAlign='Right' format='C2' width=90></e-column>
                        <e-column field='capital' headerText='Captial' textAlign='Right' format='yMd' width=120 type="bool"></e-column>
                    </e-columns>
                    <ng-template #emptyRecordTemplate>
                        <div class='emptyRecordTemplate'>
                          <!--  <img [src]="imageSrc()" class="e-emptyRecord" alt="No record"> -->
                            <span>Não há dados disponíveis para exibição no momento.</span>
                        </div>
                    </ng-template>
                </ejs-grid>

            </div>`,
  styleUrl: './grupo-acesso.component.scss',
  providers: [PageService,
               SortService,
               FilterService,
               GroupService,
               ToolbarService,
               ReorderService,DataGridService ]
})
export class GrupoAcessoListComponent implements OnInit
{

     @ViewChild('grid')
     public grid?: GridComponent;

    @Output() public onNewRegistro: EventEmitter<any> = new EventEmitter<any>();
  //  @Output() public onEditRegistro: EventEmitter<OnEventArgs> = new EventEmitter<OnEventArgs>();

    public data?: DataManager;
    public toolbar?: ToolbarItems[];


    public dataset?: Observable<DataStateChangeEventArgs>;
    public state?: DataStateChangeEventArgs;
    public pageOptions?: object;
    public query?: Query;
    public pageSettings?: PageSettingsModel;


    currentPage: number=1;
    qtdeRows: number=0;

     loadOptions        = {} as FilterModel;

    /*columns = "[
                          { field:'idCidade', header:'ID'},
                          { field:'nomeCidade',header:'Nome Cidade'},
                          { field:'idUf',header:'Código UF'},
                          { field:'capital', header:'Captial'},
                        ]";*/

    constructor(private service: DataGridService)
    {
       // this.dataset = service;
    }

     public dataStateChange(state: DataStateChangeEventArgs): void {

         this.service.execute(state,this.query,"lista-cidades");
         this.currentPage = this.grid.pagerModule.pagerObj.currentPage;
    }

    pageClick(event: any){
     // this.currentPage = this.grid.pagerModule.pagerObj.currentPage;
    }

    ngOnInit(): void
    {
       try
        {

             this.pageOptions = { enableQueryString: true, pageSize: 50 ,pageSizes : [10,20,50,100] , pageCount : 6  }; //, pageCount: 4 };
             const state = { skip: 0, take: 10 };
           //  this.query = new Query().addParams('Syncfusion_Angular_Grid', 'true');
            // this.service.execute(state, this.query,"lista-cidades");

            this.data =  new DataManager({
                url: 'api/velzon/teste/lista-cidades3',
                adaptor: new WebMethodAdaptor(),
                //crossDomain: true,
              });

       /*   this.dataset =  this.service.loadList("lista-cidades",this.loadOptions).subscribe((response: OperationResult<SmaCidade[]>) =>
             {
                //this.dataGrid.GridSettings(this.dataGridSettings);
                const result = response.data;
                const count = response.totalRows;

                  return { result, count } as DataStateChangeEventArgs;

                //this.dataset.items = result.data;
               // this.dataset.count = result.totalRows;


               // this.dataGridPag.pageCount = Math.ceil(result.totalRows / this.loadOptions.rowsPerPage);
              //  this.dataGridPag.processing = false;
               // this.dataGridPag.Refresh();
             });

             // this.data = new DataManager({
            //             url: '/api/velzon/teste/lista-cidades3', // Replace your hosted link
           //              adaptor: new WebMethodAdaptor()
           //   });

             //  this.toolbar = ['Search'];

        */
        }
        catch (ex)
        {
          let result = ex.message;
        }
    }
}
