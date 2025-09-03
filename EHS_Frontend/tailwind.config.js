module.exports = {
  darkMode: "class", // Force Tailwind à utiliser la classe "dark" et non les préférences système
  content: [
    "./src/**/*.{html,ts}",
  ],
  safelist: [
    // Notification background colors
    'bg-blue-100', 'text-blue-800',
    'bg-yellow-100', 'text-yellow-800', 
    'bg-orange-100', 'text-orange-800',
    'bg-red-100', 'text-red-800',
    'bg-green-100', 'text-green-800',
    'bg-gray-100', 'text-gray-800',
    // Notification icons
    'fas', 'fa-sync', 'fa-plus-circle', 'fa-file-alt', 'fa-user-check', 'fa-comment', 'fa-tasks', 'fa-calendar-day',
    'fa-exclamation-triangle', 'fa-clock', 'fa-hourglass-half', 'fa-user-plus',
    'fa-ban', 'fa-times-circle', 'fa-exclamation-circle', 'fa-radiation',
    'fa-info-circle', 'fa-check-circle',
    'text-blue-500', 'text-yellow-500', 'text-red-500', 'text-green-500'
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
