// src/app/services/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/Auth'; // Route de base de votre AuthController

  constructor(private http: HttpClient) { }

  // Inscription
  register(userData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, userData);
  }

  // Connexion
  login(credentials: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, credentials, { responseType: 'text' as 'json' })
      .pipe(
        tap((token: any) => {
          // On stocke le token JWT dans le navigateur
          localStorage.setItem('token', token);
        })
      );
  }

  // Vérifier si l'utilisateur est connecté
  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  // Déconnexion
  logout() {
    localStorage.removeItem('token');
  }

  getUserInfo() {
    const token = localStorage.getItem('token');
    if (!token) return null;

    try {
      // Le token est composé de 3 parties séparées par des points. La 2ème contient les données.
      const payload = JSON.parse(atob(token.split('.')[1]));

      // Les clés dans le token généré par .NET sont souvent des URLs de schémas
      return {
        name: payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
        role: payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
      };
    } catch (e) {
      return null;
    }
  }

  isAdmin(): boolean {
    const user = this.getUserInfo();
    return user?.role === 'Admin';
  }
}
