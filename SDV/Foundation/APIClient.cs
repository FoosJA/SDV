using Newtonsoft.Json;
using SDV.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SDV.Foundation
{
    class APIClient
    {
        //private static string serverName= "app-web-test.odusv.so";
        private static string ServerName;// = "sv-app-web-wsfc.odusv.so";
        public static JsonSerializer serializer = JsonSerializer.CreateDefault();
        /// <summary>
        /// Запись измерений
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <param name="serverName"></param>
        /// <param name="oi"></param>
        /// <returns></returns>
        public static bool ToWrite(TokenResponse tokenResponse, string serverName, OIck11 oi)
        {
            ServerName = serverName;
            try
            {
                WriteValuesWithClient(tokenResponse, MeasurementValueType.Numeric, oi.MeasValueList, oi.UidMeas);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Создание запроса API на запись значений
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <param name="type"></param>
        /// <param name="oiList"></param>
        /// <param name="uidOi"></param>
        private static async void WriteValuesWithClient(TokenResponse tokenResponse, MeasurementValueType type, IEnumerable<MeasValue> oiList, Guid uidOi)
        {
            var httpHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = true,
            };
            var httpClient = new HttpClient(httpHandler);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", tokenResponse.AccessToken);
            Client ck11Cli = new Client(httpClient) { ReadResponseAsString = true, BaseUrl = $"https://{ServerName}/api/public/measurement-values/v2.0" };
            Body4 body = new Body4();

            foreach (var meas in oiList)
            {
                /* MeasurementValueWriteModel writeMeas = new MeasurementValueWriteModel
                 {
                     DateTime = "2022-01-24T14:15:22Z",
                     DateTime2 = "2022-01-24T14:15:22Z",
                     QualityCodes = 268435458,
                     Uid = uidOi,
                     Value = 99
                 };*/
                string timeForApi = new DateTimeOffset(meas.Date).ToString("u", System.Globalization.CultureInfo.InvariantCulture);
                MeasurementValueWriteModel writeMeas = new MeasurementValueWriteModel
                {
                    DateTime = timeForApi,
                    DateTime2 = timeForApi,
                    QualityCodes = meas.QualityCode,
                    Uid = uidOi,
                    Value = meas.Value
                };
                body.Values.Add(writeMeas);
            }
            var result = ck11Cli.WriteAsync(type, body).Result;
        }

        #region Token
        public class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("user_login")]
            public string UserLogin { get; set; }
            [JsonProperty("token_type")]
            public string TokenType { get; set; }
            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }
        }
        public static async Task<TokenResponse> GetToken(string serverName)
        {
            //try
            //{
            var httpHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = true,
            };
            var httpClient = new HttpClient(httpHandler);
            var response = httpClient.PostAsync(
            $"https://{serverName}:9443/auth/nego/token",
            //$"https://{serverName}/auth/nego/token",
            new StringContent("{}", Encoding.UTF8, "application/json")
            ).Result;
            var responseContent = await response.Content.ReadAsStringAsync();
            return serializer.Deserialize<TokenResponse>(new JsonTextReader(new StringReader(responseContent)));
            //}
            //catch (Exception ex)
            //{
            //    return null;
            //}
        }
        #endregion
    }
}
