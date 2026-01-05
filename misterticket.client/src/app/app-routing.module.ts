import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminControlComponent } from './components/admin-control/admin-control.component';
import { AuthenticationComponent } from './components/authentication/authentication.component';
import { CreateEventComponent } from './components/create-event/create-event.component';
import { CreateSceneComponent } from './components/create-scene/create-scene.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { EditEventComponent } from './components/edit-event/edit-event.component';
import { EditSceneComponent } from './components/edit-scene/edit-scene.component';
import { HomeComponent } from './components/home/home.component';
import { MyReservationsComponent } from './components/my-reservations/my-reservations.component';
import { PaymentComponent } from './components/payment/payment.component';
import { SeatReservationComponent } from './components/seat-reservation/seat-reservation.component';
import { RoleGuard } from './guards/role.guard';

const routes: Routes = [
  {
    path: '',
    component: HomeComponent
  },
  {
    path: 'auth/:mode',
    component: AuthenticationComponent
  },
  {
    path: 'organiser/create-scene',
    component: CreateSceneComponent,
    canActivate: [RoleGuard],
    data: { expectedRole: 'Organiser' }
  },
  {
    path: 'organiser/create-event',
    component: CreateEventComponent,
    canActivate: [RoleGuard],
    data: { expectedRole: 'Organiser' }
  },
  {
    path: 'organiser/modify-event/:id',
    component: EditEventComponent,
    canActivate: [RoleGuard],
    data: { roles: ['Admin', 'Organiser'] }
  },
  {
    path: 'organiser/modify-scene/:id',
    component: EditSceneComponent,
    canActivate: [RoleGuard],
    data: { roles: ['Admin', 'Organiser'] }
  },
  {
    path: 'organiser/event/:id/dashboard',
    component: DashboardComponent,
    canActivate: [RoleGuard],
    data: { roles: ['Admin', 'Organiser'] }
  },
  {
    path: 'event/:id/reserve',
    component: SeatReservationComponent
  },
  {
    path: 'payment/:id',
    component: PaymentComponent,
    canActivate: [RoleGuard],
    data: { roles: ['Admin', 'Organiser', 'Customer'] }
  },
  {
    path: 'my-reservations',
    component: MyReservationsComponent,
    canActivate: [RoleGuard],
    data: { roles: ['Customer', 'Organiser', 'Admin'] }
  },
  {
    path: 'admin',
    component: AdminControlComponent,
    canActivate: [RoleGuard],
    data: { expectedRole: 'Admin' }
  },
  {
    path: '**',
    redirectTo: ''
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
