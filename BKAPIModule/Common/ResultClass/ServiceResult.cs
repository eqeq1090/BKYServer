using BKWebAPIComponent.ConstEnum;

namespace BKWebAPIComponent.Common.ResultClass
{
    public class ServiceResult<T>
    {
        private ServiceErrorCode m_ServiceErrorCode;
        private T? Result;

        public ServiceResult(ServiceErrorCode errorCode, T result)
        {
            Result = result;
            m_ServiceErrorCode = errorCode;
        }

        public ServiceResult(T result)
        {
            Result = result;
            m_ServiceErrorCode = ServiceErrorCode.SUCCESS;
        }

        public ServiceResult(ServiceErrorCode errorCode)
        {
            m_ServiceErrorCode = errorCode;
        }

        public bool IsSuccess()
        {
            return m_ServiceErrorCode == ServiceErrorCode.SUCCESS;
        }

        public ServiceErrorCode GetErrorCode()
        {
            return m_ServiceErrorCode;
        }

        public T GetResult()
        {
            if (Result == null)
            {
                throw new Exception("ServiceResult resultValue not Allocated.");
            }
            return Result!;
        }
    }
}
