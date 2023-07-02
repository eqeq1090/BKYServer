using BKServerBase.Logger;
using BKProtocol;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.ConstEnum;

namespace BKWebAPIComponent.Common
{
    public static class ErrorResponse<T>
        where T : IAPIResMsg, new()
    {
        public static T Make(ServiceErrorCode serviceErrorCode)
        {
            var res = new T();
            res.errorCode = ErrorCodeConvert.Instance.GetMsgErrorCode(res, serviceErrorCode);
            return res;
        }

        public static T Make(MsgErrorCode errorCode)
        {
            var res = new T();

            ContentsLog.Critical.LogError($"<== msg : {res.msgType.ToString()}'s Error [{errorCode}] responsed");

            res.errorCode = errorCode;
            return res;
        }
    }
}
