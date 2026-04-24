import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ConfirmDialogModule],
  providers: [ConfirmationService],
  template: `<p-confirmDialog />`
})
export class ConfirmDialogComponent {}
