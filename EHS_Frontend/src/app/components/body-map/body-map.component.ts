import {
  Component,
  Input,
  Output,
  EventEmitter,
  ElementRef,
  ViewChild,
  AfterViewInit,
  OnChanges,
  SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-body-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './body-map.component.html',
  styleUrls: ['./body-map.component.css']
})
export class BodyMapComponent implements AfterViewInit, OnChanges {
  @Input() injuries: { bodyPart: string }[] = [];
  @Input() mode: 'view' | 'edit' = 'edit';
  @Output() bodyPartSelected = new EventEmitter<string>();

  @ViewChild('humanBody', { static: true }) bodyContainerRef!: ElementRef<HTMLElement>;

  selectedPart: string = '';

  // Convert DB name back to SVG ID for highlighting
  private dbNameToSvgId(dbName: string): string {
    const mapping: { [key: string]: string } = {
      'Head': 'head',
      'Eyes': 'orbit',
      'Face': 'face',
      'Neck': 'neck',
      'Left Shoulder': 'left-shoulder',
      'Right Shoulder': 'right-shoulder',
      'Left Arm': 'left-arm',
      'Right Arm': 'right-arm',
      'Left Hand': 'left-hand',
      'Right Hand': 'right-hand',
      'Chest': 'chest',
      'Back': 'back',
      'Abdomen': 'abdomen',
      'Left Leg': 'left-leg',
      'Right Leg': 'right-leg',
      'Left Foot': 'left-foot',
      'Right Foot': 'right-foot'
    };
    
    return mapping[dbName] || dbName.toLowerCase().replace(/\s+/g, '-');
  }

  ngAfterViewInit() {
    this.applyHighlighting(); // Initial rendering
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['injuries'] && this.mode === 'view') {
      setTimeout(() => this.applyHighlighting(), 0); // Wait for DOM update
    }
  }

  private applyHighlighting() {
    const container = this.bodyContainerRef?.nativeElement;
    if (!container || !this.injuries) return;

    // Reset all
    container.querySelectorAll('svg').forEach(svg => {
      svg.classList.remove('injured-part');
    });

    // Apply highlighting to injured parts
    this.injuries.forEach(injury => {
      const svgId = this.dbNameToSvgId(injury.bodyPart);
      const partEl = container.querySelector(`svg#${svgId}`);
      if (partEl) {
        partEl.classList.add('injured-part');
      } else {
        console.warn(`❗ Partie non trouvée dans SVG : ${svgId} (nom DB: ${injury.bodyPart})`);
      }
    });
  }


  onSvgClick(event: MouseEvent) {
    if (this.mode === 'edit') {
      const target = event.target as SVGElement;
      const svgElement = target.closest('svg');
      if (svgElement && svgElement.id) {
        this.selectedPart = svgElement.id;
        this.bodyPartSelected.emit(this.selectedPart);

        // Clear all selections
        this.clearAllSelections();
        svgElement.classList.add('selected');
      }
    }
  }

  onBackClick() {
    if (this.mode === 'edit') {
      this.selectedPart = 'back';
      this.bodyPartSelected.emit(this.selectedPart);
      
      // Clear all selections and select back
      this.clearAllSelections();
      const backElement = this.bodyContainerRef.nativeElement.querySelector('.back-body-part');
      if (backElement) {
        backElement.classList.add('selected');
      }
    }
  }

  private clearAllSelections() {
    const container = this.bodyContainerRef?.nativeElement;
    if (!container) return;

    // Clear SVG selections
    container.querySelectorAll('svg').forEach(el => el.classList.remove('selected'));
    
    // Clear back button selection
    const backElement = container.querySelector('.back-body-part');
    if (backElement) {
      backElement.classList.remove('selected');
    }
  }

  isBackInjured(): boolean {
    if (!this.injuries || this.mode !== 'view') return false;
    return this.injuries.some(injury => injury.bodyPart === 'Back');
  }
}
