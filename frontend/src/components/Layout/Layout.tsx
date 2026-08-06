import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import './Layout.css'

interface LayoutProps {
  children: ReactNode
}

const navItems = [
  { to: '/', label: 'Tableau de bord' },
  { to: '/listings', label: 'Annonces' },
  { to: '/search-profiles', label: 'Profils de recherche' },
  { to: '/sources', label: 'Sources' },
]

/**
 * Application shell: top navigation + content area.
 * Page content (Dashboard, Listings, ...) is added incrementally.
 */
export function Layout({ children }: LayoutProps) {
  return (
    <div className="layout">
      <header className="layout-header">
        <div className="layout-brand">ImmoDigger</div>
        <nav className="layout-nav">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                isActive ? 'layout-nav-link active' : 'layout-nav-link'
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </header>
      <main className="layout-content">{children}</main>
    </div>
  )
}
