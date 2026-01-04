// src/app/components/create-scene/create-scene.component.ts
import { Component } from '@angular/core';
import { SceneService } from '../../services/scene.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-scene',
  templateUrl: './create-scene.component.html',
  styleUrls: ['./create-scene.component.css'] // Réutilisez le style auth-container/night-theme
})
export class CreateSceneComponent {
  // Modèle basé sur SceneDto du backend
  scene = { name: '', maxRows: 1, maxColumns: 1 };
  errorMessage = '';
  successMessage = '';

  constructor(private sceneService: SceneService, private router: Router) { }

  onSubmit() {
    this.errorMessage = '';

    // Validation simple
    if (this.scene.maxRows <= 0 || this.scene.maxColumns <= 0) {
      this.errorMessage = "Les dimensions doivent être supérieures à 0.";
      return;
    }

    this.sceneService.createScene(this.scene).subscribe({
      next: () => {
        this.successMessage = "Scène créée avec succès ! Redirection...";
        // Redirection après un court délai pour laisser l'utilisateur voir le message
        setTimeout(() => this.router.navigate(['/organiser/create-event']), 1500);
      },
      error: (err) => {
        console.error("Erreur de création de scène", err);
        this.errorMessage = err.error?.message || "Une erreur est survenue lors de la création.";
      }
    });
  }
}
