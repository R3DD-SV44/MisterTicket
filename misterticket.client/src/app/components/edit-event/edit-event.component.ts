import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EventService } from '../../services/event.service';
import { SceneService } from '../../services/scene.service';
import { EventDto } from '../../models/event.model';
import { SceneDto } from '../../models/scene.model';

@Component({
  selector: 'app-edit-event',
  templateUrl: './edit-event.component.html',
  styleUrls: ['./edit-event.component.css']
})
export class EditEventComponent implements OnInit {
  event: EventDto = { id: 0, name: '', description: '', date: new Date().toISOString(), sceneId: 0, imageUrl: '' };
  scenes: SceneDto[] = [];
  loading = true;
  errorMessage = '';
  selectedFile: File | null = null;
  imagePreview: string | null = null;

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
    this.sceneService.getScenes().subscribe(scenes => this.scenes = scenes);

    this.eventService.getEventById(id).subscribe({
      next: (data) => {
        this.event = data;
        this.imagePreview = this.event.imageUrl || null;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = "Impossible de charger l'événement.";
        this.loading = false;
      }
    });
  }

  onUpdate(): void {
    this.loading = true;
    this.eventService.updateEvent(this.event.id, this.event, this.selectedFile || undefined).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        this.errorMessage = "Erreur lors de la mise à jour.";
        this.loading = false;
      }
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

  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0];
    if (this.selectedFile) {
      const reader = new FileReader();
      reader.onload = (e: any) => this.imagePreview = e.target.result;
      reader.readAsDataURL(this.selectedFile);
    }
  }
}
