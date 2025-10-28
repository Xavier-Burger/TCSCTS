namespace TCSCTS;
public class Workspace(DateTime startTime, TimeSpan stepLatency)
{
    // Ephemeral items (lost if we drop the cup)
    public bool HasCup { get; set; }
    public bool HasSpoon { get; set; }
    public bool TeaBagInCup { get; set; }
    public bool SugarInCup { get; set; }
    public bool HotWaterInCup { get; set; }
    public bool MilkPoured { get; set; }
    public int Stirs { get; set; }
    
    public void ResetAfterDrop()
    {
        Workspace bench = this;
        
        bench.HasCup = false;
        bench.HasSpoon = false;
        bench.TeaBagInCup = false;
        bench.SugarInCup = false;
        bench.HotWaterInCup = false;
        // Keep Stirs and MilkPoured off until we actually do them.
        bench.MilkPoured = false;
    }
    
    
    public async Task GatherCupAsync(
        CancellationToken token
    )
    {
        ConsoleHelper.Workspace(startTime, "Gathering cup (async)");
        await Task.Delay(stepLatency, token);
        HasCup = true;
        ConsoleHelper.Workspace(startTime, "Cup gathered.");
    }

    public async Task GatherSpoonAsync(
        CancellationToken token
    )
    {
        ConsoleHelper.Workspace(startTime, "Gathering spoon (async)");
        await Task.Delay(stepLatency, token);
        HasSpoon = true;
        ConsoleHelper.Workspace(startTime, "Spoon gathered.");
    }

    // PrepareCupGroupAsync:
    // - In parallel: gather cup and spoon. Milk must already be present
    //   (EnsureDurablesAsync). We gate teabag/sugar with cupPrereqsReady TCS.
    public async Task PrepareCupGroupAsync(
        Inventory inv,
        TaskCompletionSource<bool> cupPrereqsReady,
        CancellationToken token
    )
    {
        ConsoleHelper.Workspace(startTime, "Preparing cup (async)");
        // Gather cup + spoon in parallel
        var gCup = HasCup ? Task.CompletedTask : GatherCupAsync(token);
        var gSpoon = HasSpoon ? Task.CompletedTask : GatherSpoonAsync(token);

        await Task.WhenAll(gCup, gSpoon);

        // Verify milk is present before allowing teabag/sugar phase to start
        if (!inv.MilkReady)
        {
            throw new InvalidOperationException(
                "Milk must be gathered before preparing the cup."
            );
        }

        // Signal that prerequisites (cup, spoon, milk) are ready.
        cupPrereqsReady.TrySetResult(true);

        // Note: placing teabag and scooping sugar are started by caller
        // and can happen in any order after this gate.
    }

    public async Task PlaceTeaBagIntoCupAsync(
        Inventory inv,
        CancellationToken token
    )
    {
        // Must be after cup + milk + spoon (ensured by prereq gate)
        token.ThrowIfCancellationRequested();

        if (!HasCup || !HasSpoon || !inv.MilkReady)
        {
            throw new InvalidOperationException(
                "Prerequisites for placing tea bag are not satisfied."
            );
        }

        ConsoleHelper.Workspace(startTime, "Placing tea bag into cup (async)");
        await Task.Delay(stepLatency, token);
        TeaBagInCup = true;
        ConsoleHelper.Workspace(startTime, "Tea bag placed.");
    }

    public async Task ScoopSugarIntoCupAsync(
        Inventory inv,
        CancellationToken token
    )
    {
        // Must be after cup + milk + spoon (ensured by prereq gate)
        token.ThrowIfCancellationRequested();

        if (!HasCup || !HasSpoon || !inv.MilkReady)
        {
            throw new InvalidOperationException(
                "Prerequisites for scooping sugar are not satisfied."
            );
        }

        ConsoleHelper.Workspace(startTime, "Scooping sugar into cup (async)");
        await Task.Delay(stepLatency, token);
        SugarInCup = true;
        ConsoleHelper.Workspace(startTime, "Sugar added.");
    }

    public async Task PourHotWaterAsync(
        KitchenState state,
        Workspace bench,
        CancellationToken token
    )
    {
        if (!state.Kettle.Boiled)
        {
            throw new InvalidOperationException(
                "Hot water must be ready before pouring."
            );
        }
        if (!bench.HasCup)
        {
            throw new InvalidOperationException(
                "Cup must be ready before pouring."
            );
        }

        token.ThrowIfCancellationRequested();

        ConsoleHelper.Workspace(startTime, "Pouring hot water (async)");
        await Task.Delay(stepLatency, token);
        bench.HotWaterInCup = true;
        ConsoleHelper.Workspace(startTime, "Hot water poured.");
    }

    public async Task StirAsync(
        Workspace bench,
        string label,
        CancellationToken token
    )
    {
        if (!bench.HotWaterInCup)
        {
            throw new InvalidOperationException(
                "Cannot stir before hot water is in the cup."
            );
        }

        token.ThrowIfCancellationRequested();

        ConsoleHelper.Workspace(startTime, $"Stirring ({label}) (async)");
        await Task.Delay(stepLatency, token);
        bench.Stirs++;
        ConsoleHelper.Workspace(startTime, $"Stirred ({label}).");
    }

    public async Task PourDashOfMilkAsync(
        Inventory inv,
        Workspace bench,
        CancellationToken token
    )
    {
        if (!bench.HotWaterInCup)
        {
            throw new InvalidOperationException(
                "Cannot add milk before hot water."
            );
        }

        token.ThrowIfCancellationRequested();
        if (!inv.MilkReady)
        {
            throw new InvalidOperationException("Milk not available.");
        }

        ConsoleHelper.Workspace(startTime, "Pouring a dash of milk (async)");
        await Task.Delay(stepLatency, token);
        bench.MilkPoured = true;
        ConsoleHelper.Workspace(startTime, "Milk added.");
    }
}
