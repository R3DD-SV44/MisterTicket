// src/app/components/create-event/create-event.component.ts
import { Component, OnInit } from '@angular/core';
import { SceneService } from '../../services/scene.service';
import { EventService } from '../../services/event.service';
import { Router } from '@angular/router';
import { SceneDto } from '../../models/scene.model';

@Component({
  selector: 'app-create-event',
  templateUrl: './create-event.component.html',
  styleUrls: ['./create-event.component.css']
})
export class CreateEventComponent implements OnInit {
  scenes: SceneDto[] = [];
  event = { name: '', description: '', date: '', sceneId: 0 };
  loading = true;
  errorMessage = '';


  constructor(private sceneService: SceneService, private eventService: EventService, private router: Router) { }

  ngOnInit() {
    this.loadScenes();
  }

  loadScenes() {
    this.sceneService.getScenes().subscribe({
      next: (data) => {
        this.scenes = data;
        this.loading = false;
        if (this.scenes.length === 0) {
          this.errorMessage = "Aucune scène n'est configurée. Veuillez en créer une d'abord.";
        }
      },
      error: (err) => {
        console.error("Erreur lors du chargement des scènes", err);
        this.errorMessage = "Impossible de charger les scènes.";
        this.loading = false;
      }
    });
  }

  onCreate() {
    if (this.event.sceneId === 0) {
      this.errorMessage = "Veuillez sélectionner une scène.";
      return;
    }

    this.eventService.createEvent(this.event).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        console.error("Erreur de création", err);
        this.errorMessage = "Une erreur est survenue lors de la création de l'événement.";
      }
    });
  }
}
