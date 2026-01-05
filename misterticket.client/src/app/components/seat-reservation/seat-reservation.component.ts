import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { SceneService } from '../../services/scene.service';
import { ReservationService } from '../../services/reservation.service';

@Component({
  selector: 'app-seat-reservation',
  templateUrl: './seat-reservation.component.html',
  styleUrls: ['./seat-reservation.component.css']
})
export class SeatReservationComponent implements OnInit, OnDestroy {
  event: any = null;
  scene: any = null;
  seatGrid: any[][] = [];
  selectedSeats: any[] = [];
  priceZones: any[] = [];
  loading = true;
  errorMessage = '';
  seatStatusMap: { [key: number]: string } = {};
  private hubConnection?: HubConnection;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private eventService: EventService,
    private sceneService: SceneService,
    private reservationService: ReservationService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    const eventId = Number(this.route.snapshot.paramMap.get('id'));
    this.initSignalR();
    this.loadEventData(eventId);
  }

  ngOnDestroy(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  private initSignalR() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('https://localhost:7229/ticketHub')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('SignalR Connected'))
      .catch((err: any) => console.error('Error connecting to SignalR:', err));

    this.hubConnection.on('ReceiveSeatStatusUpdate', (eventId: number, seatId: number, statusIdx: number) => {
      if (this.event && this.event.id === eventId) {
        this.updateSeatStatus(seatId, statusIdx);
      }
    });
  }

  private updateSeatStatus(seatId: number, statusIdx: number) {
    if (statusIdx === 0) {
      delete this.seatStatusMap[seatId];
    } else {
      const statusStr = statusIdx === 1 ? 'ReservedTemp' : 'Paid';
      this.seatStatusMap[seatId] = statusStr;

      const index = this.selectedSeats.findIndex(s => s.id === seatId);
      if (index > -1) {
        this.selectedSeats.splice(index, 1);
      }
    }
  }

  loadEventData(eventId: number) {
    this.eventService.getEventById(eventId).subscribe({
      next: (eventData) => {
        this.event = eventData;
        this.loadLayout(eventData.sceneId);
      },
      error: () => this.handleError("Erreur lors du chargement de l'événement.")
    });
  }

  loadLayout(sceneId: number) {
    this.sceneService.getLayout(sceneId).subscribe({
      next: (data) => {
        this.scene = data;
        this.priceZones = data.priceZones || [];
        this.initializeGrid();
        this.loadSeatStatuses(this.event.id);
      },
      error: () => this.handleError("Erreur lors du chargement du plan de salle.")
    });
  }

  loadSeatStatuses(eventId: number) {
    (this.eventService as any).getEventSeats(eventId).subscribe({
      next: (occupiedSeats: any[]) => {
        occupiedSeats.forEach(es => {
          this.updateSeatStatus(es.seatId, es.status);
        });
        this.loading = false;
      },
      error: (err: any) => {
        console.error("Impossible de charger l'état des sièges", err);
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

  selectSeat(r: number, c: number) {
    const seat = this.seatGrid[r][c];

    if (!seat || !seat.priceZoneId || this.isOccupied(seat)) return;

    const index = this.selectedSeats.findIndex(s => s.id === seat.id);

    if (index > -1) {
      this.selectedSeats.splice(index, 1);
    } else {
      this.selectedSeats.push(seat);
    }
  }

  isSelected(seat: any): boolean {
    return seat && this.selectedSeats.some(s => s.id === seat.id);
  }

  isOccupied(seat: any): boolean {
    if (!seat) return false;
    return this.seatStatusMap.hasOwnProperty(seat.id);
  }

  getZoneColor(priceZoneId: number): string {
    const zone = this.priceZones.find(z => z.id === priceZoneId);
    return zone ? zone.colorHex : 'transparent';
  }

  getTotalPrice(): number {
    return this.selectedSeats.reduce((sum, seat) => {
      const zone = this.priceZones.find(z => z.id === seat.priceZoneId);
      return sum + (zone ? zone.price : 0);
    }, 0);
  }

  onReserve() {
    if (!this.authService.isLoggedIn()) {
      alert("Vous devez être connecté pour réserver une place.");
      this.router.navigate(['/auth', 'login']);
      return;
    }

    if (this.selectedSeats.length === 0) return;

    this.loading = true;
    const reservationData = {
      eventId: this.event.id,
      seatIds: this.selectedSeats.map(s => s.id)
    };

    this.reservationService.createReservation(reservationData).subscribe({
      next: (res) => {
        this.loading = false;
        alert(`Réservation de ${this.selectedSeats.length} siège(s) créée ! Total: ${res.total}€`);
        this.router.navigate(['/payment', res.reservationId]);
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 409) {
          this.errorMessage = "Un ou plusieurs sièges viennent d'être réservés par quelqu'un d'autre.";
          this.loadSeatStatuses(this.event.id);
          this.selectedSeats = [];
        } else {
          this.errorMessage = err.error?.message || "Erreur lors de la réservation.";
        }
      }
    });
  }

  getSeatStatus(seatId: number): string | null {
    return this.seatStatusMap[seatId] || null;
  }

  private handleError(msg: string) {
    this.errorMessage = msg;
    this.loading = false;
  }
}
