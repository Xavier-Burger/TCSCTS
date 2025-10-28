namespace TCSCTS;

public class TaskHelper
{
    public static async Task SafeAwait(Task t)
    {
        try { await t; }
        catch (OperationCanceledException) { /* ignore for demo */ }
    }

    public static void BusyWait(int milliseconds)
    {
        var start = Environment.TickCount;
        while (Environment.TickCount - start < milliseconds)
        {
            // pretend to do CPU-bound work; check cancellation in caller
        }
    }
}