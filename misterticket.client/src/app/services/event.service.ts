// misterticket.client/src/app/services/event.service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Event } from '../models/event.model';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  // L'URL de base pour le contrôleur Events
  private baseUrl = '/api/Events';

  constructor(private http: HttpClient) { }

  // Récupérer tous les événements (utilisé par la page Home)
  getEvents(): Observable<Event[]> {
    return this.http.get<Event[]>(`${this.baseUrl}/getAll`);
  }

  // Créer un nouvel événement (utilisé par l'Organisateur)
  createEvent(eventData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/create`, eventData);
  }

  // Optionnel : Récupérer un événement par son ID (pour la future page de détails)
  getEventById(id: number): Observable<Event> {
    return this.http.get<Event>(`${this.baseUrl}/${id}`);
  }
}
