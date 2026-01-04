// src/app/services/scene.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SceneDto } from '../models/scene.model';

@Injectable({ providedIn: 'root' })
export class SceneService {
  private apiUrl = '/api/Scenes';

  constructor(private http: HttpClient) { }

  getScenes(): Observable<SceneDto[]> {
    return this.http.get<SceneDto[]>(this.apiUrl);
  }

  createScene(scene: any): Observable<any> {
    // Les propriétés doivent correspondre au SceneDto (name, maxRows, maxColumns)
    return this.http.post(this.apiUrl, scene);
  }
}
