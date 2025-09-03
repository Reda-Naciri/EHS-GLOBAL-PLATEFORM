import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {
  expandedSection: 'apps' | 'reports' | null = null;

  toggleSection(section: 'apps' | 'reports') {
    if (this.expandedSection === section) {
      this.expandedSection = null; // Collapse if clicked again
    } else {
      this.expandedSection = section; // Expand the clicked section
    }
  }
}
