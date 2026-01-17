import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { docsConfig, type NavItem } from '../docs.config';
import './Layout.css';

interface LayoutProps {
  children: React.ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();
  const currentPath = location.pathname;

  const activeExpandKeys = useMemo(() => {
    const keys = new Set<string>();
    for (const item of docsConfig.nav) {
      collectActiveBranchKeys(item, currentPath, keys);
    }
    return keys;
  }, [currentPath]);

  const [expanded, setExpanded] = useState<Record<string, boolean>>(() => {
    const next: Record<string, boolean> = {};

    // Default: expand label-only groups (Reference, Tools, etc.)
    for (const item of docsConfig.nav) {
      if (!item.path && item.children?.length) {
        next[getNavKey(item)] = true;
      }
    }

    // Expand the active branch on first render
    for (const key of activeExpandKeys) {
      next[key] = true;
    }

    return next;
  });

  // Keep the active branch expanded as navigation changes (without collapsing user-expanded groups).
  useEffect(() => {
    setExpanded((prev) => {
      const next = { ...prev };
      for (const key of activeExpandKeys) {
        next[key] = true;
      }
      return next;
    });
  }, [activeExpandKeys]);

  return (
    <div className="layout">
      {/* Header */}
      <header className="header">
        <div className="header-left">
          <button
            className="menu-toggle"
            onClick={() => setSidebarOpen(!sidebarOpen)}
            aria-label="Toggle navigation"
          >
            <span className="menu-icon"></span>
          </button>
          <Link to="/" className="logo">
            <span className="logo-text">{docsConfig.title}</span>
            <span className="logo-badge">Docs</span>
          </Link>
        </div>
        <nav className="header-nav">
          <a
            href={docsConfig.links.github}
            target="_blank"
            rel="noopener noreferrer"
            className="header-link"
          >
            GitHub
          </a>
          <a
            href={docsConfig.links.thunderstoreServer}
            target="_blank"
            rel="noopener noreferrer"
            className="header-link"
          >
            Thunderstore
          </a>
        </nav>
      </header>

      <div className="layout-body">
        {/* Sidebar */}
        <aside className={`sidebar ${sidebarOpen ? 'sidebar-open' : ''}`}>
          <nav className="sidebar-nav">
            {docsConfig.nav.map((item) => (
              <NavSection
                key={item.title}
                item={item}
                currentPath={currentPath}
                onNavigate={() => setSidebarOpen(false)}
                expanded={expanded}
                setExpanded={setExpanded}
              />
            ))}
          </nav>
        </aside>

        {/* Backdrop for mobile */}
        {sidebarOpen && (
          <div
            className="sidebar-backdrop"
            onClick={() => setSidebarOpen(false)}
          />
        )}

        {/* Main content */}
        <main className="main">
          <article className="content">{children}</article>
        </main>
      </div>
    </div>
  );
}

interface NavSectionProps {
  item: NavItem;
  currentPath: string;
  onNavigate: () => void;
  expanded: Record<string, boolean>;
  setExpanded: React.Dispatch<React.SetStateAction<Record<string, boolean>>>;
  depth?: number;
}

function NavSection({
  item,
  currentPath,
  onNavigate,
  expanded,
  setExpanded,
  depth = 0,
}: NavSectionProps) {
  const isActive = item.path === currentPath;
  const hasChildren = item.children && item.children.length > 0;

  // Check if this section or any children are active
  const isSectionActive =
    isActive || (item.path && currentPath.startsWith(item.path) && item.path !== '/');

  const navKey = getNavKey(item);
  const isOpen = !!expanded[navKey];

  const toggleOpen = () => {
    if (!hasChildren) return;
    setExpanded((prev) => ({ ...prev, [navKey]: !prev[navKey] }));
  };

  return (
    <div className={`nav-section ${depth > 0 ? 'nav-section-nested' : ''}`}>
      <div className="nav-row">
        {item.path ? (
          <Link
            to={item.path}
            className={`nav-link ${isActive ? 'nav-link-active' : ''} ${
              isSectionActive ? 'nav-link-section-active' : ''
            }`}
            onClick={onNavigate}
          >
            <span className="nav-title">{item.title}</span>
            {item.badge && <span className="nav-badge">{item.badge}</span>}
          </Link>
        ) : (
          <button
            type="button"
            className="nav-group"
            onClick={toggleOpen}
            aria-expanded={hasChildren ? isOpen : undefined}
          >
            <span className="nav-title">{item.title}</span>
          </button>
        )}

        {hasChildren && (
          <button
            type="button"
            className={`nav-expand ${isOpen ? 'nav-expand-open' : ''}`}
            aria-label={isOpen ? `Collapse ${item.title}` : `Expand ${item.title}`}
            aria-expanded={isOpen}
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              toggleOpen();
            }}
          >
            <span className="nav-expand-icon">▾</span>
          </button>
        )}
      </div>

      {hasChildren && (
        <div className={`nav-children-wrapper ${isOpen ? 'is-open' : ''}`}>
          <div className="nav-children">
          {item.children!.map((child) => (
            <NavSection
              key={child.title}
              item={child}
              currentPath={currentPath}
              onNavigate={onNavigate}
              expanded={expanded}
              setExpanded={setExpanded}
              depth={depth + 1}
            />
          ))}
          </div>
        </div>
      )}
    </div>
  );
}

function getNavKey(item: NavItem): string {
  return item.path ?? `group:${item.title}`;
}

function collectActiveBranchKeys(item: NavItem, currentPath: string, out: Set<string>): boolean {
  const selfActive =
    item.path === currentPath || (!!item.path && item.path !== '/' && currentPath.startsWith(item.path));

  const children = item.children ?? [];
  let childActive = false;
  for (const child of children) {
    if (collectActiveBranchKeys(child, currentPath, out)) {
      childActive = true;
    }
  }

  const active = selfActive || childActive;
  if (active && children.length > 0) {
    out.add(getNavKey(item));
  }

  return active;
}
