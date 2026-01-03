import { Component, OnInit } from '@angular/core';
import { EventService } from '../../services/event.service';
// Assure-toi que l'interface Event existe ou adapte selon ton DTO
import { Event } from '../../models/event.model';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  events: any[] = []; // Utilise EventDto[] si défini
  loading = true;

  isLoggedIn = false;
  userName = '';
  isAdmin = false;
  isOrganiser = false;

  constructor(private eventService: EventService, private authService: AuthService) { }

  ngOnInit(): void {
    this.checkAuth();
    this.loadEvents();
  }

  checkAuth() {
    this.isLoggedIn = this.authService.isLoggedIn();
    if (this.isLoggedIn) {
      const user = this.authService.getUserInfo();
      this.userName = user?.name || 'Utilisateur';
      this.isAdmin = this.authService.isAdmin();
    }
  }

  onLogout() {
    this.authService.logout();
    window.location.reload(); // Simple pour rafraîchir l'état de l'interface
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
}
