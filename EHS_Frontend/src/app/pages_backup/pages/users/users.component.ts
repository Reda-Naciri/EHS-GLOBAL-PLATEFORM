import { Component, HostListener, Renderer2, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { USERS, User } from '../../data/demo-data';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css']
})
export class UsersComponent {
  searchQuery: string = '';
  users: User[] = USERS;
  roles = ['Admin', 'HSE', 'Profil'];
  dropdownOpen = false;
  sidebarOpen = false;

  currentPage: number = 1;
  pageSize: number = 7;

  constructor(private renderer: Renderer2, private el: ElementRef) { }

  get paginatedUsers(): User[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredUsers().slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredUsers().length / this.pageSize);
  }

  filteredUsers() {
    return this.users.filter(user => {
      const fullName = `${user.firstName} ${user.lastName}`.toLowerCase();
      return (
        fullName.includes(this.searchQuery.toLowerCase()) ||
        (user.email?.toLowerCase().includes(this.searchQuery.toLowerCase()) || false) ||
        (user.role?.toLowerCase().includes(this.searchQuery.toLowerCase()) || false)
      );
    });
  }


  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  openUserModal() {
    console.log("Open add user modal");
  }

  editUser(user: User) {
    console.log("Edit user:", user);
  }

  deleteUser(userId: string) {
    this.users = this.users.filter(user => user.id !== userId);
    console.log("Deleted user with ID:", userId);
  }

  updateUserRole(user: User) {
    console.log(`Updated role for ${user.fullName} to ${user.role}`);
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  toggleSidebar() {
    this.sidebarOpen = !this.sidebarOpen;
    const sidebar = this.el.nativeElement.querySelector('.sidebar');
    if (this.sidebarOpen) {
      this.renderer.addClass(sidebar, 'open');
    } else {
      this.renderer.removeClass(sidebar, 'open');
    }
  }

  @HostListener('document:click', ['$event'])
  closeSidebar(event: Event) {
    if (this.sidebarOpen) {
      const sidebar = this.el.nativeElement.querySelector('.sidebar');
      const hamburger = this.el.nativeElement.querySelector('.hamburger');
      if (!sidebar.contains(event.target as Node) && !hamburger.contains(event.target as Node)) {
        this.renderer.removeClass(sidebar, 'open');
        this.sidebarOpen = false;
      }
    }
  }

  logout() {
    console.log("Logging out...");
  }
}
