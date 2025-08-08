import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AbstractHttpService,  OperationResult,  SmaCidade } from '@velzon.web/velzon-sdk';
import { FilterModel } from 'libs/velzon-sdk/src/lib/core/models/filter-model';
import { Observable } from 'rxjs';

@Injectable({providedIn: 'root'})
export class GrupoAcessoService extends AbstractHttpService<SmaCidade> {

//constructor(private http: HttpClient) { super(http,'/api/cta/GrupoAcesso/') }
//constructor(private http: HttpClient) { super(http,'/api/velzon/teste/') }

    constructor(http: HttpClient) { super(http,'/api/velzon/teste/') }


}

