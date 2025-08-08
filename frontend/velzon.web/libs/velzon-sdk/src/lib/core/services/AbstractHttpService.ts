import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OperationResult } from '../models/operation-result';
import { FilterModel } from '../models/filter-model';

export abstract class AbstractHttpService<T> {
  constructor(protected http: HttpClient, private baseUrl: string) {}

  protected getHeaders(): HttpHeaders {
    return new HttpHeaders({ 'Content-Type': 'application/json' });
  }

  get(endpoint: string, params?: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${endpoint}`, {
      headers: this.getHeaders(),
      params,
    });
  }

  getAll(endpoint: string, params?: HttpParams): Observable<T[]> {
    return this.http.get<T[]>(`${this.baseUrl}/${endpoint}`, {
      headers: this.getHeaders(),
      params,
    });
  }

  post(endpoint: string, body: any): Observable<T> {
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

  put(endpoint: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, {
      headers: this.getHeaders(),
    });
  }

  patch(endpoint: string, body: any): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}/${endpoint}`, body, {
      headers: this.getHeaders(),
    });
  }

  delete(endpoint: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${endpoint}`, {
      headers: this.getHeaders(),
    });
  }

  postWithParams(endpoint: string, body: any, params?: HttpParams): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body, {
      headers: this.getHeaders(),
      params,
    });
  }

  putWithParams(endpoint: string, body: any, params?: HttpParams): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, {
      headers: this.getHeaders(),
      params,
    });
  }
}