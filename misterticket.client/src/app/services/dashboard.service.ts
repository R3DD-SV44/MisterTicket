import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface EventStats {
  paid: number;
  reserved: number;
  free: number;
  revenue: number;
  fillingRate: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private baseUrl = '/api/Dashboard';

  constructor(private http: HttpClient) { }

  getEventStats(eventId: number): Observable<EventStats> {
    return this.http.get<EventStats>(`${this.baseUrl}/${eventId}_event_stats`);
  }
}
