import { AfterViewInit, Component, ElementRef, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import gsap from 'gsap';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.sass'
})
export class HomeComponent implements AfterViewInit {
  hero = viewChild.required<ElementRef<HTMLElement>>('hero');

  features = [
    {
      tag: '01',
      title: 'Compare suburb rents',
      copy: 'Benchmark any listing against ABS median rent data for its postcode, instantly.',
      link: '/compare',
      cta: 'Open comparator'
    },
    {
      tag: '02',
      title: 'Upload a lease',
      copy: 'Drop in a PDF or text lease and let the advisor pull out the clauses that matter.',
      link: '/upload',
      cta: 'Upload lease'
    },
    {
      tag: '03',
      title: 'Review flagged clauses',
      copy: 'Every clause gets a risk score and plain-English guidance, powered by an LLM in the loop.',
      link: '/upload',
      cta: 'View clauses'
    }
  ];

  stats = [
    { value: '3+', label: 'Suburbs tracked' },
    { value: '11', label: 'Digit ABN lookups' },
    { value: '<1s', label: 'Clause scoring' }
  ];

  ngAfterViewInit() {
    const tl = gsap.timeline({ defaults: { ease: 'power3.out' } });
    tl.fromTo('.hero-eyebrow', { opacity: 0, y: 12 }, { opacity: 1, y: 0, duration: 0.5 })
      .fromTo('.hero-title span', { opacity: 0, y: 40 }, { opacity: 1, y: 0, duration: 0.7, stagger: 0.08 }, '-=0.25')
      .fromTo('.hero-copy', { opacity: 0, y: 16 }, { opacity: 1, y: 0, duration: 0.5 }, '-=0.3')
      .fromTo('.hero-actions', { opacity: 0, y: 16 }, { opacity: 1, y: 0, duration: 0.5 }, '-=0.3')
      .fromTo('.stat', { opacity: 0, y: 16 }, { opacity: 1, y: 0, duration: 0.4, stagger: 0.1 }, '-=0.2')
      .fromTo('.feature-card', { opacity: 0, y: 30 }, { opacity: 1, y: 0, duration: 0.5, stagger: 0.12 }, '-=0.1');
  }
}
