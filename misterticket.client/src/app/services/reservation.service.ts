import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ReservationService {
  private apiUrl = '/api/Reservations';

  constructor(private http: HttpClient) { }

  createReservation(reservationData: { eventId: number, seatIds: number[] }): Observable<any> {
    return this.http.post(`${this.apiUrl}/confirm`, reservationData);
  }

  getMyReservations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/my-reservations`);
  }

  cancelReservation(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/cancel`, {});
  }

  getReservationById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  payReservation(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/pay`, {});
  }

  downloadTicket(reservationId: number, type: 'pdf' | 'qrcode' | 'zip'): Observable<Blob> {
    return this.http.get(`/api/Tickets/${reservationId}/${type}`, {
      responseType: 'blob'
    });
  }
}
