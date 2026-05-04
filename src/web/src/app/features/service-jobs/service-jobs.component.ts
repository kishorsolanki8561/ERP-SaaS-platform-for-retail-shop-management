import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { ApiEndpoints } from '../../shared/messages/app-api';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';

interface ServiceJobSummary {
  id: number;
  jobNumber: string;
  receivedAtDate: string;
  customerId: number;
  customerNameSnapshot: string;
  itemDescription: string;
  serialNumber: string;
  status: string;
  totalCost: number;
  deliveredAtUtc: string | null;
}

@Component({
  selector: 'app-service-jobs',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, ButtonModule, TableModule, TagModule, DialogModule, InputTextModule, TextareaModule, InputNumberModule],
  template: `
    <div class="p-4">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-xl font-semibold">Service Jobs</h2>
        <p-button label="New Job" icon="pi pi-plus" (onClick)="openNew()" />
      </div>

      <p-table [value]="jobs()" [loading]="loading()" dataKey="id" responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th>Job #</th>
            <th>Date</th>
            <th>Customer</th>
            <th>Item</th>
            <th>Status</th>
            <th>Cost</th>
            <th>Actions</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-job>
          <tr>
            <td><code>{{ job.jobNumber }}</code></td>
            <td>{{ job.receivedAtDate | date:'dd MMM yyyy' }}</td>
            <td>{{ job.customerNameSnapshot || '—' }}</td>
            <td>{{ job.itemDescription }}<br><small class="text-gray-400">{{ job.serialNumber }}</small></td>
            <td><p-tag [value]="job.status" [severity]="statusSeverity(job.status)" /></td>
            <td>₹{{ job.totalCost | number:'1.2-2' }}</td>
            <td>
              <p-button icon="pi pi-eye" [text]="true" (onClick)="viewJob(job)" pTooltip="View" />
              @if(job.status === 'Received') {
                <p-button icon="pi pi-search" [text]="true" severity="info" (onClick)="openDiagnose(job)" pTooltip="Diagnose" />
              }
              @if(job.status === 'Diagnosed') {
                <p-button icon="pi pi-check" [text]="true" severity="success" (onClick)="approveJob(job.id)" pTooltip="Customer Approved" />
              }
              @if(job.status === 'Approved') {
                <p-button icon="pi pi-play" [text]="true" severity="info" (onClick)="startProgress(job.id)" pTooltip="Start Work" />
              }
              @if(job.status === 'InProgress') {
                <p-button icon="pi pi-flag" [text]="true" severity="warn" (onClick)="markReady(job.id)" pTooltip="Mark Ready" />
              }
              @if(job.status === 'Ready') {
                <p-button icon="pi pi-send" [text]="true" severity="success" (onClick)="deliverJob(job.id)" pTooltip="Deliver" />
              }
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr><td colspan="7" class="text-center py-8 text-gray-400">No service jobs found</td></tr>
        </ng-template>
      </p-table>
    </div>

    <!-- New Job Dialog -->
    <p-dialog header="Receive New Job" [(visible)]="showNew" [modal]="true" [style]="{width:'480px'}">
      <div class="flex flex-col gap-3 p-2">
        <div class="flex flex-col gap-1">
          <label>Customer ID</label>
          <p-inputNumber [(ngModel)]="newDto.customerId" [min]="1" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Item Description</label>
          <input pInputText [(ngModel)]="newDto.itemDescription" placeholder="e.g. Drill Machine" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Serial Number</label>
          <input pInputText [(ngModel)]="newDto.serialNumber" placeholder="Optional" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Reported Issue</label>
          <textarea pTextarea [(ngModel)]="newDto.reportedIssue" rows="3" placeholder="Describe the issue"></textarea>
        </div>
      </div>
      <ng-template pTemplate="footer">
        <p-button label="Cancel" [text]="true" (onClick)="showNew = false" />
        <p-button label="Receive" icon="pi pi-check" (onClick)="receiveJob()" />
      </ng-template>
    </p-dialog>

    <!-- Diagnose Dialog -->
    <p-dialog header="Diagnose Job" [(visible)]="showDiagnose" [modal]="true" [style]="{width:'440px'}">
      <div class="flex flex-col gap-3 p-2">
        <div class="flex flex-col gap-1">
          <label>Diagnosis Notes</label>
          <textarea pTextarea [(ngModel)]="diagnoseDto.diagnosisNotes" rows="4"></textarea>
        </div>
        <div class="flex flex-col gap-1">
          <label>Estimated Cost (₹)</label>
          <p-inputNumber [(ngModel)]="diagnoseDto.estimatedCost" [min]="0" mode="currency" currency="INR" locale="en-IN" />
        </div>
      </div>
      <ng-template pTemplate="footer">
        <p-button label="Cancel" [text]="true" (onClick)="showDiagnose = false" />
        <p-button label="Save Diagnosis" icon="pi pi-check" (onClick)="submitDiagnose()" />
      </ng-template>
    </p-dialog>
  `,
})
export class ServiceJobsComponent {
  private http = inject(HttpClient);

  loading = signal(false);
  jobs = signal<ServiceJobSummary[]>([]);
  showNew = false;
  showDiagnose = false;
  selectedJobId = signal<number | null>(null);

  newDto = { customerId: 0, itemDescription: '', serialNumber: '', reportedIssue: '', branchId: 1 };
  diagnoseDto = { diagnosisNotes: '', estimatedCost: 0 };

  constructor() { this.loadJobs(); }

  loadJobs() {
    this.loading.set(true);
    this.http.get<ServiceJobSummary[]>(ApiEndpoints.serviceJobs.list).pipe(catchError(() => of([])))
      .subscribe(data => { this.jobs.set(data); this.loading.set(false); });
  }

  openNew() { this.newDto = { customerId: 0, itemDescription: '', serialNumber: '', reportedIssue: '', branchId: 1 }; this.showNew = true; }

  receiveJob() {
    const body = { ...this.newDto, isUnderWarranty: false };
    this.http.post(ApiEndpoints.serviceJobs.create, body).subscribe(() => { this.showNew = false; this.loadJobs(); });
  }

  openDiagnose(job: ServiceJobSummary) { this.selectedJobId.set(job.id); this.diagnoseDto = { diagnosisNotes: '', estimatedCost: 0 }; this.showDiagnose = true; }

  submitDiagnose() {
    const id = this.selectedJobId();
    if (!id) return;
    this.http.post(ApiEndpoints.serviceJobs.diagnose(id), this.diagnoseDto).subscribe(() => { this.showDiagnose = false; this.loadJobs(); });
  }

  approveJob(id: number) { this.http.post(ApiEndpoints.serviceJobs.approve(id), {}).subscribe(() => this.loadJobs()); }
  startProgress(id: number) { this.http.post(ApiEndpoints.serviceJobs.startProgress(id), {}).subscribe(() => this.loadJobs()); }
  markReady(id: number) { this.http.post(ApiEndpoints.serviceJobs.markReady(id), {}).subscribe(() => this.loadJobs()); }
  deliverJob(id: number) { this.http.post(ApiEndpoints.serviceJobs.deliver(id), {}).subscribe(() => this.loadJobs()); }

  viewJob(job: ServiceJobSummary) { /* detail sidebar omitted for brevity */ }

  statusSeverity(status: string): 'info' | 'success' | 'warn' | 'danger' | 'secondary' {
    const map: Record<string, 'info' | 'success' | 'warn' | 'danger' | 'secondary'> = {
      Received: 'secondary', Diagnosed: 'info', Approved: 'info',
      InProgress: 'warn', Ready: 'success', Delivered: 'success', Rejected: 'danger',
    };
    return map[status] ?? 'secondary';
  }
}
