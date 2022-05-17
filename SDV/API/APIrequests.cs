using Newtonsoft.Json;
using SDV.Foundation;
using SDV.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SDV.API
{
	public class APIrequests
	{
        private static string ServerName;// = "app-web-test.odusv.so";// = "sv-app-web-wsfc.odusv.so";
        public static JsonSerializer serializer = JsonSerializer.CreateDefault();
        private static string LuaScript = "\nlocal mvs = snapshot.GetObjects('MeasurementValue')\nlocal result = {mvCount = #mvs, mVals = {}}\nfor i=1,#mvs do\n  result.mVals[i] = \n    {\n      name = mvs[i].name, \n      uid = mvs[i].uid, \n      extId = mvs[i].externalId,\n    sourceId=mvs[i].sourceId,\n      POuid=mvs[i].ParentObject.uid,\n      POname = mvs[i].ParentObject.name,\n      MvType = mvs[i].MeasurementValueType.name}\n end\n\n\nout.AddRecord(result)";

        public static List<OIck11> GetMeasAIP(TokenResponse tokenResponse,int modelId, string serverName)
        {
            ServerName = serverName;
            var result = GetObjectsWithClient(tokenResponse, modelId, serverName);
            //var result = GetObjectsWithClient(tokenResponse, serverName);
            List<OIck11> mvList = result.Result.ToMVRT();
            return mvList;
        }
        /// <summary>
        /// Запись измерений
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <param name="serverName"></param>
        /// <param name="oi"></param>
        /// <returns></returns>
        public static void ToWrite(TokenResponse tokenResponse, string serverName, OIck11 oi)
        {
            ServerName = serverName;
            try
            {
                WriteValuesWithClient(tokenResponse, MeasurementValueTypeAPI.Numeric, oi.MeasValueList, oi.UidVal);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        /// <summary>
        /// Создание запроса API на запись значений
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <param name="type"></param>
        /// <param name="oiList"></param>
        /// <param name="uidOi"></param>
        private static async void WriteValuesWithClient(TokenResponse tokenResponse, MeasurementValueTypeAPI type, IEnumerable<MeasValue> oiList, Guid uidOi)
        {
            var httpHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = true,
            };
            var httpClient = new HttpClient(httpHandler);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", tokenResponse.AccessToken);
            SDV.Foundation.Client ck11Cli = new SDV.Foundation.Client(httpClient) { ReadResponseAsString = true, BaseUrl = $"https://{ServerName}/api/public/measurement-values/v2.0" };
            Body4 body = new Body4();

            foreach (var meas in oiList)
            {
                string timeForApi = meas.Date.ToUniversalTime().ToString("s") + "Z";// "2022-02-15T06:00:00Z",
                MeasurementValueWriteModel writeMeas = new MeasurementValueWriteModel
                 {
                     DateTime = timeForApi,
                     DateTime2 = timeForApi,
                     QualityCodes = 268435458,// meas.QualityCode,
                     Uid = uidOi,
                     Value = meas.Value
                };
                body.Values.Add(writeMeas);
            }
            var result = ck11Cli.WriteAsync(type, body).Result;
            if(result.Errors!=null)
			{
                throw new ArgumentException(result.Errors.First().Detail);
			}
        }

        /// <summary>
        /// Получение id актуальной модели
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <param name="serverName"></param>
        /// <returns></returns>
        private static int GetActualModelId(TokenResponse tokenResponse, string serverName)
        {
            var httpHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = true,
            };
            var httpClient = new HttpClient(httpHandler);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", tokenResponse.AccessToken);
            Client ck11Client = new Client(httpClient) { ReadResponseAsString = true, BaseUrl = $"https://{serverName}/api/public/object-models/v2.1" };
            Guid modelUid = new Guid("20000f1d-0000-0000-c000-0000006d746c");//TODO:Scada
            var modelList = ck11Client.GetVersionCollectionAsync(modelUid).Result;            
            var actualVersionId = modelList.Value.First(x => x.State == ModelVersionDescriptorState.Actual).VersionId;            
            return actualVersionId;
        }
        /// <summary>
        /// Чтение объектов в ИМ
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <returns></returns>
        private static async Task<Response6> GetObjectsWithClient(TokenResponse tokenResponse, int modelVersionId, string serverName)
        {
            var httpHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = true,
            };
            var httpClient = new HttpClient(httpHandler);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", tokenResponse.AccessToken);
            Client ck11Cli = new Client(httpClient) { ReadResponseAsString = true, BaseUrl = $"https://{serverName}/api/public/object-models/v2.1" };
            Guid modelUid = new Guid("20000f1d-0000-0000-c000-0000006d746c");//TODO:Scada      
            Body body = new Body();
            body.LuaScript = LuaScript;
            var result = ck11Cli.ExecuteScriptAsync(modelUid, modelVersionId, body).Result;
            return result;

        }
        /// <summary>
        /// Чтение объектов в ИМ
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <returns></returns>
        private static async Task<Response6> GetObjectsWithClient(TokenResponse tokenResponse, string serverName)
        {
            var httpHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = true,
            };
            var httpClient = new HttpClient(httpHandler);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", tokenResponse.AccessToken);
            Client ck11Cli = new Client(httpClient) { ReadResponseAsString = true, BaseUrl = $"https://{serverName}/api/public/object-models/v2.1" };
            Guid modelUid = new Guid("20000f1d-0000-0000-c000-0000006d746c");//TODO:Scada
            var actualVersionId = APIrequests.GetActualModelId(tokenResponse, serverName);           
            Body body = new Body();
            body.LuaScript = LuaScript;// "\nlocal mvs = snapshot.GetObjects('MeasurementValue')\nlocal result = {mvCount = #mvs, mVals = {}}\nfor i=1,#mvs do\n  result.mVals[i] = \n    {\n      name = mvs[i].name, \n      uid = mvs[i].uid, \n      extId = mvs[i].externalId,\n      ParObj=mvs[i].ParentObject.uid,\n      ParObjName = mvs[i].ParentObject.name,\n      MVT = mvs[i].MeasurementValueType.name}\n  if i >= 3 then break end\nend\n\nout.AddRecord(result)";
            var result = ck11Cli.ExecuteScriptAsync(modelUid, actualVersionId, body).Result;
            return result;

        }
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
    }
}
