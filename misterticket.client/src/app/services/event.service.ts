import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EventDto } from '../models/event.model'; // Utilise le nouveau nom

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private baseUrl = '/api/Events';

  constructor(private http: HttpClient) { }

  getEvents(): Observable<EventDto[]> {
    return this.http.get<EventDto[]>(`${this.baseUrl}/getAll`);
  }

  createEvent(eventData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/create`, eventData);
  }

  getEventById(id: number): Observable<EventDto> {
    return this.http.get<EventDto>(`${this.baseUrl}/get_${id}`);
  }

  updateEvent(id: number, eventData: EventDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/modify_${id}`, eventData);
  }

  deleteEvent(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/delete_${id}`);
  }
}

export { EventDto };
