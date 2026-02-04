import { Link, Outlet, useLocation } from 'react-router-dom';

export function Layout() {
  const location = useLocation();

  const navItems = [
    { path: '/', label: 'Dashboard', icon: '📊' },
    { path: '/ledger', label: 'Ledger', icon: '📋' },
    { path: '/hashes', label: 'Hashes', icon: '🔗' },
    { path: '/actions', label: 'Actions', icon: '⚡' },
    { path: '/diagnostics', label: 'Diagnostics', icon: '🔍' },
  ];

  return (
    <div className="min-h-screen flex flex-col bg-neutral-50">
      {/* Header */}
      <header className="bg-white border-b border-neutral-200 sticky top-0 z-50 shadow-sm">
        <div className="container-custom">
          <div className="flex items-center justify-between py-4">
            {/* Logo */}
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-primary-500 to-primary-600 rounded-lg flex items-center justify-center shadow-sm">
                <svg
                  className="w-6 h-6 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"
                  />
                </svg>
              </div>
              <div>
                <h1 className="text-xl font-bold text-neutral-900">
                  BlockSync<span className="gradient-text">.NET</span>
                </h1>
                <p className="text-xs text-neutral-500">
                  Enterprise Data Sync Platform
                </p>
              </div>
            </div>

            {/* Navigation */}
            <nav className="flex gap-1">
              {navItems.map((item) => {
                const isActive = location.pathname === item.path;
                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    className={`
                      px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200
                      flex items-center gap-2
                      ${
                        isActive
                          ? 'bg-primary-50 text-primary-600'
                          : 'text-neutral-600 hover:bg-neutral-50 hover:text-neutral-900'
                      }
                    `}
                  >
                    <span className="text-base">{item.icon}</span>
                    <span>{item.label}</span>
                  </Link>
                );
              })}
            </nav>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 py-8">
        <div className="container-custom">
          <Outlet />
        </div>
      </main>

      {/* Footer */}
      <footer className="bg-white border-t border-neutral-200 py-6">
        <div className="container-custom">
          <div className="flex items-center justify-between text-sm text-neutral-500">
            <div className="flex items-center gap-6">
              <div className="flex items-center gap-2">
                <span className="status-dot-success"></span>
                <span>System Online</span>
              </div>
              <div>API: http://localhost:5000</div>
            </div>
            <div className="flex items-center gap-4">
              <div>MerkleFlow Core v1.0.0</div>
              <div>© 2026 BlockSync.NET</div>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
