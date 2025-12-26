import { Component, OnInit } from '@angular/core';
import { EventService, Event } from '../../services/event.service';

@Component({
  selector: 'app-event-list',
  templateUrl: './event-list.component.html',
  styleUrls: ['./event-list.component.css']
})
export class EventListComponent implements OnInit {
  events: Event[] = [];

  constructor(private eventService: EventService) { }

  ngOnInit(): void {
    this.eventService.getEvents().subscribe({
      next: (data) => this.events = data,
      error: (err) => console.error('Erreur lors de la récupération des événements', err)
    });
  }
}
