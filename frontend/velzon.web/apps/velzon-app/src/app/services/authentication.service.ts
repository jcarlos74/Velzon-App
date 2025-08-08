import { Injectable } from '@angular/core';
import { BehaviorSubject, EMPTY, map, Observable } from 'rxjs';
import { AccessCredentials, AccessToken, OperationResult, RefreshToken } from '@velzon.web/core/model';
import { HttpClient } from '@angular/common/http';
import { TokenStorageService } from './token-storage.service';
import { environment } from '@velzon.web/environments/prod';
import { Router } from '@angular/router';

@Injectable({
    providedIn: 'root'
})
export class AuthenticationService
{
    httpOptions =
        {
            headers: { 'Access-Control-Allow-Origin': '*' }
        };

    private currentUserTokenSubject!: BehaviorSubject<AccessToken>;
    public currentUserToken!: Observable<AccessToken>;

    constructor(private http: HttpClient, private tokenStorage: TokenStorageService, private router: Router)
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

        return this.http.post<OperationResult<AccessToken>>(`${ environment.apiUrl }usuario/Login`, accessCrendetials, this.httpOptions)
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
                
                sessionStorage.setItem('toast', 'true');

                return result.data;
            }));

    }

    logout()
    {
        const nullToken = {} as AccessToken;
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

        const dataRefreshToken: RefreshToken =
        {
            userName: accessToken.userInfo?.email,
            refreshToken: accessToken.refreshToken
        };

        return this.http.post<OperationResult<AccessToken>>(`${ environment.apiUrl }cta/refresh-token`, { dataRefreshToken })
            .pipe(map((result) =>
            {
                this.tokenStorage.saveToken(result.data)
                this.tokenStorage.saveUser(result.data.userInfo);

                this.currentUserTokenSubject.next(result.data);

                return result;
            }));
    }

}
