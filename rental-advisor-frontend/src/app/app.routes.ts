import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { ComparatorComponent } from './comparator/comparator.component';
import { LeaseUploadComponent } from './leases/upload.component';
import { ClausesComponent } from './clauses/clauses.component';

export const routes: Routes = [
	{ path: '', redirectTo: 'home', pathMatch: 'full' },
	{ path: 'home', component: HomeComponent },
	{ path: 'compare', component: ComparatorComponent },
	{ path: 'upload', component: LeaseUploadComponent },
	{ path: 'clauses', component: ClausesComponent }
];
