using MySqlConnector;
using System.Data;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.Model.Common;
using BKWebAPIComponent.Model.Entity;
using static BKWebAPIComponent.Mapper.DBQueryMapper;

namespace BKWebAPIComponent.Common.DBSession
{
    public class DBTransaction : AbstractDBSession
    {
        private readonly DBQueryMapper m_QueryMapper;
        private readonly MySqlConnection m_Connection;
        private readonly AtomicFlag m_Disposed = new AtomicFlag(false);
        private MySqlTransaction m_Transaction = null!;
        private TransactionStatus m_TransactionStatus;

        public DBTransaction(string connectionString, DBQueryMapper mapper)
        {
            m_Connection = new MySqlConnection(connectionString);
            m_QueryMapper = mapper;
        }

        ~DBTransaction()
        {
            Dispose(false);
        }

        public async Task OpenAsync(IsolationLevel level)
        {
            await m_Connection.OpenAsync();
            m_Transaction = m_Connection.BeginTransaction(level);
            m_TransactionStatus = TransactionStatus.NotHandled;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Commit()
        {
            if (m_TransactionStatus != TransactionStatus.NotHandled)
            {
                CoreLog.Critical.LogError($"Commit failed, transactionStatus: {m_TransactionStatus}");
                return;
            }
            m_Transaction.Commit();
            m_TransactionStatus = TransactionStatus.Commited;
        }

        public void Rollback()
        {
            if (m_TransactionStatus != TransactionStatus.NotHandled)
            {
                CoreLog.Critical.LogError($"Rollback failed, transactionStatus: {m_TransactionStatus}");
                return;
            }
            m_Transaction.Rollback();
            m_TransactionStatus = TransactionStatus.Rollbacked;
        }

        private void Dispose(bool disposing)
        {
            if (m_Disposed.IsOn == true || disposing == false)
            {
                return;
            }
            if (m_Disposed.On() == false)
            {
                return;
            }
            
            if (m_TransactionStatus == TransactionStatus.NotHandled)
            {
                m_Transaction.Rollback();
            }
            
            m_Connection.Dispose();
            m_Transaction.Dispose();
        }

        public async Task<ServiceResult<T>> ExecuteQuery<T,U>(QueryDef queryDef, U input)
            where T : class, new()
            where U : class, new()
        {
            if (m_Connection.State != ConnectionState.Open)
            {
                return new ServiceResult<T>(ServiceErrorCode.DB_CONNECTION_NOT_OPEN_STATE);
            }
            if (m_TransactionStatus != TransactionStatus.NotHandled)
            {
                return new ServiceResult<T>(ServiceErrorCode.DB_TRANSACTION_CURRUPTED);
            }
            var queryInfo = m_QueryMapper.GetQueryInfo(queryDef);
            if (queryInfo == null)
            {
                return new ServiceResult<T>(ServiceErrorCode.QUERY_NOT_FOUND);
            }
            var result = await queryInfo.ExecuteDBQuery(m_Connection, input, m_Transaction);
            if (result.IsSuccess() == false)
            {
                return new ServiceResult<T>(result.GetServiceErrorCode());
            }

            switch (result.QueryResultType)
            {
                case QueryResultType.NoResult:
                case QueryResultType.RowCount:
                    {
                        var convertedResult = (result as QueryResultRowCount)!;
                        var ret = new VoidEntity();
                        if (ret is not T)
                        {
                            return new ServiceResult<T>(ServiceErrorCode.SERVICE_RESULT_TYPE_INVALID);
                        }
                        return new ServiceResult<T>(ServiceErrorCode.SUCCESS, (ret as T)!);
                    }
                case QueryResultType.ReturnID:
                    {
                        var convertedResult = (result as QueryResultID)!;
                        var ret = new IDEntity();
                        if (ret is not T)
                        {
                            return new ServiceResult<T>(ServiceErrorCode.SERVICE_RESULT_TYPE_INVALID);
                        }
                        ret.ID = convertedResult.GetNewID();
                        return new ServiceResult<T>(ServiceErrorCode.SUCCESS, (ret as T)!);
                    }
                case QueryResultType.Multiple:
                    {
                        var internalResult = result.GetResult<T>();
                        if (internalResult == null)
                        {
                            return new ServiceResult<T>(ServiceErrorCode.SERVICE_RESULT_TYPE_INVALID);
                        }
                        return new ServiceResult<T>(ServiceErrorCode.SUCCESS, (internalResult as T)!);
                    }
                case QueryResultType.Single:
                    {
                        var internalResult = result.GetResult<T>();
                        if (internalResult == null)
                        {
                            return new ServiceResult<T>(ServiceErrorCode.SERVICE_RESULT_TYPE_INVALID);
                        }
                        return new ServiceResult<T>(ServiceErrorCode.SUCCESS, internalResult);
                    }
                default:
                    {
                        return new ServiceResult<T>(ServiceErrorCode.QUERY_RESULT_TYPE_INVALID);
                    }
            }
        }
    }
}
