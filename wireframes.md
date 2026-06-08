# Responsive Layout Wireframes

Visual exploration of responsive terminal UI patterns. Each layout adapts across viewport widths: **S** (< 60), **M** (< 100), **L** (< 140), **XL** (≥ 140).

---

## Layout 1: Sidebar + Content (Classic 3-Pane)

**Use case**: Navigation sidebar + main content area. Used in MainWindow.cs demo.

### Small (S < 60)

```
┌─────────────────────────┐
│ Header                  │
├─────────────────────────┤
│ Content (full width)    │
│ Sidebar hidden/overlay  │
├─────────────────────────┤
│ Status                  │
└─────────────────────────┘
```

- Header: W=full, H=1
- Sidebar: hidden (W=0) or overlay
- Content: W=full, H=fill-1
- Status: W=full, H=1

### Medium (60 ≤ M < 100)

```
┌─────────────────────────────────┐
│ Header                          │
├───────────┬─────────────────────┤
│ Sidebar   │ Content             │
│ (W=20)    │ (W=fill-1)          │
│           │                     │
├───────────┴─────────────────────┤
│ Status                          │
└─────────────────────────────────┘
```

- Header: W=full, H=3
- Sidebar: W=20, H=fill-1
- Content: X=right(sidebar), W=fill, H=fill-1
- Status: W=full, H=1

### Large (100 ≤ L < 140)

```
┌──────────────────────────────────────────────────┐
│ Header (H=3)                                     │
├──────────┬───────────────────────────────────────┤
│ Sidebar  │ Content (full area)                   │
│ W=30     │                                       │
│          │                                       │
│          │                                       │
├──────────┴───────────────────────────────────────┤
│ Status (H=1)                                     │
└──────────────────────────────────────────────────┘
```

- Header: W=full, H=3
- Sidebar: W=30, H=fill-1
- Content: X=right(sidebar), W=fill, H=fill-1
- Status: W=full, H=1

### Extra Large (XL ≥ 140)

```
┌────────────────────────────────────────────────────────────────────┐
│ Header (H=3)                                                       │
├──────────┬─────────────────────────────────────────────────────────┤
│ Sidebar  │ Content (full area)                                     │
│ W=40     │                                                         │
│          │                                                         │
│          │                                                         │
│          │                                                         │
├──────────┴─────────────────────────────────────────────────────────┤
│ Status (H=1)                                                       │
└────────────────────────────────────────────────────────────────────┘
```

- Header: W=full, H=3
- Sidebar: W=40, H=fill-1
- Content: X=right(sidebar), W=fill, H=fill-1
- Status: W=full, H=1

**Dimension Rules** (pseudo):

```csharp
Header = {
    Width: Dim.Fill(),
    Height: ResponsiveDimensions(S: 1, M: 3, L: 3, XL: 3)
};

Sidebar = {
    X: 0,
    Y: Pos.Bottom(Header),
    Width: ResponsiveDimensions(S: 0, M: 20, L: 30, XL: 40),
    Height: Dim.Fill(1)
};

Content = {
    X: Pos.Right(Sidebar),
    Y: Pos.Bottom(Header),
    Width: Dim.Fill(),
    Height: Dim.Fill(1)
};

Status = {
    Y: Pos.Bottom(Content),
    Width: Dim.Fill(),
    Height: 1
};
```

---

## Layout 2: Tabular / Grid (Data Table with Sidebar)

**Use case**: Data grid responsive to viewport; sidebar for filters/actions.

### Small (S < 60)

```
┌────────────────────────────────┐
│ Filters (full width)           │
├────────────────────────────────┤
│ ID │ Name                      │
├────┼───────────────────────────┤
│  1 │ Alice                     │
│  2 │ Bob                       │
│  3 │ Charlie                   │
├────────────────────────────────┤
│ Pg 1 of 10 ◀ ▶                │
└────────────────────────────────┘
```

- Filters: full width, above table
- Table: 2 visible columns (ID, Name)
- Pagination: full width

### Medium (60 ≤ M < 100)

```
┌──────────────────────────────────────────┐
│ Filters (full)                           │
├──────────────────────────────────────────┤
│ ID │ Name      │ Email                   │
├────┼───────────┼─────────────────────────┤
│  1 │ Alice     │ alice@example.com       │
│  2 │ Bob       │ bob@example.com         │
│  3 │ Charlie   │ charlie@example.com     │
├──────────────────────────────────────────┤
│ Pg 1 of 10 ◀ Next ▶                    │
└──────────────────────────────────────────┘
```

- Filters: full width
- Table: 3 columns (ID, Name, Email)
- Pagination: centered

### Large (100 ≤ L < 140)

```
┌────────────────────────────────────────────────────────┐
│ Filters (full width)                                   │
├───────┬──────────────────────────────────────────────────┤
│Quick  │ ID │ Name      │ Email       │ Status          │
│Filter │────┼───────────┼─────────────┼─────────────────┤
│ • All │  1 │ Alice     │ alice@ex... │ ● Active        │
│ • Act │  2 │ Bob       │ bob@ex...   │ ● Active        │
│ • Inac│  3 │ Charlie   │ charlie@ex..│ ○ Inactive      │
│       │ 4  │ David     │ david@ex... │ ● Active        │
├───────┴──────────────────────────────────────────────────┤
│ Pg 1 of 10 ◀ Previous │ Next ▶ │ Last                  │
└────────────────────────────────────────────────────────┘
```

- Quick filters: left sidebar (W=7), full height
- Table: 4 columns, right area
- Pagination: full width with detailed nav

### Extra Large (XL ≥ 140)

```
┌─────────────────────────────────────────────────────────────────--──┐
│ Advanced Filters (full width, collapsible)                          │
├───────┬─────────────────────────────────────────────────────────--──┤
│Quick  │ ID │ Name      │ Email           │ Status      │ Joined     │
│Filter │────┼───────────┼─────────────────┼─────────────┼──────────--┤
│ ◆ All │  1 │ Alice     │ alice@example.. │ ● Active    │ 2024-01    │
│ ◆ Act │  2 │ Bob       │ bob@example.com │ ● Active    │ 2024-02    │
│   Inac│  3 │ Charlie   │ charlie@exa...  │ ○ Inactive  │ 2024-03    │
│       │ 4  │ David     │ david@example.. │ ● Active    │ 2024-04    │
│       │ 5  │ Eve       │ eve@example.com │ ● Active    │ 2024-05    │
├───────┴─────────────────────────────────────────────────────────────┤
│ Pg 1 of 10 ◀ Previous │ 1 2 3 4 5 │ Next ▶ │ Last │ Records: 50/100 │
└─────────────────────────────────────────────────────────────────────┘
```

- Quick filters: left sidebar (W=10), full height
- Table: 5 columns visible, right area
- Pagination: full width with page numbers

**Dimension Rules** (pseudo):

```csharp
QuickFilters = {
    X: 0,
    Y: Pos.Bottom(AdvancedFilters),
    Width: ResponsiveDimensions(S: 0, M: 0, L: 7, XL: 10),
    Height: Dim.Fill(1)
};

Table = {
    X: Pos.Right(QuickFilters),
    Y: Pos.Bottom(AdvancedFilters),
    Width: Dim.Fill(),
    Height: Dim.Fill(1)
};

Pagination = {
    Y: Pos.Bottom(Table),
    Width: Dim.Fill(),
    Height: 1
};
```

---

## Layout 3: Dashboard (Cards/Panels Grid)

**Use case**: Multi-panel dashboard. Panels reflow based on viewport width.

### Small (S < 60)

```
┌────────────────────────────────┐
│ CPU Usage                      │
│ ████████░░ 85%                 │
├────────────────────────────────┤
│ Memory                         │
│ ██████░░░░ 60%                 │
├────────────────────────────────┤
│ Network                        │
│ ███░░░░░░░ 30%                 │
├────────────────────────────────┤
│ Disk I/O                       │
│ █████████░ 90%                 │
└────────────────────────────────┘
```

- Single column layout
- Full-width cards stacked vertically

### Medium (60 ≤ M < 100)

```
┌─────────────────────────────────────────────┐
│ CPU (W=21)          │ Memory (W=21)         │
│ ████████░░ 85%      │ ██████░░░░ 60%        │
├─────────────────────┼─────────────────────┤
│ Network (W=21)      │ Disk I/O (W=21)     │
│ ███░░░░░░░ 30%      │ █████████░ 90%      │
└─────────────────────┴─────────────────────┘
```

- 2-column grid
- Each card W ≈ fill/2

### Large (100 ≤ L < 140)

```
┌────────────────────────────────────────────────────────────────┐
│ CPU        │ Memory     │ Network     │ Disk I/O               │
│ ████████░░ │ ██████░░░░ │ ███░░░░░░░  │ █████████░             │
│ 85%        │ 60%        │ 30%         │ 90%                    │
├────────────┼────────────┼─────────────┼────────────────────────┤
│ Events Log (full width, stacked below grid)                    │
│ [INFO] System startup                                          │
│ [WARN] Memory pressure detected                                │
│ [ERROR] Network timeout on node 3                              │
└────────────────────────────────────────────────────────────────┘
```

- 4-column grid for metrics
- Full-width log panel below

### Extra Large (XL ≥ 140)

```
┌──────────────────────────────────────────────────────────────────────────┐
│ CPU        │ Memory     │ Network     │ Disk I/O    │ Uptime             │
│ ████████░░ │ ██████░░░░ │ ███░░░░░░░  │ █████████░  │ 45 days 3 hrs      │
│ 85%        │ 60%        │ 30%         │ 90%         │                    │
├──────────────────────────────────────────────────────┬───────────────────┤
│ Events Log (half width)                             │ Alerts (half)     │
│ [INFO] System startup                               │ ⚠ High CPU        │
│ [WARN] Memory pressure detected                     │ ⚠ Memory: 60%      │
│ [ERROR] Network timeout on node 3                   │ 🔴 Node 3 down    │
│ [INFO] Sync completed                               │                   │
└──────────────────────────────────────────────────────┴───────────────────┘
```

- 5-column metric grid
- Log + Alerts panels side-by-side below

**Dimension Rules** (pseudo):

```csharp
MetricsGrid.ColumnCount = ResponsiveDimensions(S: 1, M: 2, L: 4, XL: 5);
MetricCard.Width = Dim.Fill() / ColumnCount;

EventsLog = {
    Y: Pos.Bottom(MetricsGrid),
    Width: ResponsiveDimensions(S: Dim.Fill(), M: Dim.Fill(), L: Dim.Fill(), XL: Dim.Fill() / 2),
    Height: Dim.Fill(1)
};

AlertsPanel = {
    X: Pos.Right(EventsLog),
    Y: Pos.Bottom(MetricsGrid),
    Width: Dim.Fill(),
    Height: Dim.Fill(1),
    Visible: ResponsiveDimensions(S: false, M: false, L: false, XL: true)
};
```

---

## Key Observations

### Responsive Patterns

1. **Responsive Visibility**: Elements appear/disappear across breakpoints
   - S: Hide sidebars, show only essential content
   - M: Small sidebars appear (W=7-20)
   - L+: Full-width sidebars (W=30-40)

2. **Column Reflow**: Multi-column grids collapse/expand
   - S: 1 column (cards stack)
   - M: 2 columns
   - L: 3-4 columns
   - XL: 4-5 columns

3. **Position Dependency**: X positions depend on sibling widths
   - `X = Pos.Right(SidebarView)` — content X shifts when sidebar width changes
   - Requires stable evaluation order

4. **Header/Spacing**: Height varies by breakpoint
   - S: H=1 (compact)
   - L+: H=3 (spacious)

5. **Text Truncation**: Table columns show fewer fields on small screens
   - S: 2 cols (ID, Name)
   - M: 3 cols (ID, Name, Email)
   - L+: 4-5 cols (ID, Name, Email, Status, Joined)

### Implementation Challenges

- **Relative Positioning**: `Pos.Right(Sidebar)` needs stable X/W evaluation
- **Dynamic Visibility**: Some panels hide on small screens; lazy init vs visibility toggle
- **Dimension Chaining**: Panel width affects children; care with evaluation order
- **Content Reflow**: Table columns must adapt; Terminal.Gui custom column width logic needed

---

## What the `ConfigureLayout()` Method Needs

Each layout pattern requires a method that:

1. **Stores references** to all views (header, sidebar, content, etc.)
2. **Computes responsive dimensions** based on viewport width
3. **Sets X/Y/Width/Height** on each view
4. **Handles position dependencies** (e.g., content X depends on sidebar width)
5. **Subscribes to viewport changes** to recompute on resize

**Example pseudocode**:

```csharp
private void ConfigureLayout()
{
    var screen = new ScreenConfiguration(this);
    
    _header.Height = Responsive.Dimension(screen, size => size switch {
        ScreenSize.S => 1,
        _ => 3
    });
    
    _sidebar.Width = Responsive.Dimension(screen, size => size switch {
        ScreenSize.S => 0,
        ScreenSize.M => 20,
        ScreenSize.L => 30,
        _ => 40
    });
    
    _content.X = Pos.Right(_sidebar);
    _content.Width = Dim.Fill();
    
    // etc...
}
```

This is where `ResponsiveDimensions` record and `Responsive.Dimension()` helper will shine.

---

## Next Step

Once we have these wireframes validated:

1. **Build runtime helpers** (Phase 1): `ScreenConfiguration`, `ResponsiveDimensions`, `Responsive`
2. **Implement wireframes** (Phase 2): Create `ConfigureLayout()` methods in concrete examples
3. **Codify patterns** (Phase 3): Source generator attributes to automate the boilerplate
