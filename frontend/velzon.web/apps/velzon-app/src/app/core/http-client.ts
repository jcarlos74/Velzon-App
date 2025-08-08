import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "@velzon.web/environments/dev";

export interface IRequestOptions
{
    headers?: HttpHeaders;
    observe?: 'body';
    params?: HttpParams;
    reportProgress?: boolean;
    responseType?: 'json';
    withCredentials?: boolean;
    body?: any;
}

@Injectable({
    providedIn: 'root'
})
export class ApplicationHttpClient
{

    private api = environment.apiUrl;
    
    httpOptions = {
        headers: new HttpHeaders({
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Credentials': 'true',
            'Access-Control-Allow-Headers': 'Content-Type',
            'Access-Control-Allow-Methods': 'GET,PUT,POST,DELETE'
        })};
    
    
    //Estendendo o HttpClient através do Angular DI.
    public constructor(public http: HttpClient)
    {
        // Se quiser usar as versões estendidas em alguns casos, pode-se acessar a propriedade pública e usar a original.
        // ex. this.httpClient.http.get(...)
    }

    /**
     * GET request
     * @param {string} endPoint
     * @param {IRequestOptions} options opções da solicitação, como cabeçalhos, corpo, etc.
     * @returns {Observable<T>}
     */
    public Get<T>(endPoint: string, options?: IRequestOptions): Observable<T>
    {
        return this.http.get<T>(endPoint, this.httpOptions );
    }
    
    /**
     * POST request
     * @param {string} endPoint end pointda api
     * @param {Object} params corpo da solicitação.
     * @param {IRequestOptions} options opções da solicitação, como cabeçalhos, corpo, etc.
     * @returns {Observable<T>}
     */
    public Post<T>(endPoint: string, params: object, options?: IRequestOptions): Observable<T>
    {
        return this.http.post<T>(endPoint, params);
    }
    
    /**
     * PUT request
     * @param {string} endPoint end point of the api
     * @param {Object} params body of the request.
     * @param {IRequestOptions} options options of the request like headers, body, etc.
     * @returns {Observable<T>}
     */
    public Put<T>(endPoint: string, params: object, options?: IRequestOptions): Observable<T>
    {
        return this.http.put<T>( endPoint, params, options);
    }

    /**
     * DELETE request
     * @param {string} endPoint end point of the api
     * @param {IRequestOptions} options options of the request like headers, body, etc.
     * @returns {Observable<T>}
     */
    public Delete<T>(endPoint: string, options?: IRequestOptions): Observable<T>
    {
        return this.http.delete<T>(endPoint, options);
    }
}

export function applicationHttpClientCreator(http: HttpClient)
{
    return new ApplicationHttpClient(http);
}
