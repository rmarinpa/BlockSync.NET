# BlockSync.NET Frontend - Design Document

## Aesthetic Vision: Cyberpunk Terminal Brutalism

Inspired by **mempool.space**, this frontend channels a dark, hacker-terminal aesthetic with blockchain-inspired data visualization.

### Core Design Principles

1. **Terminal Authenticity**
   - Ultra-dark backgrounds (#0a0e14)
   - Monospace typography (JetBrains Mono)
   - CRT scanline effects
   - Phosphor green (#39ff14) primary accent
   - ASCII-art borders and decorations

2. **Blockchain Visual Language**
   - Hash strings displayed as glowing monospace codes
   - Block-based data visualization
   - Status indicators with glow effects
   - Ledger-style transaction displays

3. **Cyberpunk Color Theory**
   ```
   Background:    #0a0e14  (Deep space black)
   Surface:       #0d1117  (Terminal surface)
   Success:       #39ff14  (Phosphor green)
   Error:         #ff3838  (Alert red)
   Warning:       #ffdd00  (Caution yellow)
   Data:          #00ffff  (Cyan data streams)
   Text:          #c9d1d9  (Terminal text)
   Muted:         #8b949e  (Dimmed text)
   ```

4. **Motion Design**
   - Scanline animation (8s loop)
   - Glow pulse on status indicators
   - Slide-down reveals for new data
   - Smooth color transitions
   - Hover state enhancements

## Component Architecture

### Layout System

```
┌─────────────────────────────────────────────────────┐
│ Header (Sticky)                                     │
│ ╔═══╗ BlockSync.NET    [Nav Items]                 │
└─────────────────────────────────────────────────────┘
│                                                     │
│ Main Content Area (Container)                      │
│                                                     │
│ ┌─────────────────────────────────────────────┐    │
│ │ Terminal Window                             │    │
│ │ ● ● ●  Title                                │    │
│ │ ────────────────────────────────────────── │    │
│ │ Content with data visualization            │    │
│ └─────────────────────────────────────────────┘    │
│                                                     │
└─────────────────────────────────────────────────────┘
│ Footer (Status bar)                                │
│ ● System Online  |  API: localhost:5000            │
└─────────────────────────────────────────────────────┘
```

### Terminal Window Component

Every major data container uses the "terminal window" pattern:

```html
<div class="terminal-window">
  <div class="terminal-header">
    <div class="terminal-dot bg-terminal-red"></div>
    <div class="terminal-dot bg-terminal-yellow"></div>
    <div class="terminal-dot bg-terminal-green"></div>
    <div class="terminal-title">Window Title</div>
  </div>
  <div class="terminal-content">
    <!-- Data content -->
  </div>
</div>
```

This creates the macOS-style traffic lights + consistent framing for all data displays.

## Page Designs

### 1. Dashboard (/)

**Purpose:** System overview at a glance

**Layout:**
- System status header (origin/destination/sync state)
- 4-card stat grid (total blocks, synchronized, corrupted, pending)
- Progress bar with percentage
- ASCII blockchain ledger status display

**Key Features:**
- Auto-refresh every 5 seconds
- Real-time sync percentage
- Visual estado indicators
- ASCII art summary

**Visual Hierarchy:**
```
[System Header - 3 columns]
    ↓
[Stat Cards Grid - 4 items]
    ↓
[Progress Bar with %-age]
    ↓
[ASCII Ledger Status]
```

### 2. Ledger (/ledger)

**Purpose:** Complete blockchain ledger inspection

**Layout:**
- Stats header (5-stat summary)
- Filter buttons (ALL, SINCRONIZADO, CORRUPTO, PENDIENTE, ERROR)
- Data table with expandable rows

**Table Columns:**
- Periodo (green, bold)
- Hash (truncated, cyan monospace)
- Estado (badge with color)
- Registros (right-aligned)
- Monto Total (cyan, currency format)
- Última Acción (SKIP/INSERT/REPAIR with colors)
- Última Sync (date/time)

**Expandable Row Details:**
- Full hash (green monospace)
- Created/Updated timestamps
- Block metadata box (ASCII border)

**Interactions:**
- Click row to expand
- Filter buttons toggle view
- Auto-refresh every 10s

### 3. Hashes (/hashes)

**Purpose:** Side-by-side hash comparison matrix

**Layout:**
- Stats header (4 metrics)
- Grid of block comparison cards
- Legend at bottom

**Block Card Structure:**
```
┌────────────────────────────┐
│ ✓  2023-06  SINCRONIZADO   │
├────────────────────────────┤
│ ORIGEN:                    │
│ [hash in green]            │
│ Registros: 20,183          │
│ Monto: $50,390,128.43      │
│                            │
│         ⇅ (green if match) │
│                            │
│ DESTINO:                   │
│ [hash - green if match]    │
│ Registros: 20,183          │
│ Monto: $50,390,128.43      │
└────────────────────────────┘
```

**Color Logic:**
- SINCRONIZADO: Green border, ✓ icon
- CORRUPTO: Red border, ⚠ icon, pulsing arrow
- FALTA_EN_DESTINO: Yellow border, ○ icon

**Responsive Grid:**
- 1 column mobile
- 2 columns tablet
- 3 columns desktop

### 4. Actions (/actions)

**Purpose:** Execute system operations

**Layout:**
- 3-card action grid:
  1. Sync (green accent)
  2. Hack (yellow accent)
  3. Reset (red accent)

**Action Cards:**
- Icon + Title
- Description text
- Interactive inputs (for hack: year/month)
- Action button with loading state

**Result Displays:**
- Sync Result: Stats grid + ASCII summary + block details table
- Hack Result: Warning box + instructions
- Reset Result: Confirmation + actions list + next step

**Visual Feedback:**
- Loading spinners
- Slide-down result animations
- Color-coded success/warning/error states

### 5. Diagnostics (/diagnostics)

**Purpose:** Deep system inspection

**Layout:**
- Memory stats (5-metric row)
- Origin vs Destination comparison (2-column grid)
- Random sample table (refreshable)
- Top 10 periods by records

**Memory Display:**
- Used MB (red)
- Total MB (cyan)
- GC generation counts

**Data Statistics:**
- Total records/blocks
- Amount totals/averages/min/max
- Unique clients/products
- Date ranges

**Sample Table:**
- ID (truncated hash, green)
- Fecha, Cliente, Producto
- Monto (cyan, currency)
- Periodo (badge)
- Refresh button in header

**Top 10 Display:**
- Numbered ranking (#1-10 in green)
- Period, records, total amount
- Hash preview

## Typography Scale

```
Hero Numbers:       3xl (30px)  - Stats displays
Large Numbers:      2xl (24px)  - Secondary stats
Titles:            xl  (20px)  - Section headers
Body:              base (16px) - Regular text
Small:             sm  (14px)  - Table cells
Tiny:              xs  (12px)  - Labels, hashes
```

All using **JetBrains Mono** for that crisp terminal feel.

## Animation Catalog

1. **scanline** (8s loop)
   - Moving horizontal line across screen
   - Creates CRT monitor effect

2. **glow** (2s alternate)
   - Text shadow pulse on status indicators
   - Hash strings glow effect

3. **slide-down** (0.3s ease-out)
   - Result panels reveal
   - Smooth entry animation

4. **fade-in** (0.5s ease-out)
   - Page load transition
   - Content reveal

5. **pulse** (infinite)
   - Status dots
   - Error indicators

6. **spin** (1s linear infinite)
   - Loading spinners
   - Activity indicators

## Responsive Breakpoints

```css
sm:  640px   /* Mobile landscape */
md:  768px   /* Tablet */
lg:  1024px  /* Desktop */
xl:  1280px  /* Large desktop */
```

**Mobile Adaptations:**
- Single column layouts
- Stacked stat cards
- Simplified tables (scroll horizontal)
- Collapsed navigation

**Desktop Enhancements:**
- Multi-column grids
- Full hash displays
- Expanded data tables
- Side-by-side comparisons

## State Management

**Loading States:**
- Centered spinner
- Skeleton screens (optional)
- Button loading text

**Error States:**
- Red terminal window
- ⚠ icon
- Error message
- Retry button

**Empty States:**
- "No data" message
- Subtle muted text
- Action suggestion

**Success States:**
- Green indicators
- ✓ checkmarks
- Glow effects

## Accessibility Considerations

- High contrast (WCAG AA compliant)
- Keyboard navigation support
- Focus indicators (green outline)
- Semantic HTML
- ARIA labels on interactive elements
- Alt text for icons

## Performance Optimizations

- Auto-refresh intervals (5-10s)
- Efficient React re-renders
- Table virtualization (for 1M records)
- Lazy loading images
- CSS-only animations (no JS)
- Optimized bundle size

## Browser Support

- Chrome/Edge 100+
- Firefox 100+
- Safari 15+
- Mobile browsers (iOS Safari, Chrome Android)

## Future Enhancements

1. **WebSocket Real-Time Updates**
   - Replace polling with live connections
   - Instant hash comparison updates

2. **Dark/Light Theme Toggle**
   - User preference storage
   - Smooth theme transition

3. **Data Visualization**
   - Charts (sync history over time)
   - Block size distribution
   - Performance metrics graphs

4. **Advanced Filtering**
   - Search by periodo, hash
   - Date range filters
   - Amount range filters

5. **Export Functionality**
   - CSV export of ledger
   - JSON export of diagnostics
   - PDF reports

---

**Design Philosophy:**
> "Make it feel like you're accessing a blockchain node through a 1980s terminal running on a 2026 quantum computer. Brutally functional, impossibly cool."

**Visual Goal:**
> Every pixel should scream "I'm looking at real blockchain data" even though it's just hash comparisons. The aesthetic IS the feature.
