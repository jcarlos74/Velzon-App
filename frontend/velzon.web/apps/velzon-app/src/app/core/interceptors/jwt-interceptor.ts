import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { AuthenticationService } from "@velzon.web/core/services"
import { environment } from "@velzon.web/environments/dev"
import { Observable } from "rxjs"

@Injectable({ providedIn: 'root' })
export class JwtInterceptor implements HttpInterceptor
{
    constructor(private authenticationService: AuthenticationService) { }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>>
    {
        // adicione o cabeçalho de autenticação com jwt se o usuário estiver conectado e a solicitação for para a URL da API
        const currentUser = this.authenticationService.currentUserTokenValue
        const isLoggedIn = currentUser && currentUser.accessToken
        const isApiUrl = request.url.startsWith(environment.apiUrl)

        if (isLoggedIn && isApiUrl) {
            request = request.clone(
                {
                    setHeaders:
                    {
                        Authorization: `Bearer ${ currentUser.accessToken }`
                    }
                })
        }

        return next.handle(request)
    }
}
