import { Component, ElementRef, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import gsap from 'gsap';

interface CompareResult {
  postcode: string;
  median: number | null;
  averageListingRent: number | null;
  minRent: number | null;
  maxRent: number | null;
  stdDeviation: number | null;
  listingCount: number;
  isLowConfidence: boolean;
  differencePercent: number | null;
}

@Component({
  selector: 'app-comparator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './comparator.component.html',
  styleUrl: './comparator.component.sass'
})
export class ComparatorComponent {
  panel = viewChild<ElementRef<HTMLElement>>('panel');

  postcode = '2010';
  result = signal<CompareResult | null>(null);
  loading = signal(false);
  error = signal<string | undefined>(undefined);

  constructor(private api: ApiService) {}

  async compare() {
    this.loading.set(true);
    this.error.set(undefined);
    try {
      const result = await this.api.compareSuburb(this.postcode) as CompareResult;
      this.result.set(result);
      queueMicrotask(() => this.animateResult());
    } catch (e: any) {
      this.error.set(e?.error?.error || e?.message || 'Something went wrong');
      this.result.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  barWidth(value: number | null, max: number): number {
    if (!value) return 0;
    return Math.min(100, (value / max) * 100);
  }

  diffClass(diff: number | null): string {
    if (diff == null) return 'badge-med';
    if (diff <= 0) return 'badge-low';
    if (diff < 10) return 'badge-med';
    return 'badge-high';
  }

  private animateResult() {
    const el = this.panel()?.nativeElement;
    if (!el) return;
    gsap.fromTo(el, { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.5, ease: 'power2.out' });
    gsap.fromTo(el.querySelectorAll('.bar-fill'), { width: '0%' }, { width: (_i: number, target: HTMLElement) => target.dataset['target'] + '%', duration: 0.8, ease: 'power3.out', delay: 0.15 });
  }
}
