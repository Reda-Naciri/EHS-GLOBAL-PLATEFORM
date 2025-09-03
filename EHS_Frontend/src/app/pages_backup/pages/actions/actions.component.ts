import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; // ✅ ajout pour ngModel
import { Action, USERS, ACTIONS } from '../../data/demo-data'; // ✅ import ES normal

@Component({
  selector: 'app-actions',
  standalone: true,
  imports: [CommonModule, FormsModule], // ✅ FormsModule ajouté ici
  templateUrl: './actions.component.html',
  styleUrls: ['./actions.component.css']
})
export class ActionsComponent {
  actions: Action[] = ACTIONS; // ✅ import direct
  filteredActions: Action[] = [];

  statuses = ['All', 'Completed', 'In Progress', 'Not Started'];
  hierarchies = ['All', 'Elimination', 'Substitution', 'Mesure d\'ingenierie', 'Mesures Administratives', 'EPI'];

  selectedStatus = 'All';
  selectedHierarchy = 'All';
  searchTerm = '';

  ngOnInit(): void {
    this.filterActions();
  }

  get totalActions(): number {
    return this.actions.length;
  }

  get completedActions(): number {
    return this.actions.filter(a => a.status === 'Completed').length;
  }

  get overdueActions(): number {
    const today = new Date();
    return this.actions.filter(a => new Date(a.dueDate) < today && a.status !== 'Completed').length;
  }

  get completionRate(): string {
    return this.totalActions === 0
      ? '0%'
      : `${Math.round((this.completedActions / this.totalActions) * 100)}%`;
  }

  getUserName(id: string): string {
    const user = USERS.find(u => u.id === id);
    return user ? `${user.firstName} ${user.lastName}` : 'Unknown';
  }

  formatDate(date: string): string {
    if (!date) return '';
    return new Date(date).toLocaleDateString();
  }

  filterActions(): void {
    this.filteredActions = this.actions.filter(action => {
      const matchesStatus = this.selectedStatus === 'All' || action.status === this.selectedStatus;
      const matchesHierarchy = this.selectedHierarchy === 'All' || action.hierarchy === this.selectedHierarchy;
      const matchesSearch = this.searchTerm === '' ||
        action.title.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        action.description.toLowerCase().includes(this.searchTerm.toLowerCase());

      return matchesStatus && matchesHierarchy && matchesSearch;
    });
  }
}
