using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace ST_Test01.Services
{
    public interface IHttpProxyService
    {
        Task<Tuple<string, byte[]>> GetBytesAsync(Uri uri,
                                                  CancellationToken cancellationToken);
    }
    public class HttpProxyService: IHttpProxyService
    {
        private const uint HRESULT_WININET_E_NAME_NOT_RESOLVED = 0x80072EE7;
        private const uint HRESULT_WININET_CONNECTION_INTERRUPTION = 0x80072EFD;
        public async Task<Tuple<string, byte[]>> GetBytesAsync(Uri uri,
                                                               CancellationToken cancellationToken)
        {
            Tuple<string, byte[]> contentTypeAndBytes = null;

            try
            {

                using (var httpClient = new HttpClient())
                {

                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        using (var response = await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead).AsTask(cancellationToken))
                        {
                            switch (response.StatusCode)
                            {
                                case HttpStatusCode.Ok:
                                    var buffer = await response.Content.ReadAsBufferAsync();
                                    var dataReader = DataReader.FromBuffer(buffer);
                                    var bytes = new byte[buffer.Length];
                                    dataReader.ReadBytes(bytes);
                                    contentTypeAndBytes = Tuple.Create<string, byte[]>(response.Content.Headers.ContentType.MediaType, bytes);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == HRESULT_WININET_E_NAME_NOT_RESOLVED ||
                    (uint)ex.HResult == HRESULT_WININET_CONNECTION_INTERRUPTION)
                {
                    // this exception indicates "Network off-line state"
                    throw new Exception(ex.Message, ex);
                }
                else
                {
                    // Rethrowing all
                    // unexpected exceptions to get crash reports
                    throw;
                }
            }
            return contentTypeAndBytes;
        }
    }
}
