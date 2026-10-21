export const environment = {
  production: false,
  apiUrl: '/api',
  endpoints: {
    auth: '/auth',
    reports: '/reports',
    users: '/users',
    dashboard: '/dashboard',
    files: '/files',
    actions: '/actions',
    correctiveActions: '/corrective-actions',
    registerRequest: '/register-request',
    pendingUsers: '/pending-users',
    notifications: '/notifications',
    adminParameters: '/admin-parameters',
    applications: '/applications'
  }
};

console.log('🔧 Environment: Configuration loaded', environment);
