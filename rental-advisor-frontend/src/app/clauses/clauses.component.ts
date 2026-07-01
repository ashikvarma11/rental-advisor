import { Component, ElementRef, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../services/api.service';
import { ActivatedRoute } from '@angular/router';
import gsap from 'gsap';

interface ClauseVm {
  id: number;
  text: string;
  riskScore: number;
  suggestion: string | null;
  isResolved: boolean;
}

@Component({
  selector: 'app-clauses',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './clauses.component.html',
  styleUrl: './clauses.component.sass'
})
export class ClausesComponent {
  list = viewChild<ElementRef<HTMLElement>>('list');

  leaseId?: number;
  clauses = signal<ClauseVm[]>([]);
  loading = signal(false);
  message = signal<string | undefined>(undefined);

  constructor(private api: ApiService, private route: ActivatedRoute) {
    this.route.queryParams.subscribe(q => {
      if (q['leaseId']) { this.leaseId = +q['leaseId']; this.loadClauses(); }
    });
  }

  riskLabel(score: number): string {
    if (score > 0.5) return 'High risk';
    if (score >= 0.2) return 'Medium risk';
    return 'Low risk';
  }

  riskClass(score: number): string {
    if (score > 0.5) return 'badge-high';
    if (score >= 0.2) return 'badge-med';
    return 'badge-low';
  }

  async loadClauses() {
    if (!this.leaseId) return;
    this.loading.set(true); this.message.set(undefined);
    try {
      const clauses = await this.api.getClauses(this.leaseId) as ClauseVm[];
      this.clauses.set(clauses);
      if (!clauses.length) this.message.set('No clauses found for this lease.');
      queueMicrotask(() => this.animateList());
    } catch (e: any) { this.message.set(e?.message || String(e)); }
    finally { this.loading.set(false); }
  }

  async resolveClause(c: ClauseVm) {
    if (!c?.id) return;
    try {
      await this.api.resolveClause(c.id);
      this.clauses.update(list => list.map(x => x.id === c.id ? { ...x, isResolved: true } : x));
    } catch (e: any) { this.message.set(e?.message || String(e)); }
  }

  async exportCsv() {
    if (!this.leaseId) return;
    try {
      const txt: any = await this.api.exportClauses(this.leaseId);
      const blob = new Blob([txt], { type: 'text/csv;charset=utf-8;' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = `clauses_${this.leaseId}.csv`; a.click(); window.URL.revokeObjectURL(url);
    } catch (e: any) { this.message.set(e?.message || String(e)); }
  }

  private animateList() {
    const el = this.list()?.nativeElement;
    if (!el) return;
    const cards = el.querySelectorAll('.clause-card');
    if (!cards.length) return;
    gsap.fromTo(cards, { opacity: 0, y: 24 }, { opacity: 1, y: 0, duration: 0.45, stagger: 0.08, ease: 'power2.out' });
  }
}
