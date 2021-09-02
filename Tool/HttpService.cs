using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WeBook.Tool
{
    public class HttpService : IHttpService
    {
        private readonly IHttpClientFactory _clientFactory;

        const string baseUrl = "https://m.weibo.cn/api/container/getIndex";
        public HttpService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<HttpResponseMessage> HttpRequestAsync(string url, HttpMethod method, HttpContent content = null, List<KeyValuePair<string, string>> headers = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (content != null)
            {
                request.Content = content;
            }
            if (headers != null)
            {
                foreach (var keyValue in headers)
                {
                    request.Headers.Add(keyValue.Key, keyValue.Value);
                }
            }
            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl);
            return await client.SendAsync(request);
        }
    }
}
