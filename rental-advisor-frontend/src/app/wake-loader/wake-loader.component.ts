import { AfterViewInit, Component, effect, input, output } from '@angular/core';
import gsap from 'gsap';

@Component({
  selector: 'app-wake-loader',
  standalone: true,
  templateUrl: './wake-loader.component.html',
  styleUrl: './wake-loader.component.sass'
})
export class WakeLoaderComponent implements AfterViewInit {

  asleep = input(true);
  done = output<void>();

  private sleepTl?: gsap.core.Timeline;

  constructor() {
    effect(() => {
      if (!this.asleep()) this.wakeUp();
    });
  }

  ngAfterViewInit() {
    this.sleepTl = gsap.timeline({ repeat: -1 })
      .to('.torso-group', { y: -3, duration: 1.6, ease: 'sine.inOut', yoyo: true, repeat: 1 }, 0)
      .to('.z1', { opacity: 1, y: -8, duration: 1, ease: 'power1.out' }, 0)
      .to('.z2', { opacity: 1, y: -8, duration: 1, ease: 'power1.out' }, 0.3)
      .to('.z3', { opacity: 1, y: -8, duration: 1, ease: 'power1.out' }, 0.6)
      .to('.z1, .z2, .z3', { opacity: 0, duration: 0.4 }, 1.6);
  }

  private wakeUp() {
    this.sleepTl?.kill();

    const tl = gsap.timeline({ onComplete: () => this.done.emit() });
    tl.to('.sun', { cy: 44, duration: 1.6, ease: 'power2.out' }, 0)
      .to('.sky', { fill: '#a3d5ff', duration: 1.6, ease: 'power2.out' }, 0)
      .to('.z1, .z2, .z3', { opacity: 0, duration: 0.2 }, 0.1)
      .to('.eye-left, .eye-right', { scaleY: 1.2, duration: 0.3, ease: 'power1.out' }, '<')
      .to('.eye-left, .eye-right', { opacity: 0, duration: 0.15 }, '<0.15')
      .to('.eye-open-left, .eye-open-right', { opacity: 1, duration: 0.15 }, '<')
      .to('.wake-screen', { opacity: 0, duration: 0.4, ease: 'power1.in' }, '+=0.5');
  }
}
