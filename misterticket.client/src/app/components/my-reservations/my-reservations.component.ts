import { Component, OnInit } from '@angular/core';
import { ReservationService } from '../../services/reservation.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-my-reservations',
  templateUrl: './my-reservations.component.html',
  styleUrls: ['./my-reservations.component.css']
})
export class MyReservationsComponent implements OnInit {
  reservations: any[] = [];
  isLoading = true;

  constructor(
    private reservationService: ReservationService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadReservations();
  }

  loadReservations() {
    this.reservationService.getMyReservations().subscribe({
      next: (res) => {
        this.reservations = res;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  getStatusLabel(status: number): string {
    switch (status) {
      case 1: return 'En attente de paiement';
      case 2: return 'Confirmé (Payé)';
      case 3: return 'Annulé';
      default: return 'Inconnu';
    }
  }

  goToPayment(id: number) {
    this.router.navigate(['/payment', id]);
  }

  downloadTicket(id: number, type: 'pdf' | 'qrcode' | 'zip') {
    this.reservationService.downloadTicket(id, type).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Ticket_${id}.${type === 'qrcode' ? 'png' : type}`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => alert("Erreur lors du téléchargement")
    });
  }
}
