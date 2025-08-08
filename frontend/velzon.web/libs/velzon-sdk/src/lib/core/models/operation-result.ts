import { ModelBase } from "./model-base";


export class OperationResult<T> extends ModelBase
{
    success: boolean = false;
    message!: string;
    data!: T;
    errors?: string[];
    totalRows: 0;
}