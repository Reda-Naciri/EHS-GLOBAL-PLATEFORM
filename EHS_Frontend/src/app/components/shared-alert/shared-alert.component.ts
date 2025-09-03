import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

export type AlertType = 'warning' | 'success' | 'error' | 'info';

@Component({
  selector: 'app-shared-alert',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './shared-alert.component.html',
  styleUrls: ['./shared-alert.component.css']
})
export class SharedAlertComponent implements OnInit, OnDestroy {
  @Input() type: AlertType = 'warning';
  @Input() message: string = '';
  @Input() show: boolean = false;
  @Input() autoHide: boolean = true;
  @Input() autoHideDuration: number = 5000; // 5 seconds default
  @Input() position: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left' = 'top-right';
  @Input() showProgressBar: boolean = true;
  @Input() allowClose: boolean = true;

  @Output() close = new EventEmitter<void>();
  @Output() hide = new EventEmitter<void>();

  private autoHideTimer?: number;

  ngOnInit() {
    if (this.show && this.autoHide) {
      this.startAutoHideTimer();
    }
  }

  ngOnDestroy() {
    this.clearAutoHideTimer();
  }

  ngOnChanges() {
    if (this.show && this.autoHide) {
      this.startAutoHideTimer();
    } else {
      this.clearAutoHideTimer();
    }
  }

  private startAutoHideTimer() {
    this.clearAutoHideTimer();
    this.autoHideTimer = window.setTimeout(() => {
      this.onClose();
    }, this.autoHideDuration);
  }

  private clearAutoHideTimer() {
    if (this.autoHideTimer) {
      clearTimeout(this.autoHideTimer);
      this.autoHideTimer = undefined;
    }
  }

  onClose() {
    this.clearAutoHideTimer();
    this.close.emit();
    this.hide.emit();
  }

  getAlertClasses(): string {
    const baseClasses = 'fixed z-50 p-4 rounded-lg shadow-lg max-w-md transform transition-all duration-300 ease-in-out';
    
    const positionClasses = {
      'top-right': 'top-4 right-4',
      'top-left': 'top-4 left-4', 
      'bottom-right': 'bottom-4 right-4',
      'bottom-left': 'bottom-4 left-4'
    };

    const typeClasses = {
      'warning': 'bg-yellow-50 border-l-4 border-yellow-400 animate-pulse',
      'success': 'bg-green-50 border-l-4 border-green-400',
      'error': 'bg-red-50 border-l-4 border-red-400',
      'info': 'bg-blue-50 border-l-4 border-blue-400'
    };

    const visibilityClasses = this.show 
      ? 'translate-x-0 opacity-100' 
      : 'translate-x-full opacity-0';

    return `${baseClasses} ${positionClasses[this.position]} ${typeClasses[this.type]} ${visibilityClasses}`;
  }

  getIconClass(): string {
    const iconClasses = {
      'warning': 'fas fa-exclamation-triangle text-yellow-500 mr-3 animate-bounce',
      'success': 'fas fa-check-circle text-green-500 mr-3 animate-pulse',
      'error': 'fas fa-exclamation-circle text-red-500 mr-3 animate-bounce',
      'info': 'fas fa-info-circle text-blue-500 mr-3'
    };

    return iconClasses[this.type];
  }

  getTextClass(): string {
    const textClasses = {
      'warning': 'text-yellow-800 font-medium',
      'success': 'text-green-800 font-medium',
      'error': 'text-red-800 font-medium',
      'info': 'text-blue-800 font-medium'
    };

    return textClasses[this.type];
  }

  getCloseButtonClass(): string {
    const buttonClasses = {
      'warning': 'ml-4 text-yellow-500 hover:text-yellow-700 transition-colors duration-200 hover:rotate-90 transform',
      'success': 'ml-4 text-green-500 hover:text-green-700 transition-colors duration-200 hover:rotate-90 transform',
      'error': 'ml-4 text-red-500 hover:text-red-700 transition-colors duration-200 hover:rotate-90 transform',
      'info': 'ml-4 text-blue-500 hover:text-blue-700 transition-colors duration-200 hover:rotate-90 transform'
    };

    return buttonClasses[this.type];
  }

  getProgressBarClasses(): string {
    const progressClasses = {
      'warning': 'mt-2 w-full bg-yellow-200 rounded-full h-1',
      'success': 'mt-2 w-full bg-green-200 rounded-full h-1',
      'error': 'mt-2 w-full bg-red-200 rounded-full h-1',
      'info': 'mt-2 w-full bg-blue-200 rounded-full h-1'
    };

    return progressClasses[this.type];
  }

  getProgressBarFillClasses(): string {
    const fillClasses = {
      'warning': 'bg-yellow-400 h-1 rounded-full animate-pulse',
      'success': 'bg-green-400 h-1 rounded-full animate-pulse',
      'error': 'bg-red-400 h-1 rounded-full animate-pulse',
      'info': 'bg-blue-400 h-1 rounded-full animate-pulse'
    };

    return fillClasses[this.type];
  }

  getAnimationDuration(): string {
    return `${this.autoHideDuration / 1000}s`;
  }
}