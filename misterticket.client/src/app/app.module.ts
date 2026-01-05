import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { JwtInterceptor } from './interceptors/jwt.interceptor';

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

@NgModule({
  declarations: [
    AdminControlComponent,
    AppComponent,
    AuthenticationComponent,
    CreateEventComponent,
    CreateSceneComponent,
    DashboardComponent,
    EditEventComponent,
    EditSceneComponent,
    HomeComponent,
    MyReservationsComponent,
    PaymentComponent,
    SeatReservationComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    FormsModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
