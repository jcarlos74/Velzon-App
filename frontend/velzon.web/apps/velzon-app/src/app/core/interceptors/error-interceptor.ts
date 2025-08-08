import { Injectable } from '@angular/core';
import { HttpErrorResponse, HttpResponse, HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { throwError, Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr'
import HttpStatusCode from '@velzon.web/core/enums'
import Swal from 'sweetalert2';
import { AuthenticationService } from '@velzon.web/core/services';

//Error Interceptor intercepta as respostas http da api para verificar se houve algum erro.
//Se a resposta for 401 Unauthorizedou 403 Forbidden o usuário é desconectado automaticamente do aplicativo, todos os outros erros serão registrados no console e enviados novamente ao serviço de chamada para que um alerta com o erro possa ser exibido na IU.
@Injectable({ providedIn: 'root' })
export class ErrorInterceptor implements HttpInterceptor
{
    constructor(private authService: AuthenticationService, private notify: ToastrService) { }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>>
    {
        return next.handle(request).pipe(map((event: HttpEvent<any>) =>
        {
            if (event instanceof HttpResponse) {
                if (event.status === HttpStatusCode.NO_CONTENT) {
                    this.notify.warning('Nenhum registro localizado.', 'Atenção');
                }
            }
            return event;
        }),
            catchError((err: HttpErrorResponse) =>
            {
                const { error } = err;

                if ([401, 403].includes(err.status) && this.authService.currentUserTokenValue) {
                    //auto logout se 401 ou 403 for retornado da api
                    this.authService.logout();

                }
                else if (!(error instanceof HttpErrorResponse)) { // Check if it's an error from an HTTP response
                            // error = error.rejection; // get the error object
                         //   error = error.rejection ? error.rejection : error;
                     Swal.fire({
                            title: 'Erro',
                            text: err.message,
                            icon: 'error',
                            showCancelButton: false
                        });
                }
                else {
                    if (err.status) {
                        Swal.fire({
                            title: 'Erro',
                            text: err.message,
                            icon: 'error',
                            showCancelButton: false
                        });

                        /* if (this.isServerError(err))  //this.notify.error(error.message, 'Atenção');
                             Swal.fire({
                                  title: 'Erro',
                                  text: error.message,
                                  icon: 'error',
                                  showCancelButton: false
                                });
                         else
                           this.notify.warning(error.message, 'Atenção');*/
                    }
                    else {
                        // this.notify.error('Erro ao se conectar com o servidor.', 'Atenção');
                        Swal.fire({
                            title: 'Erro',
                            text: 'Erro ao se conectar com o servidor.',
                            icon: 'error',
                            showCancelButton: false
                        });
                    }
                }


                return throwError(err);
            }));


    }

    private isServerError(error: any): boolean
    {
        return error.status === HttpStatusCode.INTERNAL_SERVER_ERROR;
    }
}
