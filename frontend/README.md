# BlockSync.NET Frontend

Terminal-inspired frontend interface for BlockSync.NET blockchain integrity sync engine.

## Design Aesthetic

**Cyberpunk Terminal Brutalism** - Dark mode interface inspired by mempool.space with:
- Ultra-dark background (#0a0e14) with phosphor green (#39ff14) accents
- JetBrains Mono monospace typography
- CRT scanline effects and terminal aesthetics
- Real-time data visualization with hash comparison
- ASCII-art borders and blockchain ledger displays

## Tech Stack

- **Framework:** Vite + React 18 + TypeScript
- **Styling:** TailwindCSS with custom terminal theme
- **Routing:** React Router v6
- **API:** Type-safe client generated from OpenAPI spec
- **Font:** JetBrains Mono (Google Fonts)

## Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

## Development

The app will run on `http://localhost:3000` and expects the BlockSync.NET API running on `http://localhost:5000`.

## Views

### 1. Dashboard (/)
- System status overview
- Sync statistics cards
- Progress bar visualization
- ASCII blockchain ledger status

### 2. Ledger (/ledger)
- Complete blockchain ledger table
- Filter by status (SINCRONIZADO, CORRUPTO, PENDIENTE, ERROR)
- Expandable rows with full block metadata
- Real-time updates every 10 seconds

### 3. Hashes (/hashes)
- Side-by-side hash comparison
- Visual estado indicators (✓ SINCRONIZADO, ⚠ CORRUPTO, ○ FALTA)
- Origin vs Destination block details
- Color-coded comparison matrix

### 4. Actions (/actions)
- Execute synchronization (POST /api/Sync)
- Simulate data corruption (POST /api/Sync/hack/{year}/{month})
- Reset system (POST /api/Sync/reset)
- Real-time operation results with detailed feedback

### 5. Diagnostics (/diagnostics)
- Memory usage statistics
- Origin vs Destination data comparison
- Random data samples (refreshable)
- Top 10 periods by record count

## API Configuration

Edit `src/lib/api.ts` to change the API base URL:

```typescript
const API_BASE = 'http://localhost:5000/api';
```

## Color Palette

```css
terminal-bg:         #0a0e14  /* Ultra-dark background */
terminal-surface:    #0d1117  /* Card backgrounds */
terminal-border:     #1c2128  /* Borders */
terminal-text:       #c9d1d9  /* Primary text */
terminal-muted:      #8b949e  /* Secondary text */
terminal-green:      #39ff14  /* Success/Synchronized */
terminal-green-dim:  #2a9d0f  /* Hover states */
terminal-red:        #ff3838  /* Error/Corrupted */
terminal-yellow:     #ffdd00  /* Warning/Pending */
terminal-cyan:       #00ffff  /* Data highlights */
```

## Effects

- **CRT Scanlines:** Subtle scanline overlay for terminal aesthetic
- **Glow Text:** Text-shadow glow on status indicators and hashes
- **Animations:** Smooth transitions, pulse effects, and slide-downs
- **Hover States:** Enhanced shadows and color shifts on cards

## Type Safety

All TypeScript types are generated from the OpenAPI specification in `/src/types/api.ts`, ensuring complete type safety between frontend and backend.

## Performance

- Automatic polling intervals for real-time updates
- Optimized re-renders with React hooks
- Lazy loading for large data tables
- Efficient hash comparison rendering

---

**© 2026 BlockSync.NET - MerkleFlow Core**
