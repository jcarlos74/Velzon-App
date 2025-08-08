import { HTTP_INTERCEPTORS, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { TokenStorageService } from '@velzon.web/core/services';

const TOKEN_HEADER_KEY = 'Authorization';

@Injectable({providedIn: 'root'})
export class AuthInterceptor implements HttpInterceptor
{

    constructor(private token: TokenStorageService) { }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>>
    {
        let authReq = req;
        const token = this.token.getToken();

        if (token != null) {
            // Clonar a requisição original e substituir o cabeçalho de autorização
            authReq = req.clone({ headers: req.headers.set(TOKEN_HEADER_KEY, 'Bearer ' + token.accessToken) });
        }

         // Envia a requisição clonada com o cabeçalho de autorização
        return next.handle(authReq);
    }

}

/*export const authInterceptorProviders = [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
];*/
