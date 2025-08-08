import { Injectable } from '@angular/core';
import { AccessToken } from '../core/models';


const TOKEN_KEY = 'auth-token';
const USER_KEY = 'currentUser';

@Injectable({
    providedIn: 'root'
})
export class TokenStorageService
{
    constructor() { }

    signOut(): void
    {
        window.sessionStorage.clear();
    }

    public saveToken(token: any): void
    {
        window.sessionStorage.removeItem(TOKEN_KEY);
        window.sessionStorage.setItem(TOKEN_KEY, JSON.stringify(token));
    }

    public getToken(): AccessToken
    {
        const accessToken = window.sessionStorage.getItem(TOKEN_KEY);
        if (accessToken) {
            return JSON.parse(accessToken);
        }

        return {} as AccessToken;
    }

    public saveUser(user: any): void
    {
        window.sessionStorage.removeItem(USER_KEY);
        window.sessionStorage.setItem(USER_KEY, JSON.stringify(user));
    }

    public getUser(): any
    {
        const user = window.sessionStorage.getItem(USER_KEY);
        if (user) {
            return JSON.parse(user);
        }

        return {};
    }
}
