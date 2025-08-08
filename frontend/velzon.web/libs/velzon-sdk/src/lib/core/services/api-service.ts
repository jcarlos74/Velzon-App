import { HttpClient, HttpErrorResponse, HttpHeaders } from "@angular/common/http";
import { IOperationsService } from "../models/operation-service";
import { catchError, map, Observable, of } from "rxjs";
import { OperationResult } from "../models/operation-result";
import { FilterModel } from "../models/filter-model";

export abstract class ApiService implements IOperationsService
{
    httpOptions =
    {
        headers: new HttpHeaders({ 'Content-Type': 'application/json', 'Accept': 'application/json'})
    };

    private _baseApiUrl: string = '';
    
    constructor(private _http: HttpClient,apiEndPoint:string)
    {
       this._baseApiUrl = apiEndPoint;
    }

    /**Insere um novo registro */
    create<T>(serviceEndpoint: string, entity: T): Observable<OperationResult<number>>
    {
        let urlApi = `${this._baseApiUrl}${serviceEndpoint}`;

        return this.mapAndCatchError(this._http.post<OperationResult<number>>
        (
            urlApi, entity, this.httpOptions
        ));

    }

    update<T>(serviceEndpoint: string, id: number, entity: T): Observable<OperationResult<T>>
    {
        let urlApi =  `${this._baseApiUrl}${serviceEndpoint}`;

        return this.mapAndCatchError(this._http.post<OperationResult<T>>
        (
            urlApi, entity, this.httpOptions
        ));

    }

    delete<T>(serviceEndpoint: string, id: number): Observable<OperationResult<any>>
    {
        let urlApi =  `${this._baseApiUrl}${serviceEndpoint}`;

        return this.mapAndCatchError(this._http.delete<OperationResult<T>>(`${urlApi}/${id}`, this.httpOptions));
    }

    findById<T>(serviceEndpoint: string, id: number): Observable<OperationResult<T>>
    {
         let urlApi =  `${this._baseApiUrl}${serviceEndpoint}`;

        return this.mapAndCatchError(this._http.get<OperationResult<T>>(`${urlApi}/${id}`, this.httpOptions));
    }

    findAll<T>(serviceEndpoint: string): Observable<OperationResult<T[]>>
    {
        let urlApi =  `${this._baseApiUrl}${serviceEndpoint}`;

        return this.mapAndCatchError(this._http.get<OperationResult<T[]>>(urlApi, this.httpOptions));
    }

    /**Carrega uma lista conforme as opções de carregamento definidas em loadOptions */
    loadList<T>(serviceEndpoint: string, loadOptions: FilterModel): Observable<OperationResult<T[]>>
    {
         let urlApi = `${this._baseApiUrl}${serviceEndpoint}`;

        return this.mapAndCatchError(this._http.post<OperationResult<T[]>>
        (
            urlApi, loadOptions, this.httpOptions
        ));
    }

    loadDropDown<T>(serviceEndpoint: string):  Observable<OperationResult<T[]>>
    {
           let urlApi =  `${this._baseApiUrl}${serviceEndpoint}`;

          return this._http.get<OperationResult<T[]>>(urlApi);
    }

    /**Executa um Request generico conforme o metodo http e serviceEndpoint informado */
    makeRequest<T>(httpMethod: string, serviceEndpoint: string, data: any): Observable<OperationResult<T>>
    {
         let urlApi = `${this._baseApiUrl}${serviceEndpoint}`;

        let body: any = null;

        if (httpMethod.toUpperCase() == 'GET')
        {
            urlApi += '?' + this.objectToQueryString(data);
        }
        else
        {
          body = data;
        }

        return this.mapAndCatchError<T>(
          this._http.request<OperationResult<T>>(httpMethod.toUpperCase(),
                                             urlApi,
                                             { body: body, headers: this.httpOptions.headers })
        );
    }

    /** */
    private mapAndCatchError<T>(response: Observable<OperationResult<T>>) : Observable<OperationResult<T>>
    {
      return response.pipe(
        map((r: OperationResult<T>) =>
        {
          var result = new OperationResult<T>();
          Object.assign(result, r);
          return result;
        }),
        catchError((err: HttpErrorResponse) =>
        {
          var result = new OperationResult<T>();

          // if err.error is not ApiResponse<TData> e.g. connection issue
          if (err.error instanceof ErrorEvent || err.error instanceof ProgressEvent)
          {
             result.message = 'Erro desconhecido.';
          }
          else
          {
             Object.assign(result, err.error)
          }

          return of(result);

        })
      );
    }

    private objectToQueryString(obj: any): string {
      var str = [];
      for (var p in obj)
        if (obj.hasOwnProperty(p)) {
          str.push(encodeURIComponent(p) + "=" + encodeURIComponent(obj[p]));
        }
      return str.join("&");
    }

}

