# TCSCTS: TaskCompletionSource + Cancellation Token Samples (Tea Workflow)

This small .NET console app demonstrates several advanced orchestration patterns in `Task`-based asynchronous programming using the metaphor of making a cup of tea.

It focuses on:
- Cooperative cancellation of synchronous (CPU-ish) and asynchronous work
- Group-scoped cancellation and partial restarts
- Using `TaskCompletionSource` (TCS) as readiness gates between parallel task sets
- Modeling durable vs. ephemeral state to enable selective recovery
- Safe awaiting of possibly-cancelled tasks
- Parallel composition with `Task.WhenAll`
- Ensuring asynchronous continuations (`RunContinuationsAsynchronously`)
- Simple structured, thread-safe log output with color

## Project Layout
```
Program.cs         Entry point; executes three examples.
Examples.cs        High-level scenarios (Example1..3).
KitchenState.cs    Prepares hot water; mixes sync + async + TCS signaling.
Kettle.cs          Simple state object (Filled/Boiled).
Inventory.cs       Durable items (milk / tea bags / sugar jar).
Workspace.cs       Ephemeral items (cup / spoon / contents in cup); restart logic.
TaskHelper.cs      Utilities: BusyWait + SafeAwait wrapper.
ConsoleHelper.cs   Thread-safe colored logging.
```
Target framework: `net9.0` (from the build artifacts). No external package dependencies.

## Conceptual Highlights
### 1. Cooperative Cancellation (Sync + Async)
- Synchronous CPU-ish loops call `token.ThrowIfCancellationRequested()` periodically.
- Asynchronous waits pass the token into `Task.Delay(stepLatency, token)` so the delay is aborted immediately.
- Cancellation triggered via `CancellationTokenSource.CancelAsync()` (async-friendly in .NET 8+ / 9). Example1 shows cancelling a synchronous operation mid-loop; Example2 cancels a set of async gathers; Example3 cancels only a subset (the cup group) while other work (boiling water) continues.

### 2. TaskCompletionSource as a Gate
- `hotWaterReady` signals when hot water becomes available (after fill + boil). Dependent steps await its `Task` instead of polling.
- `cupPrereqsReady` signals that cup + spoon + milk prerequisites are satisfied. Subsequent tasks (placing teabag, adding sugar) can start in any order after the gate.
- `RunContinuationsAsynchronously` avoids running downstream continuations inline on the thread that completes the TCS, preventing potential deadlocks or long synchronous chains.

### 3. Durable vs. Ephemeral State Modeling
- Durable: milk, tea bags, sugar jar (remain after dropping the cup).
- Ephemeral: cup, spoon, contents in cup (lost if the cup is dropped).
- After cancellation (simulated drop), only ephemeral state is reset; durable state is reused. This illustrates partial restart patterns without redoing unaffected work.

### 4. Group-Scoped Cancellation
- A dedicated `CancellationTokenSource` (`cupGroup`) is created for just the cup preparation tasks. Cancelling it leaves unrelated tasks (like boiling water) untouched.
- After cancellation, a new group CTS (`cupGroup2`) is created for the restart, demonstrating clean scoping.

### 5. Safe Await of Possibly Cancelled Tasks
- `TaskHelper.SafeAwait` awaits tasks swallowing `OperationCanceledException` so aggregate `Task.WhenAll` handling can proceed without noisy exception rethrows during cleanup phases.

### 6. Parallel Composition
- `Task.WhenAll` gathers several independent tasks (e.g., gathering milk/tea/sugar, or teabag + sugar additions). This shows a fan-out + join pattern.

### 7. Cooperative Sync Loop Simulation
- `BusyWait` simulates CPU-bound work segments rather than using `Thread.Sleep`, allowing cancellation checks in the caller loop.

### 8. Logging & Thread Safety
- All console writes (and temporary color changes) are wrapped in a lock to ensure atomic colored lines even with concurrent tasks.
- Log entries prefix elapsed time since scenario start for temporal ordering insights.

### 9. Resilience & Partial Restart
- The workflow intentionally cancels a subset, resets minimal state, and continues without starting over globally, modeling real-world recovery patterns.

## Examples Overview
### Example 1: Cancel Synchronous Work
Starts filling the kettle (synchronous loop with periodic cancellation checks). Cancels mid-way to demonstrate cooperative cancellation in CPU-ish code. Verifies resulting state.

### Example 2: Cancel Asynchronous Gather Operations
Concurrently gathers cup, spoon, milk, tea bags. Cancels after a delay, then reports which items completed before cancellation.

### Example 3: Full Orchestrated Workflow with Drop & Restart
- Begins boiling water and gathering durables.
- Prepares cup prerequisites; gates teabag/sugar steps behind a TCS.
- Simulates dropping the cup, cancels only cup-related tasks.
- Resets ephemeral state, restarts cup preparation while reusing durable items.
- Waits for hot water readiness, pours, stirs, adds milk, stirs again.
- Prints final consolidated state.

## Build & Run (Windows / .NET SDK)
Prerequisite: Install the .NET 9 SDK (or latest supporting `net9.0`). Verify:

```cmd
dotnet --version
```

From the repository root:

```cmd
REM (Optional) Restore explicitly
dotnet restore TCSCTS.sln

REM Build (Debug by default)
dotnet build TCSCTS.sln -c Debug

REM Run the console app
dotnet run --project TCSCTS/TCSCTS.csproj -c Debug
```

You should see colored, timestamped logs for each example separated by lines of dashes.

## Adjusting Step Latency
Currently latency per step is defined in `Program.cs`:
```csharp
private static readonly TimeSpan _stepLatency = TimeSpan.FromMilliseconds(200);
```
Change this value and rebuild to speed up or slow down the demo. Making it larger exaggerates ordering and cancellation windows.

Possible enhancement (not implemented): accept a command-line argument and parse to override `_stepLatency`.

## Key Patterns to Notice in Code
- `CancellationTokenSource.CreateLinkedTokenSource(master.Token)` to combine scopes
- `token.ThrowIfCancellationRequested()` inside loops and before starting dependent steps
- Minimal selective resetting (`ResetAfterDrop`) instead of clearing all state
- Use of `TaskCompletionSource.TrySetResult/TrySetCanceled/TrySetException` for robust signaling
- Defensive validation before actions (`InvalidOperationException` guard clauses)

## Common Pitfalls Avoided
- Inline continuations on TCS completion (mitigated via `RunContinuationsAsynchronously`)
- Forgetting cancellation checks in CPU-bound loops
- Cancelling the entire workflow instead of a logical component
- Race conditions on console output (solved with a lock)

## Extending the Demo (Ideas)
- Add command-line parameters for latency & which example(s) to run.
- Include a fourth example using channels (`System.Threading.Channels`) for producer/consumer (e.g., multiple kettles).
- Integrate `IAsyncEnumerable` for streaming progress updates.
- Add structured logging (e.g., JSON) for machine parsing.
- Introduce retry logic for transient steps (simulate flaky gather).

## Troubleshooting
- If nothing logs with color, ensure your terminal supports ANSI / Windows Console coloring (most modern terminals do).
- If cancellation appears too quick or too slow, tweak `_stepLatency`.
- Exceptions like "Milk must be gathered before preparing the cup" indicate a sequencing bug if you modify orchestration logic.

## License / Usage
This code is intended as an educational sample for asynchronous coordination patterns. Use or modify freely.

---
Happy brewing & exploring advanced async patterns!

