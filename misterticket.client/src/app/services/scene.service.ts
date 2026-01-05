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

  getSceneById(id: number): Observable<SceneDto> {
    return this.http.get<SceneDto>(`${this.apiUrl}/${id}`);
  }

  updateScene(id: number, sceneData: any, file?: File): Observable<any> {
    const formData = new FormData();
    formData.append('id', id.toString());
    formData.append('name', sceneData.name);
    formData.append('maxRows', sceneData.maxRows.toString());
    formData.append('maxColumns', sceneData.maxColumns.toString());
    if (file) {
      formData.append('imageFile', file);
    }
    return this.http.put(`${this.apiUrl}/${id}`, formData);
  }

  getLayout(id: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/${id}/layout`);
  }

  createScene(sceneData: any, file?: File): Observable<any> {
    const formData = new FormData();
    formData.append('name', sceneData.name);
    formData.append('maxRows', sceneData.maxRows.toString());
    formData.append('maxColumns', sceneData.maxColumns.toString());
    if (file) {
      formData.append('imageFile', file);
    }

    return this.http.post(this.apiUrl, formData);
  }

  updateSeatPrices(sceneId: number, updates: any[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/${sceneId}/seats/update-prices`, updates);
  }

  deleteScene(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  createPriceZone(priceZone: any): Observable<any> {
    const correctUrl = this.apiUrl.replace('/Scenes', '/PriceZone') + '/create';
    return this.http.post<any>(correctUrl, priceZone);
  }
}
