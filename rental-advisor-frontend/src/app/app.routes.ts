import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { ComparatorComponent } from './comparator/comparator.component';
import { LeaseUploadComponent } from './leases/upload.component';
import { MyLeasesComponent } from './leases/my-leases.component';
import { LoginComponent } from './auth/login.component';
import { RegisterComponent } from './auth/register.component';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
	{ path: '', redirectTo: 'home', pathMatch: 'full' },
	{ path: 'home', component: HomeComponent },
	{ path: 'compare', component: ComparatorComponent },
	{ path: 'login', component: LoginComponent },
	{ path: 'register', component: RegisterComponent },
	{ path: 'upload', component: LeaseUploadComponent, canActivate: [authGuard] },
	{ path: 'upload/:id', component: LeaseUploadComponent, canActivate: [authGuard] },
	{ path: 'my-leases', component: MyLeasesComponent, canActivate: [authGuard] }
];
