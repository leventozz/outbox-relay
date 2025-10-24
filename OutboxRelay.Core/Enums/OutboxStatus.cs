namespace OutboxRelay.Core.Enums
{
    public enum OutboxStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
        Processing = 3
    }
}
