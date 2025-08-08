import { RouterLink } from '@angular/router';
/* eslint-disable @typescript-eslint/ban-types */
import { HttpClient } from '@angular/common/http';
/* eslint-disable @typescript-eslint/no-explicit-any */
/* eslint-disable @angular-eslint/no-empty-lifecycle-method */
/* eslint-disable @typescript-eslint/no-empty-function */

import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnInit, Output, Renderer2, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AngularGridInstance,
         AngularSlickgridComponent,
         AngularSlickgridModule,
         BackendService,
         BackendServiceOption,
         Column,
         ColumnFilters,
         ContainerService,
         CurrentFilter,
         CurrentPagination,
         CurrentSorter,
         DelimiterType,
         ExcelExportService,
         FieldType,
         FileType,
         FilterChangedArgs,
         GridOption,
         Grouping,
         GroupingGetterFunction,
         MultiColumnSort,
         Pagination, PaginationChangedArgs,
         PaginationCursorChangedArgs,
         SharedService,
         SingleColumnSort,
         SlickGrid,
         TextExportService} from 'angular-slickgrid';
import { TranslateService } from '@ngx-translate/core';
import { DataGridSettings } from './datagrid-settings';
import { DataGridPaginationComponent } from '../datagrid-pagination/datagrid-pagination.component';



@Component({
  selector: 'sdk-datagrid',
  standalone: true,
  imports: [CommonModule,AngularSlickgridModule],
  template: '´`',
  providers: [ContainerService,AngularSlickgridModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DataGridComponentOld  implements OnInit,AfterViewInit,BackendService {

    private defaultPageSize = 100;


    gridHeightString!: string;
    gridWidthString!: string;

    @ViewChild('angularSlickGrid', { static: true }) angularSlickGrid!: AngularSlickgridComponent;

    columnDefinitions: Column[] = [];
    dataset!: any[];
    gridObj: SlickGrid;
    dataviewObj: any;
    isAutoEdit = false;
    updatedObject: any;
    isMultiSelect = true;
    selectedObjects!: any[];
    selectedObject: any;

    // Slick grid
    metaData: any;
    columnData: any;
    colsVisible: any = [];
    rowsData: any;
    selects: any;
    id: any;

    options!: BackendServiceOption;
    pagination?: Pagination;
    draggableGroupingPlugin: any;
    selectedGroupingFields: Array<string | GroupingGetterFunction> = ['', '', ''];

    excelExportService : ExcelExportService;
    textExportService : TextExportService;

    sortedGridColumn = '';
    currentPage = 1;
    filteredGridColumns = '';
    showPreHeader = false;
    showFilterRow = false;

    private _paginationComponent: any =
    {
      processing: false,
      realPagination: false
    };



    /** Propriedades */
    @Input() gridHeight = 100;
    @Input() gridWidth = 600;
    @Input()
    set paginationComponent(value: DataGridPaginationComponent)
    {

      if (value.realPagination)
      {
        this._paginationComponent = value;
        this.gridOptions.backendServiceApi =
        {
          service: this,
          preProcess: () => { },
          process:  () => {  null; },
          postProcess: () => { }
        } as any;


        this._paginationComponent.gridPaginationOptions = this.gridOptions;
        this.angularSlickGrid.createBackendApiInternalPostProcessCallback(this.gridOptions);
      }

    }
    get paginationComponent(): DataGridPaginationComponent
    {
      return this._paginationComponent;
    }

    /* Eventos */
    @Output() onFilterChanged: EventEmitter<FilterChangedArgs> = new EventEmitter<FilterChangedArgs>();
    @Output() onPaginationChanged: EventEmitter<PaginationChangedArgs> = new EventEmitter<PaginationChangedArgs>();
    @Output() onSortChanged: EventEmitter<any> = new EventEmitter<any>();


    //Funções injetadas
    private _onRowDoubleClick: Function = new Function();
    private _onRowClick: Function = new Function();

    private _selectedRow: any;

    gridOptions: GridOption;

    constructor(private httpClient: HttpClient, private translate: TranslateService, private el: ElementRef, private renderer: Renderer2,private cd: ChangeDetectorRef)
    {

        const defaultLang = 'pt-br';
        this.translate.use(defaultLang);

    }

    ngOnInit() {}

    ngAfterViewInit() {}

    setDefaultIfUndef(value: any,defaultValue: any): any
    {
        if ( value === undefined ){
           return defaultValue;
        }

        return value;
    }

     GridSettings(settings: DataGridSettings) {


      this.id = 'grid' + Math.floor(Math.random() * Math.floor(100));

      // get metadata from input JSON
      this.metaData = settings;

      const rowData: any = [];

      // check if allcolumns tag contains any children
      if (this.metaData.columnsConfig) {

        for (let index = 0; index < this.metaData.columnsConfig.length; index++)
        {

          this.columnDefinitions.push(this.metaData.columnsConfig[index]);

          if ( this.metaData.columnsConfig[index].visible === undefined || (this.metaData.columnsConfig[index].visible !== undefined && this.metaData.columnsConfig[index].visible === true) )
          {
              const col = { columnId: this.metaData.columnsConfig[index].id };

              this.colsVisible.push(col) ;
          }

          rowData[this.metaData.columnsConfig[index].id] = '';

        }

        const defaultGridHeight = 350;

        // Columns are not visible, seems to be a bug ? next line fixed it..
        this.gridObj?.setColumns(this.columnDefinitions);

        this.showPreHeader = this.setDefaultIfUndef(settings.showGroupPanel,false),

        this.gridOptions =
        {
          asyncEditorLoading: false,
          autoEdit: this.isAutoEdit,
          //autoResize: {
          //  container: '#common-grid-container',
         //   rightPadding: 10
         // },
          gridHeight: this.setDefaultIfUndef(settings.gridHeight,defaultGridHeight),
          gridWidth:"100%",
          enableColumnPicker: this.setDefaultIfUndef(settings.enableColumnPicker,true),
          enableCellNavigation: this.setDefaultIfUndef(settings.enableCellNavigation,true),
          enableRowSelection: this.setDefaultIfUndef(settings.enableRowSelection,true),
          rowSelectionOptions: { selectActiveRow: true },
          enableCheckboxSelector: this.setDefaultIfUndef(settings.enableCheckboxSelector,true),
          enableExcelExport: true ,
          excelExportOptions: { sanitizeDataExport: true },
         // registerExternalResources: [new ExcelExportService()],
          enableExcelCopyBuffer: true,
          enableAutoSizeColumns: false,
          enableTextExport: true,
          textExportOptions: {
             exportWithFormatter: true,
             sanitizeDataExport: true
          },
          checkboxSelector: { hideSelectAllCheckbox: true  } ,
          enableFiltering: this.setDefaultIfUndef(settings.enableFiltering,true),
          rowHeight: this.setDefaultIfUndef(settings.rowHeight,24),
          forceFitColumns: this.setDefaultIfUndef(settings.forceFitColumns,false),
          enableAutoTooltip: this.setDefaultIfUndef(settings.enableAutoTooltip,true),
          enableGridMenu: this.setDefaultIfUndef(settings.enableGridMenu,true),
          enablePagination: false,
          enableTranslate: true,
          i18n: this.translate,
          presets:{columns: this.colsVisible},
        //  registerExternalResources: [this.excelExportService, this.textExportService],
          showCustomFooter: false,
          customFooterOptions:
          {
             hideMetrics:true,
             metricTexts: {
              // default text displayed in the metrics section on the right
              // all texts optionally support translation keys,
              // if you wish to use that feature then use the text properties with the 'Key' suffix (e.g: itemsKey, ofKey, lastUpdateKey)
              // example "items" for a plain string OR "itemsKey" to use a translation key
              itemsKey: 'ITEMS',
              ofKey: 'OF',
              lastUpdateKey: 'LAST_UPDATE' ,


             },
             dateFormat: 'DD-MM-YYYY',
             hideTotalItemCount: true,
             hideLastUpdateTimestamp: true,
             hideRowSelectionCount: true,
             leftFooterText:''
          },
          enableDraggableGrouping: this.setDefaultIfUndef(settings.enableDraggableGrouping,true),
          createPreHeaderPanel: true,
          showPreHeaderPanel: this.showPreHeader,
          preHeaderPanelHeight: 40,
          draggableGrouping: {
          dropPlaceHolderText: 'Arraste um cabeçalho de coluna aqui para agrupar pela coluna',
          // groupIconCssClass: 'fa fa-outdent',
          deleteIconCssClass: 'fa fa-times',
          onGroupChanged: (e, args) => this.onGroupChanged(args),
          onExtensionRegistered: (extension) => this.draggableGroupingPlugin = extension,
        },

        };


        //this.angularSlickGrid.gridService.hideColumnById();
        this.angularSlickGrid.showPagination = false;


        // Show filters when grid starts; this could be parametrized
        // this.gridObj.setHeaderRowVisibility(false);
        // this.gridObj.setTopPanelVisibility(false);
      }

      // Dummy dataset
      this.dataset = rowData;


      //Senão foi configurado para exibir filtro, esconde a linha de filtro
      this.showFilterRow = this.gridObj?.getOptions()?.enableFiltering;

     // this.cd.detectChanges();

    }

    CommonGrid(columnsData: any, lockedColumnCount: number, uniqueColumn: string, baseURL: string, programId: string, componentId: string, enableRenders:  true, colValidationMap: any = null, checkHeader:  false, cboLinked:  false)
    {}

    set gridData(rawData: any)
    {

      const dataProvider: any = [];

      for (let index = 0; rawData && index < rawData.length; index++) {
        const row = <Object>rawData[index];
        const idObj = {
          id: index
        };

        let key: string;
        const rowData: any = [];
        for (key in row) {
          if (row.hasOwnProperty(key))
          {
           // console.log((row as any)[key])

            rowData[key] = (row as any)[key];
          }
        }

        dataProvider[index] = Object.assign(rowData, idObj);

      }

      this.dataset = dataProvider;

      this.paginationComponent.processing = false;


      this.cd.detectChanges();

      // this.gridObj.setSortColumn('excludeType', true);
      // this.dataviewObj.reSort();
      // this.gridObj.setSortColumns([{'columnId':'excludeType','sortAsc':true}]);

     //  this.gridObj.invalidate();
     //  this.gridObj.render();
    }

    get gridData(): any {
       return this.dataset;
    }

    gridReady(instance: AngularGridInstance) {

      this.gridObj = instance.slickGrid;
      this.dataviewObj = instance.dataView;

    }

    dataviewReady(dataview: any)
    {

      this.dataviewObj = dataview;
    }

    clearFilters(): void
    {
        this.angularSlickGrid.filterService.clearFilters();
    }

    disableFilterRow(): void
    {
        this.angularSlickGrid.filterService.toggleFilterFunctionality(true);

        this.cd.detectChanges();
    }

    toggleFilterRow(): void
    {
        this.angularSlickGrid.filterService.toggleFilterFunctionality();
    }

    clearGrouping()
    {

      if (this.draggableGroupingPlugin && this.draggableGroupingPlugin.setDroppedGroups)
      {
        this.draggableGroupingPlugin.clearDroppedGroups();
      }
      this.gridObj.invalidate(); // invalidate all rows and re-render
    }

    togglePanelGroup(): void
    {
       this.clearGrouping();

       this.showPreHeader = !this.showPreHeader;
      // this.gridObj.getOptions().enableDraggableGrouping = this.showPreHeader;
      // this.gridObj.getOptions().showPreHeaderPanel= this.showPreHeader;

       this.gridObj.setPreHeaderPanelVisibility(this.showPreHeader); //,true); //!this.gridObj.getOptions().showPreHeaderPanel);
    }

    clearGroupingSelects() {
      this.selectedGroupingFields.forEach((g, i) => this.selectedGroupingFields[i] = '');
    }

    collapseAllGroups()
    {
      this.dataviewObj.collapseAllGroups();
    }

    expandAllGroups()
    {
      this.dataviewObj.expandAllGroups();
    }

    onGroupChanged(change: { caller?: string; groupColumns: Grouping[] })
    {
      // the "caller" property might not be in the SlickGrid core lib yet, reference PR https://github.com/6pac/SlickGrid/pull/303
      const caller: string | any[] = change && change.caller || [];
      const groups = change && change.groupColumns || [];

      if (Array.isArray(this.selectedGroupingFields) && Array.isArray(groups) && groups.length > 0) {
        // update all Group By select dropdown
        this.selectedGroupingFields.forEach((g, i) => this.selectedGroupingFields[i] = groups[i] && groups[i].getter || '');
      }
      else if (groups.length === 0 && caller === 'remove-group') {
        this.clearGroupingSelects();
      }
    }


    exportToExcel(excelFileName:string)
    {
      this.excelExportService.exportToExcel({
        filename: excelFileName,
        format: FileType.xlsx
      });
    }

    exportToFile(fileName:string, type = 'csv')
    {
      this.textExportService.exportToFile({
        delimiter: (type === 'csv') ? DelimiterType.comma : DelimiterType.tab,
        filename: fileName,
        format: (type === 'csv') ? FileType.csv : FileType.txt
      });
    }

    /********************************************************/
    /******** Pagination+Sot+Filter service : START *********/
    /********************************************************/
    buildQuery(): string {
      return 'buildQuery...';
    }

    init(serviceOptions: BackendServiceOption, pagination?: Pagination): void {
      this.options = serviceOptions;
      this.pagination = pagination;
    }

    resetPaginationOptions() {

    }

    updateOptions(serviceOptions?: Partial<BackendServiceOption>) {
      this.options = { ...this.options, ...serviceOptions };
    }


    /**
     * FILTERING EMIT EVENT
     * @param event
     * @param args
     */
    processOnFilterChanged(event: Event | undefined, args: FilterChangedArgs): string {

     /*  this.filteredGridColumns = '';
      let timing = 0;
      if (event && (event.type === 'keyup' || event.type === 'keydown')) {
        timing = DEFAULT_FILTER_TYPING_DEBOUNCE;
        clearTimeout(timer);
      }
      timer = setTimeout(() => {
        this.filteredGridColumns = '';
        for (let idx = 0; idx < this.columnDefinitions.length; idx++) {
          if (args.columnFilters.hasOwnProperty(this.columnDefinitions[idx].field)) {
            this.filteredGridColumns += args.columnFilters[this.columnDefinitions[idx].field].searchTerms[0] + '|';
          } else {
            this.filteredGridColumns += 'All|';
          }
        }

        // Reset to the first page
        this.paginationComponent.pageNumber = 1;
        this.currentPage = 1;

        // dispatch event
        this.onFilterChanged.emit(args);
        console.log('method [onFilterChanged] - onFilterChanged.emit(args) performed, filteredGridColumns=' + this.filteredGridColumns);
      }, timing);
  */
        this.onFilterChanged.emit(args);


      return '';
    }


    /**
     * PAGINATION EMIT EVENT
     * @param event
     * @param args
     */
    processOnPaginationChanged(event: Event | undefined, args: PaginationChangedArgs) {

      this.currentPage = args.newPage;
      this.onPaginationChanged.emit(args);

      return 'onPaginationChanged';
    }

    /**
     * SORT EMIT EVENT
     * @param event
     * @param args
     */
    processOnSortChanged(event: Event | undefined, args: any) {

      this.sortedGridColumn = '';
      const sortDirection = '|' + args!.sortCols![0].sortAsc + '|';
      for (let idx = 0; idx < this.columnDefinitions.length; idx++) {
        if (this.columnDefinitions[idx].field === args!.sortCols![0].sortCol.field) {
          this.sortedGridColumn = '' + idx + sortDirection;
        }
      }
      this.onSortChanged.emit(args);

      return 'onSortChanged';
    }

    getFilteredGridColumns() {
      return this.filteredGridColumns;
    }

    getSortedGridColumn() {
      return this.sortedGridColumn;
    }

     /******** Pagination+Sot+Filter service: END *****************/

    // Getters and Setters
    get selectedRow() {
      return this._selectedRow;
    }
    set selectedRow(row: any) {
      this._selectedRow = row;
    }

    get onRowDoubleClick() {
      return this._onRowDoubleClick;
    }
    set onRowDoubleClick(event: Function) {
      this._onRowDoubleClick = event;
    }

    get onRowClick() {
      return this._onRowClick;
    }
    set onRowClick(event: Function) {
      this._onRowClick = event;
    }

    refreshGrid(id:string) {
    if (this.gridObj && this.gridObj.getData()) {
      this.gridObj.getData().beginUpdate();
      this.gridObj.getData().setItems(this.dataset, id);
      this.gridObj.getData().endUpdate();
      this.gridObj.invalidate();
    }
  }

   /* dispose?: () => void;

    postProcess?: (processResult: any) => void;
    clearSorters?: () => void;

    getDatasetName?: () => string;
    getCurrentFilters?: () => ColumnFilters | CurrentFilter[];
    getCurrentPagination?: () => CurrentPagination | null;
    getCurrentSorters?: () => CurrentSorter[];

    updateFilters?: (columnFilters: ColumnFilters | CurrentFilter[], isUpdatedByPresetOrDynamically: boolean) => void;
    updatePagination?: (newPage: number, pageSize: number, cursorArgs?: PaginationCursorChangedArgs) => void;
    updateSorters?: (sortColumns?: Array<SingleColumnSort>, presetSorters?: CurrentSorter[]) => void;*/



}
