/*import { Component, ViewChild } from "@angular/core";
import { DataGridPaginationComponent } from "./ui/datagrid-pagination/datagrid-pagination.component";
import { DataGridComponent, DataGridSettings } from "..";
import { FilterModel } from "./core/models/filter-model";
import { Observable } from "rxjs";
import { FilterColumn } from "./core/models/filter-column";
import { Formatter } from "angular-slickgrid";

@Component({template: ''})
export class DataGridComponentBase {

    @ViewChild('dataGrid', { static: true }) dataGrid!: DataGridComponent;
    @ViewChild('dataGridPag', { static: true }) dataGridPag!: DataGridPaginationComponent;

    loadOptions        = {} as FilterModel;
    dataGridSettings   = {} as DataGridSettings ;

    totalPages: number = 0;
    defaultGridHeight?:number;
    panelHeight: number;

    showAreaGrid:string = "block";
    showAreaForm:string = "none";


    dataGridInitialize()
    {
        if (this.dataGridPag)
        {
           this.dataGrid.paginationComponent = this.dataGridPag;
           this.dataGrid.disableFilterRow();
        }

        setTimeout(() =>
        {

            //this.dataGridPag.processing = true;

            this.dataGrid.currentPage = 1;
            this.dataGridPag.pageNumber = 1;

            this.loadOptions.currentPage =1;
            this.loadOptions.firstRowIndex = 1;
            this.loadOptions.rowsPerPage = 100;

            this.loadOptions.sortField = this.dataGridSettings.defaultSort.field;
            this.loadOptions.sortOrder = this.dataGridSettings.defaultSort.order;

           /* dataSource.subscribe
            (
               (json: any) =>
               {
                   if ( json )
                   {

                      this.totalPages = Math.ceil(Number(json["totalRows"]) / this.loadOptions.rowsPerPage);

                      this.dataGrid.gridData =  json["data"];

                     this.dataGridPag.pageCount = this.totalPages;
                      this.dataGridPag.processing = false;

                      this.dataGridPag.Refresh();
                   }
               }
            )
        }, 0);

    }

    updateDataGrid(dataSource: Observable<object>)
    {

      this.loadOptions.currentPage = this.dataGrid.currentPage;


      if ( this.dataGrid.sortedGridColumn !== undefined && this.dataGrid.sortedGridColumn !== '')
      {
          let arrSorter=this.dataGrid.sortedGridColumn.split("|");

          let sortField = Number(arrSorter[0]);
          let sortOrder = JSON.parse(arrSorter[1]);

          console.log(sortField);
          console.log(sortOrder);

          this.loadOptions.sortField = this.dataGridSettings.columnsConfig[sortField].field;
          this.loadOptions.sortOrder =  sortOrder ? 1 :  -1;
      }

      dataSource.subscribe
      (
          (json: any) =>
          {
              if ( json )
              {
                //this.dataGrid.CustomGrid(this.columnsConfig);


                 this.totalPages = Math.ceil(Number(json["totalRows"]) / this.loadOptions.rowsPerPage);
                 this.dataGrid.gridData =  json["data"];
                 this.dataGridPag.pageCount = this.totalPages;

                 this.dataGridPag.Refresh();

                 this.dataGridPag.processing = false;

              }
          }
      );


    }

    prepareFilters(filters:any) : FilterColumn[]
    {
        let filtros: FilterColumn[]=[];

        for (let idx = 0; idx < filters.length; idx++)
        {
           if ( filters[idx]?.searchTerms[0] !== undefined && filters[idx]?.searchTerms[0] !== null )
           {
               let textSearch = filters[idx]?.searchTerms[0];

               if ( textSearch !== undefined && textSearch.length >= 3)
               {
                    filtros.push({ columnName: filters[idx]?.columnId, value: textSearch.toUpperCase(), filterMatchMode: filters[idx]?.operator });

                    //this.loadOptions.filtersColumns = filtros;
                }
           }
        }

        return filtros;
    }

    globalFilterChanged(dataSource: Observable<object>)
    {

        this.dataGridPag.processing = true;

        //Reset para página inicial
        this.dataGrid.currentPage = 1;
        this.dataGridPag.pageNumber = 1;

        this.updateDataGrid(dataSource);
    }

    linkFormatter: Formatter = (row, cell, value, columnDef, dataContext, grid) =>
    {
      //return '<a title="Clique aqui para editar" href="#">' + value + '</a>';
      return '<button class="row-link">' + value + '</button>'
    };

    alternaLinhaDeFiltros()
    {
       this.dataGrid.toggleFilterRow();
    }

    alternaPainelDeGrupo()
    {
      this.dataGrid.togglePanelGroup();
    }

    exportGridToFile()
    {
        this.dataGrid.exportToExcel("Cidades");

    }

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
}
*/
