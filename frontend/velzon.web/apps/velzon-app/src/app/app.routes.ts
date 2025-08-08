import { Route, Routes } from '@angular/router';
import { LayoutComponent } from './layouts/layout.component';
import { loadRemoteModule } from '@nx/angular/mf';
import { AuthGuard } from './core/guards/auth-guard';

export const appRoutes: Routes = [

    { path: '', component: LayoutComponent, canActivate: [AuthGuard]},
    { path: 'auth', loadChildren: () => import('./features/seguranca/seguranca.routing').then((x) => x.SegurancaRoutes)  },

];


/*export const appRoutes: Route[] = [
    {
        path: '', component: LayoutComponent,
        children:
            [
                { path: '', loadChildren: () => loadRemoteModule('velzon-cta', './Routes').then((m) => m.remoteRoutes) }
            ],
    },
     { path: 'auth', loadChildren: () => import('./features/seguranca/seguranca.routing').then((x) => x.SegurancaRoutes) },
    { path: '', component: LayoutComponent, canActivate: [AuthGuard]},// Aplica o guard a esta rota

];*/
