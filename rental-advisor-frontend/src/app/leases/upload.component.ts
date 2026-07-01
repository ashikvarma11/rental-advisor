import { Component, ElementRef, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../services/api.service';
import gsap from 'gsap';

@Component({
  selector: 'app-lease-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.sass'
})
export class LeaseUploadComponent {
  dropZone = viewChild<ElementRef<HTMLElement>>('dropZone');

  file = signal<File | undefined>(undefined);
  dragging = signal(false);
  loading = signal(false);
  error = signal<string | undefined>(undefined);

  constructor(private api: ApiService, private router: Router) {}

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
        await this.api.extractClauses(result.id);
        this.router.navigate(['/clauses'], { queryParams: { leaseId: result.id } });
      }
    } catch (e: any) {
      this.error.set(e?.error?.error || e?.message || 'Upload failed');
    } finally {
      this.loading.set(false);
    }
  }
}
