import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ApiService } from '../services/api.service';

interface LeaseVm {
  id: number;
  fileName: string;
  uploadedAt: string;
}

@Component({
  selector: 'app-my-leases',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-leases.component.html',
  styleUrl: './my-leases.component.sass'
})
export class MyLeasesComponent implements OnInit {
  leases = signal<LeaseVm[]>([]);
  loading = signal(true);
  error = signal<string | undefined>(undefined);

  constructor(private api: ApiService, private router: Router) {}

  async ngOnInit() {
    try {
      this.leases.set(await this.api.getLeases() as LeaseVm[]);
    } catch (e: any) {
      this.error.set(e?.error?.error || e?.message || 'Failed to load leases');
    } finally {
      this.loading.set(false);
    }
  }

  open(lease: LeaseVm) {
    this.router.navigate(['/upload', lease.id]);
  }
}
