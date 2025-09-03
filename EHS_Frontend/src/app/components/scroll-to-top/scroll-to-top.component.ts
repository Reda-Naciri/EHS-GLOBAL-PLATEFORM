import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-scroll-to-top',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button 
      *ngIf="showScrollButton" 
      (click)="scrollToTop()"
      class="scroll-to-top-btn"
      title="Scroll to top">
      <i class="fas fa-chevron-up"></i>
    </button>
  `,
  styles: [`
    .scroll-to-top-btn {
      position: fixed;
      bottom: 30px;
      right: 30px;
      width: 50px;
      height: 50px;
      border-radius: 50%;
      background: #E98300;
      color: white;
      border: none;
      cursor: pointer;
      z-index: 1000;
      box-shadow: 0 4px 12px rgba(233, 131, 0, 0.3);
      transition: all 0.3s ease;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 18px;
    }
    
    .scroll-to-top-btn:hover {
      background: #cc7400;
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(233, 131, 0, 0.4);
    }
    
    @media (max-width: 768px) {
      .scroll-to-top-btn {
        bottom: 20px;
        right: 20px;
        width: 45px;
        height: 45px;
        font-size: 16px;
      }
    }
  `]
})
export class ScrollToTopComponent implements OnInit, OnDestroy {
  showScrollButton = false;

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.checkScrollPosition();
  }

  ngOnInit() {
    this.checkScrollPosition();
  }

  ngOnDestroy() {
    // Component cleanup
  }

  private checkScrollPosition() {
    const scrollPosition = window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop || 0;
    
    // Also check dashboard-content scroll
    const dashboardContent = document.querySelector('.dashboard-content');
    const dashboardScrollPosition = dashboardContent ? dashboardContent.scrollTop : 0;
    
    // Show button if either window or dashboard content is scrolled down more than 300px
    this.showScrollButton = scrollPosition > 300 || dashboardScrollPosition > 300;
  }

  scrollToTop() {
    // Scroll window
    window.scrollTo({ top: 0, behavior: 'smooth' });
    
    // Also scroll dashboard content
    const dashboardContent = document.querySelector('.dashboard-content');
    if (dashboardContent) {
      dashboardContent.scrollTo({ top: 0, behavior: 'smooth' });
    }
    
    // Force scroll on document elements
    document.documentElement.scrollTop = 0;
    document.body.scrollTop = 0;
  }
}