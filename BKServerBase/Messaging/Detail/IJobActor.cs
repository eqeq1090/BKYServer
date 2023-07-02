namespace BKServerBase.Messaging.Detail
{
    public interface IJobActor<T> : IActor
        where T : class
    {
        T Owner { get; }
    }
}
