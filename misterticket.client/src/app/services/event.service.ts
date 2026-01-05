import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EventDto } from '../models/event.model';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private baseUrl = '/api/Events';

  constructor(private http: HttpClient) { }

  getEvents(): Observable<EventDto[]> {
    return this.http.get<EventDto[]>(`${this.baseUrl}/getAll`);
  }

  createEvent(eventData: any, file?: File): Observable<any> {
    const formData = new FormData();
    formData.append('name', eventData.name);
    formData.append('description', eventData.description);
    formData.append('date', eventData.date);
    formData.append('sceneId', eventData.sceneId.toString());

    if (file) {
      formData.append('imageFile', file);
    }

    return this.http.post(`${this.baseUrl}/create`, formData);
  }

  getEventById(id: number): Observable<EventDto> {
    return this.http.get<EventDto>(`${this.baseUrl}/get_${id}`);
  }

  getEventSeats(eventId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${eventId}/seats`);
  }

  updateEvent(id: number, eventData: any, file?: File): Observable<any> {
    const formData = new FormData();
    formData.append('name', eventData.name);
    formData.append('description', eventData.description);
    formData.append('date', eventData.date);
    formData.append('sceneId', eventData.sceneId.toString());

    if (file) {
      formData.append('imageFile', file);
    }

    return this.http.put(`${this.baseUrl}/modify_${id}`, formData);
  }

  deleteEvent(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/delete_${id}`);
  }
}

export { EventDto };
