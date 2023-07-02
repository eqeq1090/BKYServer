using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Model.Entity;

namespace BKWebAPIComponent.Common.ResultClass
{
    public enum QueryResultType
    {
        RowCount,
        NoResult,
        Single,
        Multiple,
        ReturnID
    }

    public abstract class IQueryResult
    {
        protected ServiceErrorCode m_ServiceErrorCode;
        public QueryResultType QueryResultType { get; protected set; }

        public bool IsSuccess()
        {
            return m_ServiceErrorCode == ServiceErrorCode.SUCCESS;
        }

        public ServiceErrorCode GetServiceErrorCode()
        {
            return m_ServiceErrorCode;
        }

        public virtual U? GetResult<U>()
            where U : class
        {
            throw new NotImplementedException();
        }
    }

    public class QueryResultRowCount : IQueryResult
    {
        private int m_AffectedRowCount;

        public QueryResultRowCount(ServiceErrorCode errorCode, int affectedRowCount)
        {
            QueryResultType = QueryResultType.RowCount;
            m_ServiceErrorCode = errorCode;
            m_AffectedRowCount = affectedRowCount;
        }

        public QueryResultRowCount(ServiceErrorCode errorCode)
        {
            m_ServiceErrorCode = errorCode;
        }

        public int GetAffectedRowCount()
        {
            return m_AffectedRowCount;
        }
    }

    public class QueryResultNoResult : IQueryResult
    {
        public QueryResultNoResult(ServiceErrorCode errorCode)
        {
            QueryResultType = QueryResultType.NoResult;
            m_ServiceErrorCode = errorCode;
        }
    }

    public class QueryResultID : IQueryResult
    {
        private long m_ReturnID;
        public QueryResultID(ServiceErrorCode errorCode, long returnID)
        {
            QueryResultType = QueryResultType.ReturnID;
            m_ReturnID = returnID;
            m_ServiceErrorCode = errorCode;
        }

        public long GetNewID()
        {
            return m_ReturnID;
        }
    }

    public class QueryResultSingle<T> : IQueryResult
        where T : class, new()
    {
        private T? Result;

        public QueryResultSingle(ServiceErrorCode errorCode, T result)
        {
            QueryResultType = QueryResultType.Single;
            Result = result;
            m_ServiceErrorCode = errorCode;
        }

        public QueryResultSingle(ServiceErrorCode errorCode)
        {
            m_ServiceErrorCode = errorCode;
        }

        public override U? GetResult<U>()
            where U : class
        {
            if (Result is not U)
            {
                return null;
            }
            return Result as U;
        }
    }

    public class QueryResultMultiple<T> : IQueryResult
        where T : class
    {
        private List<T>? Result;

        public QueryResultMultiple(ServiceErrorCode errorCode, List<T> result)
        {
            QueryResultType = QueryResultType.Multiple;
            Result = result;
            m_ServiceErrorCode = errorCode;
        }

        public QueryResultMultiple(ServiceErrorCode errorCode)
        {
            m_ServiceErrorCode = errorCode;
        }

        public override U? GetResult<U>()
            where U : class
        {
            if (Result is not U)
            {
                return null;
            }
            return Result as U;
        }
    }
}
