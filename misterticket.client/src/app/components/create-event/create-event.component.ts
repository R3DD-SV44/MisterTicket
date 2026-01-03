// src/app/components/create-event/create-event.component.ts
import { Component, OnInit } from '@angular/core';
import { SceneService } from '../../services/scene.service';
import { EventService } from '../../services/event.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-event',
  templateUrl: './create-event.component.html'
})
export class CreateEventComponent implements OnInit {
  scenes: any[] = [];
  event = { name: '', description: '', date: '', sceneId: 0 };

  constructor(private sceneService: SceneService, private eventService: EventService, private router: Router) { }

  ngOnInit() {
    this.sceneService.getScenes().subscribe(data => this.scenes = data);
  }

  onCreate() {
    this.eventService.createEvent(this.event).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => console.error("Erreur de création", err)
    });
  }
}
