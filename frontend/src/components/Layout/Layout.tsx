import type { ReactNode } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { useEmailImportStatus } from '../../hooks/useSources'
import { AppIcon, type AppIconName } from '../Icon/AppIcon'
import './Layout.css'

interface LayoutProps {
  children: ReactNode
}

const navItems: Array<{ to: string; label: string; mobileLabel: string; icon: AppIconName }> = [
  { to: '/', label: 'Vue d’ensemble', mobileLabel: 'Accueil', icon: 'dashboard' },
  { to: '/listings', label: 'Annonces', mobileLabel: 'Biens', icon: 'building' },
  { to: '/search-profiles', label: 'Mes recherches', mobileLabel: 'Recherches', icon: 'search' },
  { to: '/sources', label: 'Sources', mobileLabel: 'Sources', icon: 'database' },
  { to: '/import', label: 'Importer un bien', mobileLabel: 'Importer', icon: 'upload' },
]

/**
 * Application shell: a top header with the navigation (a bottom tab bar on
 * tablet and below) and the page content underneath.
 */
export function Layout({ children }: LayoutProps) {
  const { data: emailImportStatus } = useEmailImportStatus()
  const mailboxReady = emailImportStatus?.isConfigured === true
  return (
    <div className="layout">
      <header className="layout-header">
        <div className="layout-header-inner">
          <Link to="/" className="layout-brand" aria-label="ImmoDigger — accueil">
            <span className="layout-brand-mark" aria-hidden="true">
              <AppIcon name="home" />
            </span>
            <span className="layout-brand-name">
              Immo<b>Digger</b>
            </span>
          </Link>

          <nav className="layout-nav" aria-label="Navigation principale">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  isActive ? 'layout-nav-link active' : 'layout-nav-link'
                }
              >
                <AppIcon name={item.icon} className="layout-nav-icon" />
                <span className="layout-nav-desktop-label">{item.label}</span>
                <span className="layout-nav-mobile-label">{item.mobileLabel}</span>
              </NavLink>
            ))}
          </nav>

          <div className="layout-header-aside">
            <Link
              to="/sources"
              className={`layout-mailbox${mailboxReady ? '' : ' layout-mailbox--offline'}`}
              title={mailboxReady
                ? `${emailImportStatus.supportedSources.length} sources e-mail`
                : 'Ouvrir les sources'}
            >
              <span className="layout-status-dot" aria-hidden="true" />
              {mailboxReady ? 'Boîte connectée' : 'Boîte à configurer'}
            </Link>
            <span className="layout-avatar" title="Fabien Hance">FH</span>
          </div>
        </div>
      </header>

      <main className="layout-content">{children}</main>
    </div>
  )
}
