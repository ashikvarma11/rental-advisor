import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import gsap from 'gsap';
import { WakeService } from './services/wake.service';
import { WakeLoaderComponent } from './wake-loader/wake-loader.component';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, WakeLoaderComponent],
  templateUrl: './app.html',
  styleUrl: './app.sass'
})
export class App {
  wake = inject(WakeService);
  auth = inject(AuthService);
  showLoader = signal(!this.wake.ready());

  constructor() {
    this.wake.ping();
  }

  onActivate() {
    gsap.fromTo('.site-main > *', { opacity: 0, y: 16 }, { opacity: 1, y: 0, duration: 0.5, ease: 'power2.out' });
  }
}
