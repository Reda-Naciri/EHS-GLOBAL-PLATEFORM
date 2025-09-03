import { Component, ElementRef, HostListener, Renderer2, inject, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute, NavigationEnd, RouterModule } from '@angular/router';
import { filter, map } from 'rxjs/operators';
import { Title } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { NavbarComponent } from '../components/navbar/navbar.component';
import { SidebarComponent } from '../components/sidebar/sidebar.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterModule, NavbarComponent, SidebarComponent],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.css']
})
export class LayoutComponent implements OnDestroy {
  sidebarOpen = false;
  pageTitle: string = 'Dashboard';
  private routerSubscription?: Subscription;

  private router = inject(Router);
  private route = inject(ActivatedRoute);

  constructor(
    private renderer: Renderer2,
    private el: ElementRef,
    private titleService: Title
  ) {
    this.routerSubscription = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      map(() => {
        let child = this.route.firstChild;
        while (child?.firstChild) {
          child = child.firstChild;
        }
        return child?.snapshot.data['title'] || 'Dashboard';
      })
    ).subscribe(title => {
      this.pageTitle = title;
      this.titleService.setTitle(`HSE - ${title}`);
      // Auto-close sidebar on navigation
      this.closeSidebarOnNavigation();
    });
  }

  toggleSidebar = () => {
    this.sidebarOpen = !this.sidebarOpen;
    const sidebar = this.el.nativeElement.querySelector('.sidebar');
    if (sidebar) {
      this.sidebarOpen
        ? this.renderer.addClass(sidebar, 'open')
        : this.renderer.removeClass(sidebar, 'open');
    }
  };

  @HostListener('document:click', ['$event'])
  closeSidebar(event: Event) {
    const sidebar = this.el.nativeElement.querySelector('.sidebar');
    const hamburger = this.el.nativeElement.querySelector('.hamburger');
    if (
      this.sidebarOpen &&
      sidebar &&
      !sidebar.contains(event.target as Node) &&
      !hamburger.contains(event.target as Node)
    ) {
      this.renderer.removeClass(sidebar, 'open');
      this.sidebarOpen = false;
    }
  }

  closeSidebarOnNavigation() {
    if (this.sidebarOpen) {
      const sidebar = this.el.nativeElement.querySelector('.sidebar');
      if (sidebar) {
        this.renderer.removeClass(sidebar, 'open');
        this.sidebarOpen = false;
      }
    }
  }

  ngOnDestroy() {
    this.routerSubscription?.unsubscribe();
  }

}
