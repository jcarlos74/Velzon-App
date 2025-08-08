import { DataGridColumns } from "./datagrid-columns";

export interface DataGridSettings
{
    rowsPerPage: number,
    globalFilter: boolean,
    paginator: boolean,
    emptyMessage: string,
    defaultSort: { field: string, order: number },
    rowsPerPageOptions: string[];
    gridHeight?: number,
    enableColumnPicker?: boolean,
    enableCellNavigation?: boolean,
    enableRowSelection?: boolean,
    enableCheckboxSelector?: boolean,
    enableFiltering?: boolean,
    rowHeight?: number,
    forceFitColumns?: boolean,
    enableAutoTooltip?: boolean,
    enableGridMenu?: boolean,
    showGroupPanel?: boolean,
    enableDraggableGrouping?:boolean,
    i18n?: any,
    columnsConfig: DataGridColumns[];
    footerText:'';
    showFooter: boolean;
}
