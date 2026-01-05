import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const user = this.authService.getUserInfo();
    const expectedRole = route.data['expectedRole'];
    const allowedRoles = route.data['roles'] as Array<string>;

    if (user) {
      if (allowedRoles && allowedRoles.includes(user.role)) {
        return true;
      }
      if (expectedRole && user.role === expectedRole) {
        return true;
      }
    }

    this.router.navigate(['/auth/login']);
    return false;
  }
}
