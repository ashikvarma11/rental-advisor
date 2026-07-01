import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import gsap from 'gsap';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.sass'
})
export class App {
  onActivate() {
    gsap.fromTo('.site-main > *', { opacity: 0, y: 16 }, { opacity: 1, y: 0, duration: 0.5, ease: 'power2.out' });
  }
}
