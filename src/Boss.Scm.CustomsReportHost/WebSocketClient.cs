using System;
using Newtonsoft.Json;
using NLog;
using WebSocketSharp;

namespace Boss.Scm.CustomsReportHost
{
    public class WebSocketClient
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<SignSuccessEventArgs> OnSignSuccessed;

        private WebSocketChannel _webSocketChannel;

        public bool IsConnected => _webSocketChannel.IsConnected;


        public void Connect(string url)
        {
            var stocket = new WebSocket(url);

            _webSocketChannel = new WebSocketChannel(stocket);
            //_webSocketChannel.WebStockHeadBeatTimer = new ResetbaleTimer(3000);

            stocket.OnOpen += (sender, e) => { _logger.Info($"与[加签WebStocket服务]握手成功。"); };
            stocket.OnClose += (sender, e) => { _logger.Info($"与[加签WebStocket服务]关闭链接。Message:{e.Reason}"); };
            stocket.OnError += (sender, e) => { _logger.Info($"与[加签WebStocket服务]连接错误。Message:{e.Message}"); };
            stocket.OnMessage += (sender, e) =>
            {
                _logger.Info($"接收到[加签WebStocket服务]消息，{e.Data}");

                try
                {
                    var data = JsonConvert.DeserializeObject<SignWebStockCallbackMessage>(e.Data);
                    if (data.SignData != null && !string.IsNullOrEmpty(data.SignData.Sign))
                    {
                        OnSignSuccessed?.Invoke(sender, new SignSuccessEventArgs() { Id = data.Id, Sign = data.SignData.Sign });
                        _logger.Info($"加签成功");
                    }

                    if (data.SignData != null && data.SignData.Error?.Length > 0)
                    {
                        _logger.Error($"[加签WebStocket服务]返回错误：{data.SignData.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"签名返回数据解析异常，EX:{ex.StackTrace}");
                }
            };

            stocket.Connect();


        }

        public void Send(string message)
        {
            _webSocketChannel.Send(message);
        }

        public void Close()
        {
            _webSocketChannel.Close();
        }

    }

    public class SignSuccessEventArgs : EventArgs
    {
        /// <summary>
        /// 消息Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 签名值
        /// </summary>
        public string Sign { get; set; }
    }

    /// <summary>
    /// 发送签名消息
    /// </summary>
    public class SignWebStockMessage
    {
        [JsonProperty("_method")]
        public string Method { get; set; } = "cus-sec_SpcSignDataAsPEM";

        [JsonProperty("_id")]
        public int Id { get; set; }

        [JsonProperty("args")]
        public SignDataArgs SignDataArgs { get; set; }
    }

    public class SignDataArgs
    {
        [JsonProperty("inData")]
        public string InData { get; set; }

        [JsonProperty("passwd")]
        public string Passwd { get; set; }
    }


    /// <summary>
    /// 签名回调消息
    /// </summary>
    public class SignWebStockCallbackMessage
    {
        [JsonProperty("_id")]
        public int Id { get; set; }

        [JsonProperty("_method")]
        public string Method { get; set; }

        [JsonProperty("_status")]
        public string Status { get; set; }

        [JsonProperty("_args")]
        public SignData SignData { get; set; }
    }

    public class SignData
    {
        //{"Result":true,"Data":["SvdrQCa1/QuHWdapz5PbBZDQKWeRwphuNP/Ipa9eEI8GL873CFvqzDqJjmhf653Q2ikurJW4nZ7xmLxy0FosoKNRMXgkdPAUTV1HVnmLIowLIV+b7oXCo10XCNlNQ+qqjh2ulLbK1mpJOniDkB4SSt2dVcLl6x4Jct9pTH+gInY=","011f03d1"],"Error":[]}

        [JsonIgnore]
        public string Sign
        {
            get
            {
                if (Data != null && Data.Length > 0)
                {
                    if (Data.Length >= 2)
                    {
                        return Data[0].ToString();
                    }
                }

                return null;
            }
        }

        [JsonIgnore]
        public string ErrorMessage
        {
            get
            {
                if (Error != null && Error.Length > 0)
                {
                    return Error.JoinAsString(",");
                }
                return null;
            }
        }

        public bool Result { get; set; }

        public object[] Data { get; set; }

        public string[] Error { get; set; }
    }
}