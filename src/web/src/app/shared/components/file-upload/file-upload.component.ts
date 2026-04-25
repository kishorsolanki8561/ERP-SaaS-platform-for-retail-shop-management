import {
  ChangeDetectionStrategy, Component,
  inject, input, output, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ApiEndpoints } from '../../messages/app-api';

export interface UploadedFileDto {
  id: number;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  purpose: string;
  entityType: string | null;
  entityId: number | null;
  url: string;
  uploadedAtUtc: string;
}

@Component({
  selector: 'app-file-upload',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ButtonModule, ProgressBarModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast position="top-right" />

    <div class="border-2 border-dashed border-surface-300 rounded-lg p-6 text-center
                hover:border-primary-400 transition-colors cursor-pointer"
         [class.opacity-50]="uploading()"
         (dragover)="$event.preventDefault()"
         (drop)="onDrop($event)"
         (click)="fileInput.click()">

      <input #fileInput type="file"
             [accept]="accept()"
             [multiple]="multiple()"
             class="hidden"
             (change)="onFileSelected($event)" />

      @if (uploading()) {
        <div class="space-y-2">
          <i class="pi pi-spin pi-spinner text-3xl text-primary-500"></i>
          <p class="text-sm text-surface-600">Uploading {{ uploadingName() }}…</p>
          <p-progressBar mode="indeterminate" styleClass="h-1" />
        </div>
      } @else {
        <div class="space-y-2">
          <i class="pi pi-cloud-upload text-4xl text-surface-400"></i>
          <p class="text-sm font-medium text-surface-700">
            Drop file here or <span class="text-primary-500">click to browse</span>
          </p>
          @if (accept()) {
            <p class="text-xs text-surface-400">Allowed: {{ accept() }}</p>
          }
          @if (maxSizeMb() > 0) {
            <p class="text-xs text-surface-400">Max {{ maxSizeMb() }} MB</p>
          }
        </div>
      }
    </div>

    @if (uploadedFiles().length > 0) {
      <ul class="mt-3 space-y-2">
        @for (f of uploadedFiles(); track f.id) {
          <li class="flex items-center gap-3 p-2 bg-surface-50 rounded border border-surface-200">
            <i [class]="iconFor(f.contentType)" class="text-surface-500"></i>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium truncate">{{ f.originalFileName }}</p>
              <p class="text-xs text-surface-400">{{ formatSize(f.sizeBytes) }}</p>
            </div>
            <a [href]="f.url" target="_blank"
               class="text-xs text-primary-500 hover:underline">View</a>
            <p-button icon="pi pi-times" severity="danger" [text]="true" size="small"
                      (onClick)="removeFile(f)" [disabled]="uploading()" />
          </li>
        }
      </ul>
    }
  `,
})
export class FileUploadComponent {
  purpose   = input.required<string>();
  entityType = input<string | null>(null);
  entityId   = input<number | null>(null);
  accept    = input<string>('.jpg,.jpeg,.png,.pdf,.xlsx,.csv');
  maxSizeMb = input<number>(5);
  multiple  = input<boolean>(false);

  fileUploaded = output<UploadedFileDto>();
  fileRemoved  = output<UploadedFileDto>();

  uploading    = signal(false);
  uploadingName = signal('');
  uploadedFiles = signal<UploadedFileDto[]>([]);

  private http = inject(HttpClient);
  private toast = inject(MessageService);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.uploadFiles(Array.from(input.files));
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    const files = event.dataTransfer?.files;
    if (files?.length) this.uploadFiles(Array.from(files));
  }

  private async uploadFiles(files: File[]): Promise<void> {
    for (const file of files) {
      if (this.maxSizeMb() > 0 && file.size > this.maxSizeMb() * 1024 * 1024) {
        this.toast.add({ severity: 'error', summary: 'File too large',
          detail: `${file.name} exceeds ${this.maxSizeMb()} MB limit.` });
        continue;
      }
      await this.uploadOne(file);
    }
  }

  private async uploadOne(file: File): Promise<void> {
    this.uploading.set(true);
    this.uploadingName.set(file.name);
    try {
      const form = new FormData();
      form.append('file', file);
      form.append('purpose', this.purpose());
      if (this.entityType()) form.append('entityType', this.entityType()!);
      if (this.entityId() != null) form.append('entityId', String(this.entityId()));

      const dto = await firstValueFrom(
        this.http.post<UploadedFileDto>(ApiEndpoints.files.upload, form));

      this.uploadedFiles.update(list => [...list, dto]);
      this.fileUploaded.emit(dto);
      this.toast.add({ severity: 'success', summary: 'Uploaded', detail: file.name, life: 3000 });
    } catch {
      this.toast.add({ severity: 'error', summary: 'Upload failed', detail: file.name });
    } finally {
      this.uploading.set(false);
      this.uploadingName.set('');
    }
  }

  async removeFile(file: UploadedFileDto): Promise<void> {
    try {
      await firstValueFrom(this.http.delete(ApiEndpoints.files.file(file.id)));
      this.uploadedFiles.update(list => list.filter(f => f.id !== file.id));
      this.fileRemoved.emit(file);
    } catch {
      this.toast.add({ severity: 'error', summary: 'Delete failed', detail: file.originalFileName });
    }
  }

  iconFor(contentType: string): string {
    if (contentType.startsWith('image/')) return 'pi pi-image';
    if (contentType === 'application/pdf') return 'pi pi-file-pdf';
    if (contentType.includes('spreadsheet') || contentType.includes('excel')) return 'pi pi-file-excel';
    return 'pi pi-file';
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
