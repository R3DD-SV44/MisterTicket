import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { EventListComponent } from './components/event-list/event-list.component';
import { AuthenticationComponent } from './components/authentication/authentication.component';
import { CreateSceneComponent } from './components/create-scene/create-scene.component';
import { CreateEventComponent } from './components/create-event/create-event.component';
import { RoleGuard } from './guards/role.guard';
import { EditEventComponent } from './components/edit-event/edit-event.component';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'events', component: EventListComponent },
  { path: 'auth/:mode', component: AuthenticationComponent },
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
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
