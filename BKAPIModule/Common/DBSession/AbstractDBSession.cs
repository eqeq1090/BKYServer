using MySqlConnector;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.Model.Entity;
using static BKWebAPIComponent.Mapper.DBQueryMapper;

namespace BKWebAPIComponent.Common.DBSession
{
    public interface AbstractDBSession : IDisposable
    {
        public Task<ServiceResult<T>> ExecuteQuery<T,U>(QueryDef queryDef, U input)
            where T : class, new ()
            where U : class, new ();
    }
}
