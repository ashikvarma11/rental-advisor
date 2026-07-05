import { Component, computed, ElementRef, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../services/api.service';
import gsap from 'gsap';
import jsPDF from 'jspdf';

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
  selector: 'app-lease-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.sass'
})
export class LeaseUploadComponent {
  dropZone = viewChild<ElementRef<HTMLElement>>('dropZone');
  list = viewChild<ElementRef<HTMLElement>>('list');
  dashboard = viewChild<ElementRef<HTMLElement>>('dashboard');
  resultsTitle = viewChild<ElementRef<HTMLElement>>('resultsTitle');

  file = signal<File | undefined>(undefined);
  dragging = signal(false);
  loading = signal(false);
  processing = signal(false);
  error = signal<string | undefined>(undefined);

  leaseId?: number;
  clauses = signal<ClauseVm[]>([]);
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
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.leaseId = +id;
      this.loadClauses();
    }
  }

  onDragOver(e: DragEvent) {
    e.preventDefault();
    this.dragging.set(true);
  }

  onDragLeave() {
    this.dragging.set(false);
  }

  onDrop(e: DragEvent) {
    e.preventDefault();
    this.dragging.set(false);
    const f = e.dataTransfer?.files?.[0];
    if (f) this.setFile(f);
  }

  onFileChange(event: any) {
    const f = event.target.files?.[0];
    if (f) this.setFile(f);
  }

  private setFile(f: File) {
    this.file.set(f);
    this.error.set(undefined);
    this.clauses.set([]);
    this.leaseId = undefined;
    const el = this.dropZone()?.nativeElement;
    if (el) gsap.fromTo(el, { scale: 0.98 }, { scale: 1, duration: 0.3, ease: 'back.out(2)' });
  }

  async upload() {
    const file = this.file();
    if (!file) return;
    this.loading.set(true);
    this.error.set(undefined);
    try {
      const result: any = await this.api.uploadLease(file);
      if (result?.id) {
        this.leaseId = result.id;
        await this.api.extractClauses(result.id);
        await this.pollExtraction(result.id);
      }
    } catch (e: any) {
      this.error.set(e?.error?.error || e?.message || 'Upload failed');
    } finally {
      this.loading.set(false);
    }
  }

  private async pollExtraction(leaseId: number) {
    this.processing.set(true);
    try {
      while (true) {
        const status: any = await this.api.getExtractStatus(leaseId);
        if (status?.status === 'Done') {
          await this.loadClauses();
          break;
        }
        if (status?.status === 'Failed') {
          this.error.set(status?.error || 'Clause extraction failed');
          break;
        }
        await new Promise(r => setTimeout(r, 2000));
      }
    } finally {
      this.processing.set(false);
    }
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
    this.message.set(undefined);
    try {
      const clauses = await this.api.getClauses(this.leaseId) as ClauseVm[];
      this.clauses.set(clauses);
      if (!clauses.length) this.message.set('No clauses found for this lease.');
      setTimeout(() => { this.scrollToResults(); this.animateDashboard(); this.animateList(); });
    } catch (e: any) { this.message.set(e?.message || String(e)); }
  }

  async resolveClause(c: ClauseVm) {
    if (!c?.id) return;
    try {
      await this.api.resolveClause(c.id);
      this.clauses.update(list => list.map(x => x.id === c.id ? { ...x, isResolved: true } : x));
      setTimeout(() => this.animateDashboard());
    } catch (e: any) { this.message.set(e?.message || String(e)); }
  }

  exportPdf() {
    const sorted = [...this.clauses()].sort((a, b) => b.riskScore - a.riskScore);
    if (!sorted.length) return;

    const doc = new jsPDF({ unit: 'pt', format: 'a4' });
    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();
    const margin = 48;
    const contentWidth = pageWidth - margin * 2;
    let y = margin;

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(18);
    doc.text('Lease Clause Analysis', margin, y);
    y += 28;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.setTextColor(110);
    doc.text(`Total clauses: ${this.total()}  ·  Average risk: ${this.avgRisk()}%  ·  High: ${this.counts().high}  Medium: ${this.counts().med}  Low: ${this.counts().low}`, margin, y);
    y += 24;
    doc.setTextColor(0);

    const ensureSpace = (needed: number) => {
      if (y + needed > pageHeight - margin) {
        doc.addPage();
        y = margin;
      }
    };

    for (const c of sorted) {
      const label = this.riskLabel(c.riskScore).toUpperCase();
      const scorePct = Math.round(c.riskScore * 100);

      const textLines = doc.splitTextToSize(c.text || '', contentWidth);
      const suggestionLines = c.suggestion ? doc.splitTextToSize(`Suggestion: ${c.suggestion}`, contentWidth) : [];

      ensureSpace(24 + textLines.length * 13 + (suggestionLines.length ? suggestionLines.length * 13 + 8 : 0) + 16);

      doc.setFont('helvetica', 'bold');
      doc.setFontSize(11);
      if (c.riskScore > 0.5) doc.setTextColor(200, 40, 40);
      else if (c.riskScore >= 0.2) doc.setTextColor(200, 130, 0);
      else doc.setTextColor(60, 130, 80);
      doc.text(`${label} — ${scorePct}%`, margin, y);
      doc.setTextColor(0);
      y += 16;

      doc.setFont('helvetica', 'normal');
      doc.setFontSize(10);
      doc.text(textLines, margin, y);
      y += textLines.length * 13 + 4;

      if (suggestionLines.length) {
        doc.setFont('helvetica', 'italic');
        doc.setTextColor(90);
        doc.text(suggestionLines, margin, y);
        y += suggestionLines.length * 13;
        doc.setTextColor(0);
      }

      y += 16;
      doc.setDrawColor(220);
      doc.line(margin, y - 8, pageWidth - margin, y - 8);
    }

    doc.save(`clauses_${this.leaseId}.pdf`);
  }

  private scrollToResults() {
    const el = this.resultsTitle()?.nativeElement;
    if (!el) return;
    const targetY = el.getBoundingClientRect().top + window.scrollY - 24;
    const pos = { y: window.scrollY };
    gsap.to(pos, {
      y: targetY, duration: 0.9, ease: 'power2.inOut',
      onUpdate: () => window.scrollTo(0, pos.y)
    });
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
