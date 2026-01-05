import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { SceneService } from '../../services/scene.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-admin-control',
  templateUrl: './admin-control.component.html',
  styleUrls: ['./admin-control.component.css']
})
export class AdminControlComponent implements OnInit {
  users: any[] = [];
  events: any[] = [];
  scenes: any[] = [];
  activeTab: string = 'users';
  errorMessage: string = '';

  constructor(
    private authService: AuthService,
    private eventService: EventService,
    private sceneService: SceneService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData() {
    this.loadUsers();
    this.loadEvents();
    this.loadScenes();
  }

  loadUsers() {
    this.authService.getAllUsers().subscribe({
      next: (data) => this.users = data,
      error: (err) => console.error('Erreur chargement utilisateurs', err)
    });
  }

  loadEvents() {
    this.eventService.getEvents().subscribe({
      next: (data) => this.events = data,
      error: (err) => console.error('Erreur chargement événements', err)
    });
  }

  loadScenes() {
    this.sceneService.getScenes().subscribe({
      next: (data) => this.scenes = data,
      error: (err) => console.error('Erreur chargement scènes', err)
    });
  }

  deleteUser(id: number) {
    if (confirm('Êtes-vous sûr de vouloir supprimer cet utilisateur ?')) {
      this.authService.deleteUser(id).subscribe({
        next: () => {
          this.users = this.users.filter(u => u.id !== id);
        },
        error: (err) => this.errorMessage = "Impossible de supprimer (il a peut-être des réservations)."
      });
    }
  }

  changeRole(user: any, newRole: string) {
    const updatedUser = { ...user, role: newRole };
    this.authService.updateUser(user.id, updatedUser).subscribe({
      next: () => {
        user.role = newRole;
      },
      error: (err) => this.errorMessage = "Erreur lors du changement de rôle."
    });
  }

  deleteEvent(id: number) {
    if (confirm('Supprimer cet événement ?')) {
      this.eventService.deleteEvent(id).subscribe({
        next: () => this.events = this.events.filter(e => e.id !== id),
        error: () => this.errorMessage = "Erreur suppression événement."
      });
    }
  }

  modifyEvent(id: number) {
    this.router.navigate(['/organiser/modify-event', id]);
  }

  deleteScene(id: number) {
    if (confirm('Supprimer cette scène ? Attention, cela échouera si des événements y sont liés.')) {
      this.sceneService.deleteScene(id).subscribe({
        next: () => this.scenes = this.scenes.filter(s => s.id !== id),
        error: () => this.errorMessage = "Impossible de supprimer (des événements utilisent cette salle)."
      });
    }
  }

  modifyScene(id: number) {
    this.router.navigate(['/organiser/modify-scene', id]);
  }
}
