import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/Auth';

  constructor(private http: HttpClient) { }

  register(userData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, userData);
  }

  login(credentials: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, credentials)
      .pipe(
        tap((response: any) => {
          const tokenValue = response.token ? response.token : response;
          localStorage.setItem('token', tokenValue);
        })
      );
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  logout() {
    localStorage.removeItem('token');
  }

  getUserInfo() {
    const token = localStorage.getItem('token');
    if (!token) return null;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        name: payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
        role: payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
      };
    } catch (e) {
      return null;
    }
  }

  getAllUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/users`);
  }

  updateUser(id: number, userData: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/users/${id}`, userData);
  }

  deleteUser(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  isAdmin(): boolean {
    const user = this.getUserInfo();
    return user?.role === 'Admin';
  }

  isOrganiser(): boolean {
    const user = this.getUserInfo();
    return user?.role === 'Organiser';
  }
}
