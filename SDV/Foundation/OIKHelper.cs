using System;
using System.Collections.Generic;
using System.Threading;
using OICDAC;

namespace ArhiveKDD
{
    public class OIKHelper
    {
        public delegate void OnDataReceived(List<OikRequestResult> result);

        private static OIKHelper _instance;
        private readonly DAC _dac;

        private readonly List<OikRequestResult> _requestResult = new List<OikRequestResult>();
        private OIRequest _rq;

        private OIKHelper()
        {
            _dac = new DAC();
        }

        public static OIKHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new OIKHelper();
                }

                return _instance;
            }
        }

        /// <summary>
        ///     Событие возникает при получении всех запрошенных данных от ОИК
        /// </summary>
        public event OnDataReceived DataReceived;

        public void CloseConnection()
        {
            _dac.Connection.Connected = false;
        }

        public void OpenDefaultConnection()
        {
            // подключение к основному ОИК
            _dac.Connection.Connected = false;
            _dac.Connection.RTDBTaskName = "FoosJA";
            _dac.Connection.ConnectKind = ConnectKindEnum.ck_Default;

            var connectTryNum = 0;
            while (!_dac.Connection.Connected && connectTryNum < 6)
            {
                connectTryNum++;
                try
                {
                    _dac.Connection.Connected = true;
                    _dac.Connection.ADOConnection.ConnectionTimeout = 5;
                    _dac.Connection.ADOConnection.CommandTimeout = 10;
                }
                catch (Exception)
                {
                    //MessageHelper.SendMessage(this, MessageType.Error,
                    //    $"Попытка соединения №{connectTryNum} неудачна: \r\n{ex.Message}");
                }

                Thread.Sleep(100);
            }
        }

        internal void WriteInformationToOIK(Dictionary<string, object> valuesToWrite)
        {
            if (!_dac.Connection.Connected)
            {
                return;
            }

            StopRequest();
            _requestResult.Clear();

            if (_rq != null)
            {
                _rq.OIRequestItems.Clear();
            }
            else
            {
                _rq = _dac.OIRequests.Add();
                _rq.UseMilliseconds = true;
                _rq.OnGetResult += Request_OnGetResult;
                _rq.OnReceivedAllData += Request_OnReceivedAllData;
            }



            foreach (var item in valuesToWrite)
            {
                var rqi = _rq.AddOIRequestItem();
                rqi.KindRefresh = KindRefreshEnum.kr_WriteData;
                rqi.DataValue = item.Value;
                rqi.Sign = (int)ValueSignEnum.vs_Aux;
                rqi.DataSource = item.Key;
            }

            // выполняем запрос
            _rq.Start();
        }

        private void ConnectSelected()
        {
            // подключение к ОИК, выбранному из дерева
            _dac.Connection.Connected = false;
            _dac.Connection.ShowDialog();
        }

        private void CreateRequest()
        {
            if (_rq != null)
            {
                _rq.OIRequestItems.Clear();
                return;
            }

            _rq = _dac.OIRequests.Add();
            _rq.UseMilliseconds = true;
            _rq.OnGetResult += Request_OnGetResult;
            _rq.OnReceivedAllData += Request_OnReceivedAllData;
        }

        private void Request_OnGetResult(string dataSource, KindRefreshEnum kindRefresh, DateTime time, object data,
            int sign, int tag)
        {
            _requestResult.Add(new OikRequestResult(dataSource, kindRefresh, time, data, sign, tag));
        }

        private void Request_OnReceivedAllData()
        {
            DataReceived?.Invoke(_requestResult);
        }

        public void StopRequest()
        {
            if (_dac.OIRequests.Count > 0)
            {
                _dac.OIRequests.Item(0).Stop();
            }
        }

        public void RunGetDataForDate(DateTime date, string[] oikParams)
        {
            // запрос значений на интервале
            if (!_dac.Connection.Connected)
            {
                return;
            }

            StopRequest();

            _requestResult.Clear();


            if (oikParams.Length > 0)
            {
                CreateRequest();

                foreach (var s in oikParams)
                {
                    var rqi = _rq.AddOIRequestItem();
                    rqi.IsLocalTime = true;
                    rqi.TimeStart = date;
                    rqi.KindRefresh = KindRefreshEnum.kr_RequiredTime;
                    rqi.DataSource = s;
                }

                // выполняем запрос
                _rq.Start();
            }
        }

        public DAC IOCDAC => _dac;
        public const ValueSignEnum IS_NOTTRUSTED = /*ValueSignEnum.vs_Ren;*/ 
            ValueSignEnum.vs_OSC | ValueSignEnum.vs_DTrust | ValueSignEnum.vs_PNU | ValueSignEnum.vs_FParam |
            ValueSignEnum.vs_TMF | ValueSignEnum.vs_Ren | ValueSignEnum.vs_CalcF | ValueSignEnum.vs_DDup |
            ValueSignEnum.vs_Fbound | ValueSignEnum.vs_Estim | ValueSignEnum.vs_NoData | ValueSignEnum.vs_Leap |
            ValueSignEnum.vs_Jump;
        // признак подтверждения достоверности
        private const ValueSignEnum IS_RECONFIRMED = ValueSignEnum.vs_Dup | ValueSignEnum.vs_Manual;
        public void GetDataForInterval(DateTime dtStart, DateTime dtStop, string[] oikParams)
        {
            // запрос значений на интервале
            if (!_dac.Connection.Connected)
            {
                return;
            }

            StopRequest();

            if (oikParams.Length > 0)
            {
                CreateRequest();

                foreach (var s in oikParams)
                {
                    // добавляем элемент запроса за последний час с шагом 5 мин
                    var rqi = _rq.AddOIRequestItem();
                    rqi.IsLocalTime = true;
                    rqi.TimeStart = dtStart;
                    rqi.TimeStop = dtStop;
                    rqi.TimeStep = 0;
                    rqi.KindRefresh = KindRefreshEnum.kr_Period;
                    rqi.DataSource = s;
                }

                // выполняем запрос
                _rq.Start();
            }
        }

        public bool SignCodeCorrect(string code)
        {
            double dCode;
            return double.TryParse(code, out dCode) && SignCodeCorrect(dCode);
        }

        private bool SignCodeCorrect(double dCode)
        {
            throw new NotImplementedException();
        }
    }

    public class OikRequestResult
    {
        public OikRequestResult(string dataSource, KindRefreshEnum kindRefresh, DateTime time, object data, int sign,
            int tag)
        {
            DataSource = dataSource;
            KindRefresh = kindRefresh;
            Time = time;
            Data = data;
            Sign = sign;
            Tag = tag;
        }

        public string DataSource { get; set; }
        public KindRefreshEnum KindRefresh { get; set; }
        public DateTime Time { get; set; }

        public object Data { get; set; }
        public long Sign { get; set; }

        public double Tag { get; set; }
    }
}
