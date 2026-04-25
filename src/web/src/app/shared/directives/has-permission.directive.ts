import { Directive, Input, OnInit, TemplateRef, ViewContainerRef, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Directive({
  selector: '[hasPermission]',
  standalone: true
})
export class HasPermissionDirective implements OnInit {
  @Input('hasPermission') permissionCode = '';

  private readonly auth = inject(AuthService);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (user?.isPlatformAdmin || user?.permissionCodes.includes(this.permissionCode)) {
      this.vcr.createEmbeddedView(this.templateRef);
    }
  }
}
