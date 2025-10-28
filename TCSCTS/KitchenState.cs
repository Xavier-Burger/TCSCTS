namespace TCSCTS;

public class KitchenState(DateTime startTime, TimeSpan stepLatency)
{
    public Kettle Kettle { get; } = new Kettle();

    public async Task PrepareHotWaterAsync(
        TaskCompletionSource<bool> hotWaterReady,
        CancellationToken token
    )
    {
        try
        {
            FillKettleSync(token); // sync piece
            await BoilKettleAsync(token); // async piece
            hotWaterReady.TrySetResult(true);
        }
        catch (OperationCanceledException oce)
        {
            hotWaterReady.TrySetCanceled(oce.CancellationToken);
            ConsoleHelper.Error(startTime, "Hot water preparation cancelled.");
        }
        catch (Exception ex)
        {
            hotWaterReady.TrySetException(ex);
            ConsoleHelper.Error(startTime, $"Hot water preparation failed: {ex.Message}");
        }
    }

    // Synchronous, CPU-ish work: cooperative cancellation by checking the token.
    public void FillKettleSync(CancellationToken token)
    {
        ConsoleHelper.Kettle(startTime, "Filling kettle (sync)");

        int tenthStep = (int)Math.Round(stepLatency.TotalMilliseconds / 10, MidpointRounding.AwayFromZero);

        int totalStepLatency = 3;
        
        for (var i = 0; i < totalStepLatency * 10; i++)
        {
            token.ThrowIfCancellationRequested();
            TaskHelper.BusyWait(tenthStep); // simulate some CPU-bound chunks
        }

        Kettle.Filled = true;
        ConsoleHelper.Kettle(startTime, "Kettle filled.");
    }

    public async Task BoilKettleAsync(CancellationToken token)
    {
        if (!Kettle.Filled)
        {
            throw new InvalidOperationException("Kettle must be filled first.");
        }

        ConsoleHelper.Kettle(startTime, "Boiling kettle (async)");

        for (var i = 0; i < 5; i++)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(stepLatency, token);
        }

        Kettle.Boiled = true;
        ConsoleHelper.Kettle(startTime, "Kettle boiled.");
    }
}