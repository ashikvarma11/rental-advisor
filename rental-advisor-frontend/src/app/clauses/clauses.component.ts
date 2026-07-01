import { Component, computed, ElementRef, signal, viewChild } from '@angular/core';
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

const CIRCUMFERENCE = 2 * Math.PI * 54;

type RiskFilter = 'all' | 'high' | 'med' | 'low' | 'resolved';

@Component({
  selector: 'app-clauses',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './clauses.component.html',
  styleUrl: './clauses.component.sass'
})
export class ClausesComponent {
  list = viewChild<ElementRef<HTMLElement>>('list');
  dashboard = viewChild<ElementRef<HTMLElement>>('dashboard');

  leaseId?: number;
  clauses = signal<ClauseVm[]>([]);
  loading = signal(false);
  message = signal<string | undefined>(undefined);
  filter = signal<RiskFilter>('all');

  filteredClauses = computed(() => {
    const list = this.clauses();
    switch (this.filter()) {
      case 'high': return list.filter(c => c.riskScore > 0.5);
      case 'med': return list.filter(c => c.riskScore >= 0.2 && c.riskScore <= 0.5);
      case 'low': return list.filter(c => c.riskScore < 0.2);
      case 'resolved': return list.filter(c => c.isResolved);
      default: return list;
    }
  });

  total = computed(() => this.clauses().length);
  resolved = computed(() => this.clauses().filter(c => c.isResolved).length);
  avgRisk = computed(() => {
    const list = this.clauses();
    if (!list.length) return 0;
    return Math.round((list.reduce((s, c) => s + c.riskScore, 0) / list.length) * 100);
  });
  counts = computed(() => {
    const list = this.clauses();
    return {
      high: list.filter(c => c.riskScore > 0.5).length,
      med: list.filter(c => c.riskScore >= 0.2 && c.riskScore <= 0.5).length,
      low: list.filter(c => c.riskScore < 0.2).length
    };
  });
  circumference = CIRCUMFERENCE;
  dashOffsets = computed(() => {
    const { high, med, low } = this.counts();
    const t = high + med + low || 1;
    const seg = (n: number) => (n / t) * CIRCUMFERENCE;
    return {
      high: seg(high),
      med: seg(med),
      low: seg(low),
      medOffset: -seg(high),
      lowOffset: -(seg(high) + seg(med))
    };
  });

  constructor(private api: ApiService, private route: ActivatedRoute) {
    this.route.queryParams.subscribe(q => {
      if (q['leaseId']) { this.leaseId = +q['leaseId']; this.loadClauses(); }
    });
  }

  toggleFilter(f: RiskFilter) {
    this.filter.set(this.filter() === f ? 'all' : f);
    setTimeout(() => this.animateList());
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
      setTimeout(() => { this.animateDashboard(); this.animateList(); });
    } catch (e: any) { this.message.set(e?.message || String(e)); }
    finally { this.loading.set(false); }
  }

  async resolveClause(c: ClauseVm) {
    if (!c?.id) return;
    try {
      await this.api.resolveClause(c.id);
      this.clauses.update(list => list.map(x => x.id === c.id ? { ...x, isResolved: true } : x));
      setTimeout(() => this.animateDashboard());
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

  private animateDashboard() {
    const el = this.dashboard()?.nativeElement;
    if (!el) return;

    gsap.fromTo(el.querySelectorAll('.stat-tile'), { opacity: 0, y: 20 },
      { opacity: 1, y: 0, duration: 0.45, stagger: 0.08, ease: 'power2.out' });

    el.querySelectorAll<HTMLElement>('.stat-value[data-count]').forEach(node => {
      const target = +(node.dataset['count'] || 0);
      const suffix = node.dataset['suffix'] || '';
      const obj = { val: 0 };
      gsap.to(obj, {
        val: target, duration: 0.9, ease: 'power2.out', delay: 0.15,
        onUpdate: () => node.textContent = Math.round(obj.val) + suffix
      });
    });

    const rings = el.querySelectorAll<SVGCircleElement>('.donut-seg');
    gsap.set(rings, { strokeDasharray: `0 ${this.circumference}` });
    rings.forEach(ring => {
      const full = +(ring.dataset['len'] || 0);
      gsap.to(ring, { strokeDasharray: `${full} ${this.circumference}`, duration: 0.9, ease: 'power2.inOut', delay: 0.2 });
    });
  }
}
