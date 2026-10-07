/**
 * Listing links come from outside (portal emails, scraped pages, manual
 * imports). Only plain web links are ever rendered as a clickable href, so
 * a crafted "javascript:" or "data:" URL can't run in the app.
 */
export function safeExternalUrl(url: string | null | undefined): string | undefined {
  if (!url) return undefined

  try {
    const parsed = new URL(url)
    return parsed.protocol === 'https:' || parsed.protocol === 'http:' ? parsed.href : undefined
  } catch {
    return undefined
  }
}
