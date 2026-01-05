import { Component, OnInit } from '@angular/core';
import { EventService } from '../../services/event.service';
import { SceneService } from '../../services/scene.service';
import { SceneDto } from '../../models/scene.model';
import { EventDto } from '../../models/event.model';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  events: EventDto[] = [];
  scenes: SceneDto[] = [];
  loading = true;
  loadingScenes = true;

  isLoggedIn = false;
  userName = '';
  isAdmin = false;
  isOrganiser = false;

  constructor(
    private eventService: EventService,
    private sceneService: SceneService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.checkAuth();
    this.loadEvents();
    this.loadScenes();
  }

  checkAuth() {
    this.isLoggedIn = this.authService.isLoggedIn();
    if (this.isLoggedIn) {
      const user = this.authService.getUserInfo();
      this.userName = user?.name || 'Utilisateur';
      this.isAdmin = this.authService.isAdmin();
      this.isOrganiser = this.authService.isOrganiser();
    }
  }

  onLogout() {
    this.authService.logout();
    window.location.reload();
  }

  loadEvents(): void {
    this.eventService.getEvents().subscribe({
      next: (data) => {
        this.events = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur lors de la récupération des événements', err);
        this.loading = false;
      }
    });
  }

  loadScenes(): void {
    this.sceneService.getScenes().subscribe({
      next: (data) => {
        this.scenes = data;
        this.loadingScenes = false;
      },
      error: (err) => {
        console.error('Erreur salles', err);
        this.loadingScenes = false;
      }
    });
  }
}
