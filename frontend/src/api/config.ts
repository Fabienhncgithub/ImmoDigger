/**
 * Base URL of the ImmoDigger backend API.
 * Configurable via the VITE_API_BASE_URL environment variable
 * (see .env.example at the repository root).
 *
 * Defaults to port 5080, not the ASP.NET Core convention of 5000: on
 * macOS, port 5000 is bound by the AirPlay Receiver (ControlCenter) by
 * default, which makes the backend fail to start silently confusing.
 */
export const API_BASE_URL: string =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080/api'
