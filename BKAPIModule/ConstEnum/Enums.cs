namespace BKWebAPIComponent.ConstEnum
{
    internal enum TransactionStatus
    {
        NotHandled,
        Commited,
        Rollbacked
    }

    internal enum QueryType
    {
        SelectOne,
        SelectMany,
        Insert,
        Update,
        Delete,
        InsertAndReturnID,
        InsertMultiple
    }

    internal enum JobExecutorState
    {
        NotHandled,
        Started,
        QueryExecuted,
        Committed,
        Rollbacked
    }
}
