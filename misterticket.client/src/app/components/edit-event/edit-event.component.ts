// src/app/components/edit-event/edit-event.component.ts
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EventService } from '../../services/event.service';
import { SceneService } from '../../services/scene.service';
import { EventDto } from '../../models/event.model';
import { SceneDto } from '../../models/scene.model';

@Component({
  selector: 'app-edit-event',
  templateUrl: './edit-event.component.html',
  styleUrls: ['./edit-event.component.css'] // Réutilise les styles de create-event
})
export class EditEventComponent implements OnInit {
  event: EventDto = { id: 0, name: '', description: '', date: new Date().toISOString(), sceneId: 0 };
  scenes: SceneDto[] = [];
  loading = true;
  errorMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private eventService: EventService,
    private sceneService: SceneService
  ) { }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loadData(id);
  }

  loadData(id: number): void {
    // Charger les scènes pour la liste déroulante
    this.sceneService.getScenes().subscribe(scenes => this.scenes = scenes);

    // Charger l'événement actuel
    this.eventService.getEventById(id).subscribe({
      next: (data) => {
        this.event = data;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = "Impossible de charger l'événement.";
        this.loading = false;
      }
    });
  }

  onUpdate(): void {
    this.eventService.updateEvent(this.event.id, this.event).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => this.errorMessage = "Erreur lors de la mise à jour."
    });
  }

  onDelete(): void {
    if (confirm("Êtes-vous sûr de vouloir supprimer cet événement ? Cette action est irréversible.")) {
      this.eventService.deleteEvent(this.event.id).subscribe({
        next: () => {
          alert("Événement supprimé avec succès.");
          this.router.navigate(['/']);
        },
        error: (err) => {
          console.error("Erreur lors de la suppression", err);
          this.errorMessage = "Impossible de supprimer l'événement. Vérifiez s'il y a des réservations en cours.";
        }
      });
    }
  }
}
