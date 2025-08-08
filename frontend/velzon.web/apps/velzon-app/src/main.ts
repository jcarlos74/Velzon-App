import { setRemoteDefinitions } from '@nx/angular/mf';
import '@angular/localize/init';
//import { registerLicense } from '@syncfusion/ej2-base';

// Registering Syncfusion license key
//registerLicense('NjA1NkAzMjM2MkUzMTJFMzliSzVTQlJKN0NLVzNVOFVKSlErcVEzYW9PSkZ2dUhicHliVjkrMncxdHpRPQ==');

fetch('/assets/module-federation.manifest.json')
  .then((res) => res.json())
  .then((definitions) => setRemoteDefinitions(definitions))
  .then(() => import('./bootstrap').catch((err) => console.error(err)));
