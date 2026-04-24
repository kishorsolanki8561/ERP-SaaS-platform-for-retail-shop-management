import { Directive, Input, OnInit, TemplateRef, ViewContainerRef, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Directive({
  selector: '[hasFeature]',
  standalone: true
})
export class HasFeatureDirective implements OnInit {
  @Input('hasFeature') featureCode = '';

  private readonly auth = inject(AuthService);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (user?.featureCodes.includes(this.featureCode)) {
      this.vcr.createEmbeddedView(this.templateRef);
    }
  }
}
