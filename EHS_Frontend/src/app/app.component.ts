import { Component, OnInit } from '@angular/core';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { BackendTestService } from './services/backend-test.service';
import { AlertContainerComponent } from './components/alert-container/alert-container.component';
import { ScrollToTopComponent } from './components/scroll-to-top/scroll-to-top.component';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  imports: [RouterModule, AlertContainerComponent, ScrollToTopComponent]  // ✅ Added ScrollToTopComponent
})
export class AppComponent implements OnInit {
  title = 'EHS_Frontend';

  constructor(
    private backendTestService: BackendTestService,
    private router: Router
  ) {
    console.log('🔧 AppComponent: Constructor called');
  }

  ngOnInit() {
    console.log('🔧 AppComponent: ngOnInit called');
    // Backend health check removed to prevent console errors
  }
}
