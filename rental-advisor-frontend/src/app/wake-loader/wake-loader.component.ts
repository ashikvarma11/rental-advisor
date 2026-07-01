import { AfterViewInit, Component } from '@angular/core';
import gsap from 'gsap';

@Component({
  selector: 'app-wake-loader',
  standalone: true,
  templateUrl: './wake-loader.component.html',
  styleUrl: './wake-loader.component.sass'
})
export class WakeLoaderComponent implements AfterViewInit {
  letters = ['R', 'E', 'N', 'T'];

  ngAfterViewInit() {
    gsap.to('.wake-key', {
      y: -14,
      duration: 0.45,
      ease: 'power2.out',
      stagger: { each: 0.09, repeat: -1, yoyo: true }
    });
  }
}
