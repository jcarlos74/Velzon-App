import { Injectable } from '@angular/core';
import { map, Observable, Subject } from 'rxjs';
import {DataStateChangeEventArgs,Sorts,DataResult, } from '@syncfusion/ej2-angular-grids';
import { HttpClient,HttpHeaders, HttpParams  } from '@angular/common/http';
import { FilterModel } from '../models/filter-model';
import { OperationResult } from '../models/operation-result';

@Injectable()
export class DataGridService extends Subject<DataStateChangeEventArgs> {
	
	loadOptions: FilterModel;
	
    baseUrl: string = 'api/velzon/teste/';
    urlApiService :string;
	
	constructor(public http: HttpClient) {super();}
	
    protected getHeaders(): HttpHeaders {
        return new HttpHeaders({ 'Content-Type': 'application/json' });  
    }
    
    
    public execute(state: any, query: any, serviceEndpoint: string): void {
        this.getData(serviceEndpoint,state, query).subscribe(x => super.next(x));      
    }
    
    public getData(serviceEndpoint: string, state: DataStateChangeEventArgs, addtionalParam: any): Observable<DataStateChangeEventArgs> {
      
        const urlApi = `${this.baseUrl}${serviceEndpoint}`;
        const pageQuery = `$skip=${state.skip}&$top=${state.take}`;
        
        let sortQuery: string = '';
    
        if ((state.sorted || []).length) {
            sortQuery = `&$orderby=` + state.sorted?.map((obj: Sorts) => {
            return obj.direction === 'descending' ? `${obj.name} desc` : obj.name;
            }).reverse().join(',');
        }
    
         
       // const customQuery = `&${addtionalParam.params[0].key}=${addtionalParam.params[0].value}`;
        
        this.loadOptions.currentPage = 1;
        this.loadOptions.rowsPerPage = 50;
        this.loadOptions.firstRowIndex = 1;
        
        // this.service.loadList("lista-cidades",this.loadOptions).subscribe((response: OperationResult<SmaCidade[]>)
        return  this.loadList(urlApi,this.loadOptions)
                .pipe(map((response: any) => response)) 
                .pipe(map((response: any) => (<DataResult>{
                    result: response['data'],
                    count: Response['totalRows'],
                    })))
                    .pipe((data: any) => data);      
      }
  
	  /** Teste */
 
     get<T>(endpoint: string, params?: HttpParams): Observable<T> {
        return this.http.get<T>(`${this.baseUrl}/${endpoint}`, {
          headers: this.getHeaders(),
          params,
        });
      }
    
      getAll<T>(endpoint: string, params?: HttpParams): Observable<T[]> {
        return this.http.get<T[]>(`${this.baseUrl}/${endpoint}`, {
          headers: this.getHeaders(),
          params,
        });
      }
    
      post<T>(endpoint: string, body: any): Observable<T> {
        return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body, {
          headers: this.getHeaders(),
        });
      }
    
      loadList<T>(endpoint: string, loadOptions: FilterModel): Observable<OperationResult<T[]>>
      {
    
           return this.http.post<OperationResult<T[]>>(`${this.baseUrl}/${endpoint}`, loadOptions, {
                  headers: this.getHeaders(),
           });
      }
    
      put<T>(endpoint: string, body: any): Observable<T> {
        return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, {
          headers: this.getHeaders(),
        });
      }
    
      patch<T>(endpoint: string, body: any): Observable<T> {
        return this.http.patch<T>(`${this.baseUrl}/${endpoint}`, body, {
          headers: this.getHeaders(),
        });
      }
    
      delete(endpoint: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${endpoint}`, {
          headers: this.getHeaders(),
        });
      }
    
      postWithParams<T>(endpoint: string, body: any, params?: HttpParams): Observable<T> {
        return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body, {
          headers: this.getHeaders(),
          params,
        });
      }
    
      putWithParams<T>(endpoint: string, body: any, params?: HttpParams): Observable<T> {
        return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, {
          headers: this.getHeaders(),
          params,
        });
      }     
	
	


}
