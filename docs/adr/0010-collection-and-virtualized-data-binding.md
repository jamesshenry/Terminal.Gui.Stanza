---
status: accepted
---

# ADR 10: Collection Data Binding and Virtualized Data Sources

## Context

Standard MVVM architectures represent lists of items using `ObservableCollection<T>`. However, Terminal.Gui's `ListView` and `TableView` do not use native view-trees to display rows; they use low-level flat rendering interfaces (`IListDataSource` and `ITableSource`) directly bound to the console buffer to optimize performance [1.1.6, 1.2.6].

Stanza needs a declarative way to bind ViewModel collections to these flat sources without requiring the developer to manually implement these custom datasources in their view code-behind.

## Decision

Provide pre-built, generic adapter implementations of `IListDataSource` and `ITableSource` inside `Stanza.TerminalGui` that bridge `ObservableCollection<T>` directly to Terminal.Gui views:

1. Expose a `BindItemsSource` placeholder on list views.
2. The runtime library provides `StanzaListDataSource<T>` which implements `IListDataSource` and automatically listens to `INotifyCollectionChanged` events.
3. When collection changes occur on the ViewModel, the data source wrapper automatically invokes `ListView.SetNeedsDraw()` to trigger a redraw on the main thread.
4. The generator recognizes `BindItemsSource = nameof(MyViewModel.MyCollection)` and emits the assignment of the generated adapter.

## Consequences

### Positive

- High-performance, flat rendering of lists is preserved [1.1.6, 1.2.6].
- Developers can use standard `ObservableCollection<T>` patterns on their ViewModels.
- UI elements automatically update when items are added, removed, or cleared from the ViewModel collection.

### Negative

- Cells do not support individual complex UI control hierarchies; list rendering remains flat-text as mandated by the Terminal.Gui engine [1.1.6, 1.2.6].
- Custom formatting of rows requires developers to pass formatting lambdas or converters to the data source adapters.
