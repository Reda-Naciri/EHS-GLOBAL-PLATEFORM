export const environment = {
  production: false,
  apiUrl: `http://${window.location.hostname}:5225/api`,
  endpoints: {
    auth: '/auth',
    reports: '/reports',
    users: '/users',
    dashboard: '/dashboard',
    files: '/files',
    actions: '/actions',
    correctiveActions: '/corrective-actions',
    registerRequest: '/register-request',
    pendingUsers: '/pending-users'
  }
};
