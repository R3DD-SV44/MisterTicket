import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { DashboardService, EventStats } from '../../services/dashboard.service';
import { EventService } from '../../services/event.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  stats: EventStats | null = null;
  eventName: string = 'Chargement...';
  isLoading = true;
  errorMessage = '';

  private hubConnection?: HubConnection;
  private eventId!: number;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dashboardService: DashboardService,
    private eventService: EventService
  ) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.eventId = +idParam;
      this.loadData(this.eventId);
      this.initSignalR();
    }
  }

  ngOnDestroy(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  loadData(id: number) {
    if (!this.stats) {
      this.isLoading = true;
    }

    this.eventService.getEventById(id).subscribe({
      next: (event) => this.eventName = event.name,
      error: () => this.eventName = 'Événement inconnu'
    });

    this.dashboardService.getEventStats(id).subscribe({
      next: (data) => {
        this.stats = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = "Impossible de charger les statistiques.";
        this.isLoading = false;
      }
    });
  }

  private initSignalR() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('https://localhost:7229/ticketHub')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('Dashboard connecté au Hub SignalR'))
      .catch((err: any) => console.error('Erreur connexion SignalR Dashboard:', err));

    this.hubConnection.on('ReceiveSeatStatusUpdate', (eventId: number, seatId: number, statusIdx: number) => {
      if (eventId === this.eventId) {
        this.loadData(this.eventId);
      }
    });
  }

  goBack() {
    this.router.navigate(['/events']);
  }
}
