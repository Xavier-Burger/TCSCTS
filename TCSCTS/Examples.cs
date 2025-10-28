namespace TCSCTS;

public static class Examples
{
    // Example 1: Cancel synchronous work via cooperative checks
    public static async Task Example1_CancelSynchronousWork(TimeSpan stepLatency)
    {
        DateTime startTime = DateTime.Now;
        ConsoleHelper.Title("Example 1: Cancel synchronous work (Fill kettle)");

        var kitchen = new KitchenState(startTime, stepLatency);
        var cts = new CancellationTokenSource();

        // Kick off the synchronous work on a background thread so we can cancel.
        var fillTask = Task.Run(() => {
            try
            {
                kitchen.FillKettleSync(cts.Token);
                ConsoleHelper.Example(startTime, "Fill kettle completed.");
            }
            catch (OperationCanceledException)
            {
                ConsoleHelper.Error(startTime, "Fill kettle cancelled.");
            }
        });

        // Cancel it after a short moment.
        await Task.Delay(stepLatency * 2);
        
        ConsoleHelper.Error(startTime, "Oops. Power outage. Cancelling fill kettle work");

        await cts.CancelAsync();

        await fillTask;
        ConsoleHelper.Example(startTime, $"Kettle filled: {kitchen.Kettle.Filled}");
        Console.WriteLine();
    }

    // Example 2: Cancel asynchronous gather operations
    public static async Task Example2_CancelAsynchronousWork(TimeSpan stepLatency)
    {
        DateTime startTime = DateTime.Now;
        ConsoleHelper.Title("Example 2: Cancel asynchronous gathers (cup, spoon, milk, tea)");

        var inv = new Inventory(startTime, stepLatency);
        var bench = new Workspace(startTime, stepLatency);
        var cts = new CancellationTokenSource();

        var gatherTasks = Task.WhenAll(
            bench.GatherCupAsync(cts.Token),
            bench.GatherSpoonAsync(cts.Token),
            inv.GatherMilkAsync(cts.Token),
            inv.GatherTeaBagsAsync(cts.Token)
        );
        

        // Simulate cutting the work short (e.g., someone calls you away).
        await Task.Delay(stepLatency * 3);
        
        ConsoleHelper.Error(startTime, "Oops. Called away. Cancelling any remaining gather work");
        
        await cts.CancelAsync();

        try
        {
            await gatherTasks;
            ConsoleHelper.Example(startTime, "All gathers complete.");
        }
        catch (OperationCanceledException)
        {
            ConsoleHelper.Error(startTime, "Gathering cancelled.");
        }

        ConsoleHelper.Example(startTime, 
            $"State => Cup:{bench.HasCup}, Spoon:{bench.HasSpoon}, "
            + $"Milk:{inv.MilkReady}, TeaBags:{inv.TeaBagsReady}");

        Console.WriteLine();
    }

    // Example 3: Full workflow, with TaskCompletionSource gating dependencies,
    // group-scoped cancellation (drop the cup), and selective re-execution.
    public static async Task Example3_FullWorkflowWithDropAndRestart(TimeSpan stepLatency)
    {
        DateTime startTime = DateTime.Now;
        ConsoleHelper.Title("Example 3: Orchestrated tea w/ TCS + group cancellation + restart");

        var inv = new Inventory(startTime, stepLatency); // durable stuff (milk/sugar/tea bags)
        var bench = new Workspace(startTime, stepLatency); // the cup and what’s in it
        var kitchen = new KitchenState(startTime, stepLatency); // kettle

        // Master CTS for overall workflow if you wanted a global cancel
        using var master = new CancellationTokenSource();

        // 1) Start preparing hot water. TCS signals when water is ready.
        var hotWaterReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var hotWaterTask = kitchen.PrepareHotWaterAsync(hotWaterReady, master.Token);

        // 2) Prepare the "cup group" concurrently.
        // First make sure durable items exist on the side (milk, tea, sugar).
        await inv.EnsureDurablesAsync(master.Token);

        // Gate for "cup prerequisites are ready" so sugar/teabag can start in
        // any order after cup + milk + spoon are ready.
        var cupPrereqsReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Have a group CTS for the cup workflow so we can cancel just that set.
        using var cupGroup = CancellationTokenSource.CreateLinkedTokenSource(master.Token);

        var cupPrepTask = bench.PrepareCupGroupAsync(inv, cupPrereqsReady, cupGroup.Token);

        // Wait until cup group reaches "ready to add teabag/sugar"
        await cupPrereqsReady.Task;

        // Kick off teabag/sugar steps that can happen in any order.
        var addTeabagTask = bench.PlaceTeaBagIntoCupAsync(inv, cupGroup.Token);
        var addSugarTask = bench.ScoopSugarIntoCupAsync(inv, cupGroup.Token);

        // Simulate an error before pouring: drop the cup after a moment.
        await Task.Delay(stepLatency * 3);
        
        ConsoleHelper.Error(startTime, "Oops. Dropped the cup. Cancelling cup preparation");

        cupGroup.Cancel();

        // Any in-flight cup-related tasks will cancel. Handle their completion.
        await Task.WhenAll(
            TaskHelper.SafeAwait(cupPrepTask),
            TaskHelper.SafeAwait(addTeabagTask),
            TaskHelper.SafeAwait(addSugarTask)
        );

        // Reset only what’s appropriate after a drop:
        // - Cup and spoon: lost; need to re-gather.
        // - Contents in cup (tea bag, sugar): lost.
        // - Milk/TeaBags/Sugar jars: still gathered (durable).
        bench.ResetAfterDrop();

        ConsoleHelper.Example(startTime, "Restarting cup preparation (reusing milk/sugar/tea jars).");

        // Recreate a new group CTS and new prereq gate.
        using var cupGroup2 = CancellationTokenSource.CreateLinkedTokenSource(master.Token);

        cupPrereqsReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var cupPrepTask2 = bench.PrepareCupGroupAsync(inv, cupPrereqsReady, cupGroup2.Token);

        await cupPrereqsReady.Task;

        // Now do teabag and sugar again (order doesn't matter).
        var addTeabagTask2 = bench.PlaceTeaBagIntoCupAsync(inv, cupGroup2.Token);

        var addSugarTask2 = bench.ScoopSugarIntoCupAsync(inv, cupGroup2.Token);

        await Task.WhenAll(addTeabagTask2, addSugarTask2);
        ConsoleHelper.Example(startTime, "Cup prepared with tea bag and sugar.");

        // 3) Pour hot water (must be after hot water ready + cup prepared)
        await hotWaterReady.Task;
        ConsoleHelper.Example(startTime, "Hot water ready. Pouring into cup");
        await bench.PourHotWaterAsync(kitchen, bench, master.Token);

        // 4) Stir, milk, stir again
        await bench.StirAsync(bench, "first stir", master.Token);
        await bench.PourDashOfMilkAsync(inv, bench, master.Token);
        await bench.StirAsync(bench, "second stir", master.Token);

        ConsoleHelper.Success(startTime, "Done.");

        ConsoleHelper.Example(startTime, $"Final state => WaterBoiled:{kitchen.Kettle.Boiled}, "
            + $"Cup:{bench.HasCup}, Spoon:{bench.HasSpoon}, "
            + $"TeaBagInCup:{bench.TeaBagInCup}, SugarInCup:{bench.SugarInCup}, "
            + $"MilkPoured:{bench.MilkPoured}, Stirs:{bench.Stirs}");

        Console.WriteLine();

        // Ensure the hot water task finishes
        await hotWaterTask;
    }
}