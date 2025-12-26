import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Cette interface doit correspondre à votre modèle C# Event.cs
export interface Event {
  id: number;
  name: string;
  date: string;
  description: string;
  sceneId: number;
  // Ajoutez d'autres champs si nécessaire selon votre backend
}

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private apiUrl = '/api/events'; // L'URL de votre EventsController

  constructor(private http: HttpClient) { }

  getEvents(): Observable<Event[]> {
    return this.http.get<Event[]>(this.apiUrl);
  }
}
