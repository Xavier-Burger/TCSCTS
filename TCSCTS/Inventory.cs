namespace TCSCTS;

public class Inventory(DateTime startTime, TimeSpan stepLatency)
{
    // Durable items (persist across dropping the cup)
    public bool MilkReady { get; set; }
    public bool TeaBagsReady { get; set; }
    public bool SugarJarReady { get; set; }


    // Synchronous, CPU-ish work: cooperative cancellation by checking the token.

    // Durable supplies. Gather once; they persist across a dropped cup.
    public async Task EnsureDurablesAsync(CancellationToken token)
    {
        // Gather milk, tea bags, and sugar in parallel.
        var tasks = new Task[]
        {
            MilkReady ? Task.CompletedTask : GatherMilkAsync(token),
            TeaBagsReady ? Task.CompletedTask : GatherTeaBagsAsync(token),
            SugarJarReady
                ? Task.CompletedTask
                : GatherSugarJarAsync(token),
        };

        await Task.WhenAll(tasks);
        ConsoleHelper.Inventory(startTime, "Durables on bench (milk/tea/sugar).");
    }

    public async Task GatherMilkAsync(CancellationToken token)
    {
        ConsoleHelper.Inventory(startTime, "Gathering milk (async)");
        await Task.Delay(stepLatency, token);
        MilkReady = true;
        ConsoleHelper.Inventory(startTime, "Milk gathered.");
    }

    public async Task GatherTeaBagsAsync(CancellationToken token)
    {
        ConsoleHelper.Inventory(startTime, "Gathering tea bags (async)");
        await Task.Delay(stepLatency, token);
        TeaBagsReady = true;
        ConsoleHelper.Inventory(startTime, "Tea bags gathered.");
    }

    public async Task GatherSugarJarAsync(CancellationToken token)
    {
        ConsoleHelper.Inventory(startTime, "Gathering sugar jar (async)");
        await Task.Delay(stepLatency, token);
        SugarJarReady = true;
        ConsoleHelper.Inventory(startTime, "Sugar jar gathered.");
    }
}