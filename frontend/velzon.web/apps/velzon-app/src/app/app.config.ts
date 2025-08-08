/* eslint-disable @typescript-eslint/no-explicit-any */
import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { appRoutes } from './app.routes';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { rootReducer } from './store';
//import { environment } from '@velzon.web/environments/dev';
import { AsyncPipe } from '@angular/common';
import { StoreModule } from '@ngrx/store';
import { ToastrModule } from 'ngx-toastr';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { NgPipesModule } from 'ngx-pipes';
//import { createTranslateLoader } from './app.module';
import { StoreDevtoolsModule } from '@ngrx/store-devtools';
import { ApplicationHttpClient, applicationHttpClientCreator } from './core/http-client';
import { AuthInterceptor, ErrorInterceptor, GlobalErrorHandler, JwtInterceptor } from './core/interceptors';
//import { provideAnimations } from '@angular/platform-browser/animations';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { environment } from '@velzon.web/environments/dev';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { AuthGuard } from './core/guards/auth-guard';



export function createTranslateLoader(http: HttpClient): any
{
    return new TranslateHttpLoader(http, 'assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    importProvidersFrom(BrowserModule,
      //LayoutsModule,
      //   GrupoAcessoComponent,
      AsyncPipe,
      ToastrModule.forRoot({ positionClass: 'toast-top-right' }),
      TranslateModule.forRoot({
        defaultLanguage: 'pt-br',
        loader: {
          provide: TranslateLoader,
          useFactory: (createTranslateLoader),
          deps: [HttpClient]
        }
      }),
      NgPipesModule,
      StoreModule.forRoot(rootReducer),
      StoreDevtoolsModule.instrument({
        maxAge: 25, // Retains last 25 states
        logOnly: environment.production, // Restrict extension to log-only mode
      })),
    provideRouter(appRoutes),
        // { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
         //  { provide: AuthGuard, useFactory: authInterceptorProviders, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: ApplicationHttpClient, useFactory: applicationHttpClientCreator, deps: [HttpClient] },
   // { provide: HTTP_INTERCEPTORS, useFactory: GlobalErrorHandler, multi: true },
    provideHttpClient(withInterceptorsFromDi()),
        //  provideAnimations(),
        provideAnimationsAsync(),
  ],
};
