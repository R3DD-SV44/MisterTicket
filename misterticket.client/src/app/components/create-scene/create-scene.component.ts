import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { SceneService } from '../../services/scene.service';

@Component({
  selector: 'app-create-scene',
  templateUrl: './create-scene.component.html',
  styleUrls: ['./create-scene.component.css']
})
export class CreateSceneComponent {
  // Modèle correspondant au SceneDto du Backend
  scene = {
    name: '',
    maxRows: 1,
    maxColumns: 1
  };

  errorMessage = '';

  constructor(
    private sceneService: SceneService,
    private router: Router
  ) { }

  onCreate() {
    if (this.scene.maxRows <= 0 || this.scene.maxColumns <= 0) {
      this.errorMessage = "Les dimensions doivent être supérieures à 0.";
      return;
    }

    this.sceneService.createScene(this.scene).subscribe({
      next: () => {
        // Redirection vers la création d'événement une fois la scène prête
        this.router.navigate(['/organiser/create-event']);
      },
      error: (err) => {
        this.errorMessage = "Erreur lors de la création de la scène. Le nom est peut-être déjà pris.";
        console.error(err);
      }
    });
  }
}
