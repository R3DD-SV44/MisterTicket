import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ReservationService } from '../../services/reservation.service';

@Component({
  selector: 'app-payment',
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.css']
})
export class PaymentComponent implements OnInit {
  reservationId!: number;
  reservation: any = null;
  isLoading = true;
  isPaid = false;
  paymentProcessing = false;
  cardNumber: string = '';
  expiryDate: string = '';
  cvv: string = '';

  constructor(
    private route: ActivatedRoute,
    private reservationService: ReservationService,
    private router: Router
  ) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.reservationId = +idParam;
      this.loadReservation();
    }
  }

  loadReservation() {
    this.reservationService.getReservationById(this.reservationId).subscribe({
      next: (res) => {
        this.reservation = res;
        if (res.status === 2) {
          this.isPaid = true;
        }
        this.isLoading = false;
      },
      error: () => {
        alert("Réservation introuvable");
        this.router.navigate(['/']);
      }
    });
  }

  calculateTotal(): number {
    if (!this.reservation || !this.reservation.selectedSeats) {
      return 0;
    }
    return this.reservation.selectedSeats.reduce((total: number, seat: any) => total + seat.price, 0);
  }

  processPayment() {
    if (!this.cardNumber || this.cardNumber.length < 10 || !this.expiryDate || !this.cvv) {
      alert("Veuillez saisir des informations de carte bancaire (fictives) valides.");
      return;
    }

    this.paymentProcessing = true;

    setTimeout(() => {
      this.reservationService.payReservation(this.reservationId).subscribe({
        next: () => {
          this.isPaid = true;
          this.paymentProcessing = false;
        },
        error: (err) => {
          console.error(err);
          alert("Erreur lors du paiement");
          this.paymentProcessing = false;
        }
      });
    }, 1500);
  }

  cancelPayment() {
    if (confirm("Voulez-vous vraiment annuler la commande et libérer les sièges ?")) {
      this.isLoading = true;
      this.reservationService.cancelReservation(this.reservationId).subscribe({
        next: () => {
          this.router.navigate(['/']);
        },
        error: (err) => {
          console.error(err);
          alert("Erreur lors de l'annulation");
          this.isLoading = false;
        }
      });
    }
  }

  download(type: 'pdf' | 'qrcode' | 'zip') {
    this.reservationService.downloadTicket(this.reservationId, type).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        const extension = type === 'qrcode' ? 'png' : type;
        link.download = `MisterTicket_${this.reservationId}.${extension}`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error("Erreur téléchargement", err);
        alert("Impossible de télécharger le fichier. Vérifiez que vous êtes bien connecté.");
      }
    });
  }
}
