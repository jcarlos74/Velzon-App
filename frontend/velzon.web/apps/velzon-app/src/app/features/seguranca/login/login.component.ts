import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgbCarouselModule } from '@ng-bootstrap/ng-bootstrap';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationService } from '@velzon.web/core/services';
import { ToastService } from './toast-service';
import { AccessToken } from '@velzon.web/core/model';

@Component({
    selector: 'app-login',
    standalone: true,
    imports:
        [
            CommonModule,
            NgbCarouselModule,
            ReactiveFormsModule,
            FormsModule,
        ],
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit
{
    year: number = new Date().getFullYear();
    loginForm!: FormGroup;
    submitted = false;
    isLoggedIn = false;
    fieldTextType!: boolean;
    error = '';
    returnUrl!: string;
    showNavigationArrows: any; //exibir a seta e navegação do Carrosel

     loading = false;

    constructor(private formBuilder: FormBuilder,
                private route: ActivatedRoute,
                private router: Router,
                private authenticationService: AuthenticationService,
                public toastService: ToastService)
    {

        //redirecionada para página principal caso esteja logado
        if (this.authenticationService.currentUserTokenValue?.accessToken != undefined  )
        {
             this.isLoggedIn = true;

            this.router.navigate(['/']);
        }
    }
    
    get ctrl() { return this.loginForm.controls; }

    ngOnInit()
    {
        if (sessionStorage.getItem('currentUser')) {
            this.router.navigate(['/']);
        }

        this.loginForm = this.formBuilder.group(
        {
            userName: ['', Validators.required],
            password: ['', Validators.compose([Validators.required,Validators.minLength(4)])]
        });

    }

    onSubmit()
    {
        this.submitted = true;

       try {


                this.authenticationService.login(this.ctrl['userName'].value, this.ctrl['password'].value).subscribe((response: AccessToken) =>
                {
                    this.loading = false;

                    if (response.authenticated) {
                        
                        this.isLoggedIn = true;
                        
                        sessionStorage.setItem('toast', 'true');
                        sessionStorage.setItem('currentUser', JSON.stringify(response.userInfo));
                        sessionStorage.setItem('token', response.accessToken);

                        this.router.navigate(['/']);
                    }
                    else {
                        this.toastService.show('mensagem a verificar', { classname: 'bg-danger text-white', delay: 15000 });
                    }
                });
            }
            catch (error: any) {
            }
            finally{
                this.loading = false;
            }
    }

    /**
    * Password Hide/Show
    */
    toggleFieldTextType()
    {
        this.fieldTextType = !this.fieldTextType;
    }
}
