import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
// VÉRIFIEZ BIEN LE "export" ICI
export class RoleGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const expectedRole = route.data['expectedRole'];
    const user = this.authService.getUserInfo();

    if (user && user.role === expectedRole) {
      return true;
    }

    this.router.navigate(['/auth/login']);
    return false;
  }
}
