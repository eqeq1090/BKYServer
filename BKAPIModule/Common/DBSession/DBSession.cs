using MySqlConnector;
using System.Data;
using BKServerBase.ConstEnum;
using BKServerBase.Threading;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.Model.Common;
using BKWebAPIComponent.Model.Entity;
using BKWebAPIComponent.Service;
using BKWebAPIComponent.Service.Detail;
using static BKWebAPIComponent.Mapper.DBQueryMapper;

namespace BKWebAPIComponent.Common.DBSession
{
    public class DBSession : AbstractDBSession
    {
        private readonly AtomicFlag m_Disposed = new AtomicFlag(false);
        // private readonly ShardPoolingInfo m_ShardPoolingInfo;
        private MySqlConnection m_Connection;
        private DBQueryMapper m_QueryMapper;

        public DBSession(string connectionString, DBQueryMapper mapper/*, ShardPoolingInfo shardPoolingInfo*/)
        {
            m_Connection = new MySqlConnection(connectionString);
            // m_Connection.Open();
            m_QueryMapper = mapper;
            // m_ShardPoolingInfo = shardPoolingInfo;
        }

        ~DBSession()
        {
            Dispose(false);
        }

        //public void Reuse()
        //{
        //    m_Disposed.Off();
        //}

        //public void Return()
        //{
        //    m_ShardPoolingInfo.Add(this);
        //}

        public Task OpenAsync()
        {
            return m_Connection.OpenAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing == false)
            {
                return;
            }

            if (m_Disposed.On() is false)
            {
                return;
            }

            m_Connection.Dispose();
            // m_Connection.Close();
            // IDBService.Return(this);
        }

        public async Task<ServiceResult<T>> ExecuteQuery<T,U>(QueryDef queryDef, U input)
            where T : class, new()
            where U : class, new()
        {
            if (m_Connection.State != ConnectionState.Open)
            {
                return new ServiceResult<T>(ServiceErrorCode.DB_CONNECTION_NOT_OPEN_STATE);
            }
            var queryInfo = m_QueryMapper.GetQueryInfo(queryDef);
            if (queryInfo == null)
            {
                return new ServiceResult<T>(ServiceErrorCode.QUERY_NOT_FOUND);
            }
            var result = await queryInfo.ExecuteDBQuery(m_Connection, input);
            if (result.IsSuccess() == false)
            {
                return new ServiceResult<T>(result.GetServiceErrorCode());
            }

            switch (result.QueryResultType)
            {
                case QueryResultType.NoResult:
                case QueryResultType.RowCount:
                    {
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

        public ServiceResult<T> ExecuteQuerySync<T,U>(QueryDef queryDef, U input)
            where T : class, new()
            where U : class, new()
        {
            if (m_Connection.State != ConnectionState.Open)
            {
                return new ServiceResult<T>(ServiceErrorCode.DB_CONNECTION_NOT_OPEN_STATE);
            }
            var queryInfo = m_QueryMapper.GetQueryInfo(queryDef);
            if (queryInfo == null)
            {
                return new ServiceResult<T>(ServiceErrorCode.QUERY_NOT_FOUND);
            }
            var result = queryInfo.ExecuteDBQuerySync(m_Connection, input);
            if (result.IsSuccess() == false)
            {
                return new ServiceResult<T>(result.GetServiceErrorCode());
            }

            switch (result.QueryResultType)
            {
                case QueryResultType.NoResult:
                case QueryResultType.RowCount:
                    {
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
