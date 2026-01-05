import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SceneService } from '../../services/scene.service';

@Component({
  selector: 'app-edit-scene',
  templateUrl: './edit-scene.component.html',
  styleUrls: ['./edit-scene.component.css']
})
export class EditSceneComponent implements OnInit {
  scene: any = { name: '', maxRows: 0, maxColumns: 0 };
  priceZones: any[] = [];
  selectedPriceZoneId: number | null | undefined = undefined;
  seatGrid: any[][] = [];
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  loading = true;
  errorMessage = '';

  newZone = {
    name: '',
    price: 0,
    colorHex: '#ff0000',
    sceneId: 0
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private sceneService: SceneService
  ) { }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loadData(id);
  }

  loadData(id: number) {
    this.sceneService.getLayout(id).subscribe({
      next: (data) => {
        this.scene = data;
        this.priceZones = data.priceZones || [];
        this.imagePreview = data.imageUrl;
        this.initializeGrid();
        this.loading = false;
      },
      error: () => {
        this.errorMessage = "Erreur lors du chargement des données.";
        this.loading = false;
      }
    });
  }

  initializeGrid() {
    this.seatGrid = Array(this.scene.maxRows).fill(null)
      .map(() => Array(this.scene.maxColumns).fill(null));

    if (this.scene.seats) {
      this.scene.seats.forEach((seat: any) => {
        if (seat.row <= this.scene.maxRows && seat.column <= this.scene.maxColumns) {
          this.seatGrid[seat.row - 1][seat.column - 1] = { ...seat };
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

  applyPriceZone(r: number, c: number) {
    if (this.selectedPriceZoneId === undefined) {
      alert("Veuillez d'abord sélectionner une zone de prix.");
      return;
    }

    if (!this.seatGrid[r][c]) {
      this.seatGrid[r][c] = { row: r + 1, column: c + 1 };
    }

    this.seatGrid[r][c].priceZoneId = this.selectedPriceZoneId;
  }

  onCreateZone() {
    if (!this.newZone.name || this.newZone.price <= 0) {
      alert("Veuillez saisir un nom et un prix valide.");
      return;
    }

    this.newZone.sceneId = this.scene.id;

    this.sceneService.createPriceZone(this.newZone).subscribe({
      next: (createdZone) => {
        this.priceZones.push(createdZone);
        this.selectedPriceZoneId = createdZone.id;
        this.newZone = { name: '', price: 0, colorHex: '#ff0000', sceneId: this.scene.id };
      },
      error: () => alert("Erreur lors de la création de la zone.")
    });
  }

  getZoneColor(priceZoneId: number): string {
    const zone = this.priceZones.find(z => z.id === priceZoneId);
    return zone ? zone.colorHex : '#333';
  }

  onDelete() {
    if (confirm(`Êtes-vous sûr de vouloir supprimer la salle "${this.scene.name}" ? Cette action est irréversible.`)) {
      this.loading = true;
      this.sceneService.deleteScene(this.scene.id).subscribe({
        next: () => {
          this.loading = false;
          alert("Salle supprimée avec succès.");
          this.router.navigate(['/']);
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = err.error?.message || "Erreur lors de la suppression de la salle.";
          console.error("Erreur suppression:", err);
        }
      });
    }
  }

  onSave() {
    this.loading = true;

    this.sceneService.updateScene(this.scene.id, this.scene, this.selectedFile || undefined).subscribe({
      next: () => {
        const seatUpdates = [];
        for (let r = 0; r < this.scene.maxRows; r++) {
          for (let c = 0; c < this.scene.maxColumns; c++) {
            const seat = this.seatGrid[r] ? this.seatGrid[r][c] : null;

            seatUpdates.push({
              row: r + 1,
              column: c + 1,
              priceZoneId: seat ? seat.priceZoneId : null
            });
          }
        }

        this.sceneService.updateSeatPrices(this.scene.id, seatUpdates).subscribe({
          next: () => {
            this.loading = false;
            this.router.navigate(['/']);
          },
          error: (err) => {
            console.error("Erreur sièges:", err);
            this.errorMessage = "Les dimensions ont été mises à jour, mais une erreur est survenue lors de la configuration des sièges.";
            this.loading = false;
          }
        });
      },
      error: (err) => {
        console.error("Erreur scène:", err);
        this.errorMessage = "Erreur lors de la mise à jour des informations de la salle.";
        this.loading = false;
      }
    });
  }

  onGridSizeChange(): void {
    const newRows = this.scene.maxRows;
    const newCols = this.scene.maxColumns;

    const newGrid: any[][] = [];

    for (let r = 0; r < newRows; r++) {
      const row = [];
      for (let c = 0; c < newCols; c++) {
        if (this.seatGrid[r] && this.seatGrid[r][c]) {
          row.push(this.seatGrid[r][c]);
        } else {
          row.push({
            row: r,
            column: c,
            priceZoneId: null
          });
        }
      }
      newGrid.push(row);
    }

    this.seatGrid = newGrid;
  }
}
