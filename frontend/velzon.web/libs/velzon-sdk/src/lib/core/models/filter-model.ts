import { FilterColumn } from "./filter-column";
import { FilterSorter } from "./filter-sorter";

 export interface FilterModel
 {
    filtersColumns: FilterColumn[] ;
    globalFilter: string;
    firstRowIndex: number;
    rowsPerPage: number;
    currentPage: number;
    sortField: string;
    sortOrder: number;
    multiSort: FilterSorter[];
}