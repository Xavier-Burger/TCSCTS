namespace TCSCTS;

public class Program
{
    // Simulated per-step latency so you can see the ordering and cancellation.
    private static readonly TimeSpan _stepLatency =
        TimeSpan.FromMilliseconds(200);

    public static async Task Main()
    {
        Console.WriteLine("=== Cancellation + TCS: Tea examples ===");
        Console.WriteLine();

        await Examples.Example1_CancelSynchronousWork(_stepLatency);
        Console.WriteLine(new string('-', 72));

        await Examples.Example2_CancelAsynchronousWork(_stepLatency);
        Console.WriteLine(new string('-', 72));

        await Examples.Example3_FullWorkflowWithDropAndRestart(_stepLatency);
        Console.WriteLine(new string('-', 72));

        Console.WriteLine("All examples finished.");
    }
}




