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
  event = { name: '', description: '', date: '', sceneId: 0, imageUrl: '' };
  loading = true;
  errorMessage = '';
  selectedFile: File | null = null;

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

  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0];
  }

  onCreate() {
    this.loading = true;
    this.eventService.createEvent(this.event, this.selectedFile || undefined).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.loading = false;
        this.errorMessage = "Erreur lors de la création.";
      }
    });
  }
}
