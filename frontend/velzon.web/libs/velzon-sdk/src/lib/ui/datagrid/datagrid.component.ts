import { ColumnMenuService,ResizeService, ColumnChooserService, ColumnModel, DeleteEventArgs,PdfExportProperties, ExcelExportProperties, ExcelExportService, FilterService, FilterSettingsModel, GridComponent, GridModule, GroupService, GroupSettingsModel, PageService, PageSettingsModel, PdfExportService, ReorderService, SaveEventArgs, SortService, FreezeService, ToolbarService, ToolbarItems, SearchEventArgs, AddEventArgs, QueryCellInfoEventArgs, Column, SearchSettingsModel } from '@syncfusion/ej2-angular-grids';
import { CommonModule } from "@angular/common";
import { AfterViewInit, Component, ElementRef, EventEmitter, Input,  OnInit, Output, Query,  TemplateRef, ViewChild } from "@angular/core";
import { Localization } from "../../core/localization";
import { DataManager } from '@syncfusion/ej2-data';
import { DropDownButtonAllModule, ItemModel } from '@syncfusion/ej2-angular-splitbuttons';


Localization.ptBR();

interface ColumnData{
    [key: string]: number| string;
    OrderID:number,
    Freight:number,
    CustomerID:string,
    ShipCity:string,
    ShipName:string,
    ShipCountry:string,
    ShipPostalCode:number

  }

@Component({
  selector: 'sdk-datagrid',
  templateUrl: './datagrid.component.html',
  styleUrls: ['./datagrid.component.scss'],
  standalone: true,
  imports: [CommonModule,
            GridModule,
            DropDownButtonAllModule
           ],
        //    ToolbarItems,
        //    ExcelExportService,
      //      PdfExportService],
  providers: [PageService,
              SortService,
              FilterService,
              GroupService,
              ColumnMenuService,
              ColumnChooserService,
              ToolbarService,
              ReorderService,
              ExcelExportService,
              PdfExportService,
              ResizeService,
              FreezeService,
               ]
})
export class DataGridComponent implements OnInit  {

    private _showActions: boolean = true;
    private _showActionEdit: boolean = true;
    private _showActionDelete: boolean = true;
    private _actionDeleteText: string = 'Excluir';

    @ViewChild('toolbarGrid', {static: false}) toolbarGrid: ElementRef;

    @Input() dataSource?: DataManager;
    @Input() columns: ColumnModel[] = [];
    @Input() allowRowDragAndDrop: false;
    @Input() allowFiltering: false;
    //@Input() allowGrouping:  { showGroupedColumn: true };
    @Input() rowHeight: 30;
  //  @Input() selectionOptions = { allowSelection: false };


    @Input('showActions')
    get showActions(): boolean {
        return this._showActions;
     }
     set showActions(value: boolean) {
        this._showActions = value;
     }


    @Input('showActionEdit')
    get showActionEdit(): boolean {
        return this._showActionEdit;
     }
      set showActionEdit(value: boolean) {
        this._showActionEdit = value;
     }

    @Input('showActionDelete')
    get showActionDelete(): boolean {
        return this._showActionDelete;
     }
      set showActionDelete(value: boolean) {
        this._showActionDelete = value;
     }

    @Input('actionDeleteText')
    get actionDeleteText(): string {
        return this._actionDeleteText;
     }
      set actionDeleteText(value: string) {
        this._actionDeleteText = value;
     }



    @Output() onAdd = new EventEmitter<any>();
    @Output() onEdit = new EventEmitter<any>();
    @Output() onDelete = new EventEmitter<any>();
    @Output() onUpdate = new EventEmitter<any>();

    @ViewChild('grid') grid!: GridComponent;
    //@ViewChild('customToolbarTemplate') public customToolbarTemplate: TemplateRef<any>;
    @ViewChild('template',{static:true})  public toolbarTemplate: any;
    @ViewChild('template2', {static:true}) public dropdownTemplate: any;

    public pageOptions?: object;
    public query?: Query;
    public pageSettings?: PageSettingsModel;
    public showFilters = true;
    public showGrouping = true;
    public groupOptions?: GroupSettingsModel;
    public filterSettings?: FilterSettingsModel;
    public searchSettings?: SearchSettingsModel;
    public columnMenuItems?: any = [{ text: 'Clear Sorting', id: 'gridclearsorting' }];
    public key?:string = '';
    public toolbar: any;
    public loadingIndicator: Object;
    public editSettings: Object;
    gridLines: string = 'Vertical';


    public exportItems: ItemModel[] = [
        { text: 'PDF', iconCss: '.e-pdf' },
        { text: 'Excel', iconCss: 'e-excel' },
        { text: 'CSV', iconCss: 'e-csv' }
    ];

    ngOnInit(): void
    {
        this.pageOptions = { enableQueryString: true, pageSize: 50 ,pageSizes : [10,20,50,100] , pageCount : 6  };
        this.groupOptions = { showGroupedColumn: true };
        this.filterSettings = {  type: 'Excel'};
        this.searchSettings = { ignoreAccent: true, ignoreCase: true,operator:'contais'};
        this.toolbar = ['Add','Print',  {template: this.dropdownTemplate}, 'Search'];
        this.editSettings = { allowAdding: true };
        this.loadingIndicator = {indicatorType: 'Shimmer'};
    }

    public onGridCreated() : void {

        const gridElement = this.grid.element;

        gridElement.insertBefore(this.grid.groupModule.element,this.grid.getHeaderContent());

        //Faz a busca após digitar pelo menos 3 caracteres no campo "Procurar"
        (document.getElementById((this.grid as GridComponent).element.id + "_searchbar") as Element).addEventListener('keyup', () => {

            var searchText: string = ((event as MouseEvent).target as HTMLInputElement).value;

            if ( searchText.length >= 3 ) {
                (this.grid as GridComponent).search(searchText);
            }

        });
    }

    exportGrid(args: any): void {
        if (args.item.text === 'PDF') {
          this.grid.pdfExport();
        } else if (args.item.text === 'Excel') {
          this.grid.excelExport();
        } else if (args.item.text === 'CSV') {
          this.grid.csvExport();
        }
      }

      editRecord(args: any)
      {
         alert('Teste') ;
      }

     toolBarClikc(args: any)
     {
        if ( (args.item.id.includes('add')) ) {
            this.onAdd.emit(args);
        }
     
     }


     queryCellInfo(args: QueryCellInfoEventArgs) {
        if ((this.key as string) != '') {
          var cellContent = (args.data as ColumnData)[(args.column as Column).field];
          var parsedContent = cellContent.toString().toUpperCase();
          if (parsedContent.includes((this.key as string).toUpperCase())) {
            var i = 0;
            var searchStr = '';
            while (i < (this.key as string).length) {
              var index = parsedContent.indexOf((this.key as string)[i]);
              searchStr = searchStr + cellContent.toString()[index];
              i++;
            }
            (args.cell as HTMLElement).innerHTML = (args.cell as HTMLElement).innerText.replace(
              searchStr,
              "<span class='grid-highlight'>" + searchStr + '</span>'
            );
          }
        }
    }

}
