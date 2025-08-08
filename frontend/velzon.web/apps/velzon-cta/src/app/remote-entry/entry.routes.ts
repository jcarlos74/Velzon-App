import { Route } from '@angular/router';
//import { RemoteEntryComponent } from './entry.component';
import { GrupoAcessoComponent } from '../grupo-acesso/grupo-acesso.component';
import { UsuariosComponent } from '../usuarios/usuarios.component';

export const remoteRoutes: Route[] = [
 // { path: '', component: RemoteEntryComponent },
  { path: 'cta/grupo-acesso', loadComponent: ()=> GrupoAcessoComponent },
  { path: 'cta/usuarios', loadComponent: ()=> UsuariosComponent },
];
