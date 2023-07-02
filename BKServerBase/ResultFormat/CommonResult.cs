using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKProtocol;

namespace BKServerBase.ResultFormat
{
    public class CommonResult<T>
    {
        private T? m_Result;

        public static CommonResult<T> Success(T result)
        {
            return new CommonResult<T>(MsgErrorCode.Success, result);
        }

        public static CommonResult<T> Fail(MsgErrorCode errorCode)
        {
            return new CommonResult<T>(errorCode, default);
        }
        private CommonResult(MsgErrorCode errorCode, T? result)
        {
            ErrorCode = errorCode;
            m_Result = result;
        }

        public void Deconstruct(out MsgErrorCode errorCode, out T result)
        {
            errorCode = ErrorCode;
            result = Result;
        }

        public MsgErrorCode ErrorCode { get; }
        public T Result => m_Result!;
    }
}
