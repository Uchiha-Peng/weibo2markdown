using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WeBook.Tool
{
    interface IHttpService
    {
        Task<HttpResponseMessage> HttpRequestAsync(string url, HttpMethod method, HttpContent content = null, List<KeyValuePair<string, string>> headers = null);
    }
}
