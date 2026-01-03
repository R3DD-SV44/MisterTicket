import { Component, OnInit } from '@angular/core';
import { EventService } from '../../services/event.service';
// Assure-toi que l'interface Event existe ou adapte selon ton DTO
import { EventDto } from '../../models/event.model';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  events: any[] = []; // Utilise EventDto[] si défini
  loading: boolean = true;

  constructor(private eventService: EventService) { }

  ngOnInit(): void {
    this.loadEvents();
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
