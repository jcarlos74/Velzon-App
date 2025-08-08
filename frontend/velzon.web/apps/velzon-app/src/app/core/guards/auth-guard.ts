import { Injectable } from "@angular/core";
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { AuthenticationService } from "../services/authentication.service";

@Injectable({ providedIn: 'root' })
export class AuthGuard
{
    constructor(
        private router: Router,
        private authenticationService: AuthenticationService
    ) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot)
    {
        const currentUser = this.authenticationService.currentUserTokenValue;
        if (currentUser.accessToken !== undefined) {
            // logged in so return true
            return true;
        }
        // check if user data is in storage is logged in via API.
        if (sessionStorage.getItem('currentUser')) {
            return true;
        }

        this.router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });

        return false;
    }


}

