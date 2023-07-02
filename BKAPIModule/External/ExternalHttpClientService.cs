using RestSharp;
using System.Data;

namespace BKWebAPIComponent.External
{
    public class ExternalHttpClientService
    {
        public async Task<U> Post<T,U>(string url,
            Dictionary <string, string> headers,
            T reqBodyClass)
            where T : class
            where U : class
        {
            // 클라이언트 작성
            var client = new RestClient(url);

            // 쿼리 파라미터 작성
            var request = new RestRequest();
            foreach (var item in headers)
            {
                request.AddHeader(item.Key, item.Value);
            }
            request.AddJsonBody<T>(reqBodyClass);

            try
            {
                var result = await client.PostAsync<U>(request);
                if (result != null)
                {
                    return result;
                }
                else
                {
                    throw new Exception("ExternalHttpClientService post failed");
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<U> Get<T, U>(string url,
            Dictionary<string, string> headers,
            Dictionary<string, string> queryParams)
            where T : class
            where U : class
        {
            // 클라이언트 작성
            var client = new RestClient(url);

            // 쿼리 파라미터 작성
            var request = new RestRequest();
            foreach (var item in headers)
            {
                request.AddHeader(item.Key, item.Value);
            }
            foreach (var item in queryParams)
            {
                request.AddQueryParameter(item.Key, item.Value);
            }

            try
            {
                var result = await client.GetAsync<U>(request);
                if (result != null)
                {
                    return result;
                }
                else
                {
                    throw new Exception("ExternalHttpClientService get failed");
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
