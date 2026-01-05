import { Component, OnInit } from '@angular/core';
import { SceneService } from '../../services/scene.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-scene',
  templateUrl: './create-scene.component.html',
  styleUrls: ['./create-scene.component.css']
})
export class CreateSceneComponent implements OnInit {
  scene = { name: '', maxRows: 5, maxColumns: 10 };
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  seatGrid: any[][] = [];
  loading = false;
  errorMessage = '';

  constructor(private sceneService: SceneService, private router: Router) { }

  ngOnInit(): void {
    this.onGridSizeChange();
  }

  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0];
    if (this.selectedFile) {
      const reader = new FileReader();
      reader.onload = (e: any) => this.imagePreview = e.target.result;
      reader.readAsDataURL(this.selectedFile);
    }
  }

  onGridSizeChange(): void {
    this.seatGrid = Array(this.scene.maxRows).fill(null)
      .map(() => Array(this.scene.maxColumns).fill(null));
  }

  onSubmit() {
    this.loading = true;
    this.sceneService.createScene(this.scene, this.selectedFile || undefined).subscribe({
      next: (createdScene) => {
        this.loading = false;
        this.router.navigate(['/organiser/modify-scene', createdScene.id]);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || "Erreur lors de la création.";
      }
    });
  }
}
