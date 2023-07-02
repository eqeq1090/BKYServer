namespace BKServerBase.Messaging.Detail
{
    public interface IJobMessage
    {
        bool IsAwait { get; }
        string Tag { get; }

        void Execute();
    }
}
