/* eslint-disable @angular-eslint/component-selector */
/* eslint-disable @angular-eslint/no-empty-lifecycle-method */
/*import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { GridOption } from 'angular-slickgrid';
import { DataGridComponent } from '../datagrid/datagrid.component';

@Component({
  selector: 'sdk-datagrid-pagination',
  templateUrl: './datagrid-pagination.component.html',
  standalone: true,
  styleUrls: ['./datagrid-pagination.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DataGridPaginationComponent implements OnInit
{
   
   @Input() pageCount = 1;
   @Input() pageNumber = 1;

   totalItems = 0;
   public processing: boolean = false;

   realPagination = true;
  _gridPaginationOptions!: GridOption;

   dataGrid!: DataGridComponent;

   @Input()
   set gridPaginationOptions(gridPaginationOptions: GridOption )
   {


      this._gridPaginationOptions = gridPaginationOptions;

      // The backendServiceApi is itself the SwtCommonGridComponent (This is a hack)
      this.dataGrid = <DataGridComponent>this.gridPaginationOptions!.backendServiceApi!.service;
   }

   get gridPaginationOptions(): GridOption
   {
      return this._gridPaginationOptions;
   }

   constructor(private httpClient: HttpClient,private cd: ChangeDetectorRef) { }

   ngOnInit()
   {
      // console.log('Total de páginas:' + this.pageCount);
   }

   changeToFirstPage(event: any) {

    this.pageNumber = 1;
    this.onPageChanged(event, this.pageNumber);
  }

  changeToLastPage(event: any) {

    this.pageNumber = this.pageCount;
    this.onPageChanged(event, this.pageNumber);
  }

  changeToNextPage(event: any) {

    if (this.pageNumber < this.pageCount)
    {
      this.pageNumber++;
      this.onPageChanged(event, this.pageNumber);
    }
  }

  changeToPreviousPage(event: any)
  {

    if (this.pageNumber > 1)
    {
      this.pageNumber--;
      this.onPageChanged(event, this.pageNumber);
    }
  }


  changeToCurrentPage(event: any) {

    this.pageNumber = event.currentTarget.value;

    if (this.pageNumber < 1)
    {
      this.pageNumber = 1;
    }
    else if (this.pageNumber > this.pageCount)
    {
      this.pageNumber = this.pageCount;
    }

    this.onPageChanged(event, this.pageNumber);
  }

  onPageChanged(event?: Event, pageNumber?: number) {


    this.dataGrid.processOnPaginationChanged(event, { newPage: pageNumber as number, pageSize: -1 });

  }

  Refresh()
  {
      this.cd.detectChanges();
  }
} */
