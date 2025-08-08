export interface DataGridColumns
{
    /**Identificador da coluna */
    id: string,
     /**Titulo da coluna */
    name: string,
     /**Nome do campo no base de dados/json */
    field: string,
    sortable?: boolean,
    filterable?: boolean,
    type: string,
    resizable?:boolean,
    editor?: any,
    formatter?: any,
    filter?: any,
    outputType?: any,
    params?: any,
    width?: number,
    minWidth?: number,
    visible?:boolean,
    cssClass?: string,
    exportWithFormatter?: boolean,
    unselectable?:boolean,
    colspan?: number | '*'
}
