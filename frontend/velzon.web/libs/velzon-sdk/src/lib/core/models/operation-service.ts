import { Observable } from "rxjs";
import { OperationResult } from "./operation-result";
import { FilterModel } from "./filter-model";

export interface IOperationsService
{

  create<T>(serviceEndpoint: string, entity: T): Observable<OperationResult<any>>

	update<T>(serviceEndpoint: string,id: number, entity: T): Observable<OperationResult<T>>;

  delete<T>(serviceEndpoint: string,id: number):  Observable<OperationResult<any>>;

	findById<T>(serviceEndpoint: string,id: number): Observable<OperationResult<T>>;

	findAll<T>(serviceEndpoint: string): Observable<OperationResult<T[]>>;

  loadList<T>(serviceEndpoint: string,loadOptions: FilterModel):  Observable<OperationResult<T[]>>;

  loadDropDown<T>(serviceEndpoint: string):  Observable<OperationResult<T[]>>;

  /**Metodo comum para fazer qualquer requisição */
  makeRequest<T>(httpMethod: string, serviceEndpoint: string, data: any) : Observable<OperationResult<T>>;

}
