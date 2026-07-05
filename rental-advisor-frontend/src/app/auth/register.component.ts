import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './auth.sass'
})
export class RegisterComponent {
  email = '';
  password = '';
  loading = signal(false);
  error = signal<string | undefined>(undefined);

  constructor(private auth: AuthService, private router: Router) {}

  async submit() {
    this.loading.set(true);
    this.error.set(undefined);
    try {
      await this.auth.register(this.email, this.password);
      this.router.navigateByUrl('/upload');
    } catch (e: any) {
      this.error.set(e?.error?.error || e?.message || 'Registration failed');
    } finally {
      this.loading.set(false);
    }
  }
}
