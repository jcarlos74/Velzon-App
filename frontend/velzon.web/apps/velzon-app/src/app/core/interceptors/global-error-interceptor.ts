import { HttpErrorResponse } from "@angular/common/http";
import { ErrorHandler, Injectable, NgZone } from "@angular/core";
import Swal from 'sweetalert2';

@Injectable({ providedIn: 'root' })
export class GlobalErrorHandler implements ErrorHandler
{
    constructor(
        //  private errorDialogService: ErrorDialogService,
        private zone: NgZone
    ) { }

    handleError(error: any)
    {
        let errObj = error;

        // Check if it's an error from an HTTP response
        if (!(error instanceof HttpErrorResponse)) {
            // error = error.rejection; // get the error object
            errObj = error.rejection ? error.rejection : error;
        }

        this.zone.run(() =>
            /* this.errorDialogService.openDialog(
               error?.message || 'Undefined client error',
               error?.status
             )*/

            Swal.fire({
                title: 'Erro',
                text: error?.message || 'Erro não identificado.',
                icon: 'error',
                showCancelButton: false
            })

        );

        console.error('Erro ocorrido', error);
    }
}
