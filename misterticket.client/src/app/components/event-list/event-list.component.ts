// src/app/components/event-list/event-list.component.ts
import { Component, OnInit } from '@angular/core';
import { EventService } from '../../services/event.service';
import { EventDto } from '../../models/event.model'; // Importez bien le DTO

@Component({
  selector: 'app-event-list',
  templateUrl: './event-list.component.html'
})
export class EventListComponent implements OnInit {
  events: EventDto[] = []; // Utilisez EventDto ici

  constructor(private eventService: EventService) { }

  ngOnInit(): void {
    this.eventService.getEvents().subscribe({
      next: (data) => this.events = data,
      error: (err) => console.error(err)
    });
  }
}
