namespace BKServerBase.Threading
{
    public delegate void Command();

    public interface ICommandExecutor
    {
        bool IsEmpty();
        long Length();
        void
        Close();
        void Execute();
        bool Invoke(Command command);
    }

    public struct QueueCommand
    {
        public QueueCommand(Command command)
        {
            CommandDelegate = command;
        }
        public void Execute()
        {
            CommandDelegate();
        }
        Command CommandDelegate;
    }
}
