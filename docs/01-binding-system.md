# The Binding Engine

## 1. Thread Safety

Terminal.Gui v2 is multi-threaded for input, but the UI is not. Any update from a background thread must be marshalled back to the UI thread.

- **Automatic Marshalling**: Bindings detect the current thread and use `App.Invoke` when necessary.
- **Async Support**: ViewModels can use `async/await` safely without manual UI-thread boilerplate.

## 2. Binding Primitives

The engine provides low-level building blocks for data synchronization.

- **One-Way**: Subscribes to `INotifyPropertyChanged` and pushes ViewModel changes to the UI.
- **Two-Way**: Synchronizes state between the UI and ViewModel using a re-entrancy guard (`updating` flag) to prevent circular update loops.
- **Initial Sync**: Every binding immediately synchronizes the UI with the current ViewModel state upon creation.

## 3. Command Integration

Bridges the `ICommand` interface to interactive UI elements.

- **Execution**: Maps Button activation (Accepting event) to `Command.Execute`.
- **Enablement**: Automatically synchronizes a Button’s `Enabled` state with `ICommand.CanExecute` across thread boundaries.

## 4. Managed Lifecycle

Because bindings create event subscriptions, they must be tracked to prevent memory leaks.

- **BindingContext**: A container used to collect the `IDisposable` tokens returned by binding methods.
- **Manual Management**: In standalone usage, the developer is responsible for registering bindings into a context and ensuring that context is disposed of when the View is destroyed to detach event handlers.
