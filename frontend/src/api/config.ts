/**
 * Base URL of the ImmoDigger backend API.
 * Configurable via the VITE_API_BASE_URL environment variable
 * (see .env.example at the repository root).
 */
export const API_BASE_URL: string =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api'
