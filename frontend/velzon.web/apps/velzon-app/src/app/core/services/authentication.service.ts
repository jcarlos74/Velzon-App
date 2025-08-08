/* eslint-disable prefer-const */
import { Injectable } from '@angular/core';
import { BehaviorSubject, EMPTY, first, map, Observable } from 'rxjs';
import { AccessCredentials, AccessToken, OperationResult, RefreshToken } from '../models';
import { HttpHeaders } from '@angular/common/http';
import { TokenStorageService } from './token-storage.service';
import { Router } from '@angular/router';
import { ApplicationHttpClient } from '../http-client';

@Injectable({
    providedIn: 'root'
})
export class AuthenticationService
{
    /*httpOptions: IRequestOptions =
        {
            headers: new HttpHeaders({
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Credentials': 'true',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'GET,PUT,POST,DELETE',
        })};*/

    httpOptions = {
        headers: new HttpHeaders({
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Credentials': 'true',
            'Access-Control-Allow-Headers': 'Content-Type',
            'Access-Control-Allow-Methods': 'GET,PUT,POST,DELETE'

        })
    };

    private currentUserTokenSubject!: BehaviorSubject<AccessToken>;
    public currentUserToken!: Observable<AccessToken>;

    constructor(private http: ApplicationHttpClient, private tokenStorage: TokenStorageService, private router: Router)
    {
        this.currentUserTokenSubject = new BehaviorSubject<AccessToken>(tokenStorage.getUser()!);
        this.currentUserToken = this.currentUserTokenSubject.asObservable();
    }

    public get currentUserTokenValue(): AccessToken
    {
        return this.currentUserTokenSubject.value;
    }

    login(userName: string, password: string)
    {

        const accessCrendetials = new AccessCredentials(userName, password);

        return this.http.Post<OperationResult<AccessToken>>('/api/cta/Account/login', accessCrendetials)
            .pipe(map(result =>
            {
                // armazene os detalhes do usuário e o token jwt no local storage
                this.tokenStorage.saveToken(result.data)
                this.tokenStorage.saveUser(result.data.userInfo);

                if (this.currentUserTokenSubject !== undefined) {
                    this.currentUserTokenSubject.next(result.data);
                }
                else {
                    this.currentUserTokenSubject = new BehaviorSubject<AccessToken>(result.data);
                }

                //VERIFICAR  sessionStorage.setItem('toast', 'true');

                return result.data;
            }));

    }

    logout()
    {
        let nullToken = {} as AccessToken;
        this.tokenStorage.signOut();

        this.currentUserTokenSubject.next(nullToken);
        this.router.navigate(['/auth/login']);
    }

    refreshToken()
    {
        const accessToken = this.currentUserTokenValue;

        if (accessToken?.userInfo?.userName == undefined) {
            return EMPTY;
        }

        let dataRefreshToken: RefreshToken =
        {
            userName: accessToken.userInfo?.email,
            refreshToken: accessToken.refreshToken
        };

        return this.http.Post<OperationResult<AccessToken>>('/cta/refresh-token', dataRefreshToken)
            .pipe(map((result) =>
            {
                this.tokenStorage.saveToken(result.data)
                this.tokenStorage.saveUser(result.data.userInfo);

                this.currentUserTokenSubject.next(result.data);

                return result;
            }));
    }

}
