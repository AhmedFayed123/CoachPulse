namespace CoachPulse.Api.Infrastructure;

public sealed class DatabaseStartupState
{
    private int _initialized;
    private int _databaseReachable;
    private string? _failure;

    public bool IsInitialized => Volatile.Read(ref _initialized) == 1;
    public bool DatabaseReachable => Volatile.Read(ref _databaseReachable) == 1;
    public string? Failure => Volatile.Read(ref _failure);

    public void MarkInitialized()
    {
        Volatile.Write(ref _failure, null);
        Volatile.Write(ref _initialized, 1);
    }

    public void MarkFailure(string reason)
    {
        Volatile.Write(ref _failure, reason);
        Volatile.Write(ref _initialized, 0);
    }

    public void MarkDatabaseUnavailable(string reason)
    {
        Volatile.Write(ref _databaseReachable, 0);
        MarkFailure(reason);
    }

    public void MarkDatabaseReachable() => Volatile.Write(ref _databaseReachable, 1);
}
