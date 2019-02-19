using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace Boss.Scm.CustomsReportHost
{
    public class CustomsPaymentReport
    {
        private readonly string _customsPaymentReportUrl = ConfigurationManager.AppSettings["CustomsPaymentReportUrl"];

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();


        private static ConcurrentDictionary<string, byte[]> CertCache = new ConcurrentDictionary<string, byte[]>();

        private static Func<string, string, string> GetCertCacheKey = (url, certKey) => $"{url},{certKey}";

        private static HttpClient HttpClient = new HttpClient(GetCertificateHandler());


        private static HttpClientHandler GetCertificateHandler()
        {
            var handler = new WebRequestHandler();

            var provider = new CustomsReportSettingProvider();

            var setting = provider.GetCustomsReportSetting();


            var key = GetCertCacheKey(setting.CertFilePath, setting.CertPassword);
            var certStream = CertCache.GetOrAdd(key, ReadCertFile(setting.CertFilePath));
            X509Certificate2 cert = new X509Certificate2(certStream, setting.CertPassword);

            handler.ClientCertificates.Add(cert);

            return handler;

        }

        private static byte[] ReadCertFile(string file)
        {
            using (var fileStream = new FileStream(file, FileMode.Open))
            {
                var fileSize = fileStream.Length;
                var buffer = new byte[fileSize];

                fileStream.Read(buffer, 0, buffer.Length);

                return buffer;
            }
        }



        /// <summary>
        /// 上报数据
        /// </summary>
        /// <returns></returns>
        public async Task Report(PaymentReportData input)
        {

            var data = input.ToSpecFormatJsonString();

            _logger.Info($"海关上报支付数据：[上报] {data} ");

            using (var msg = new HttpRequestMessage(HttpMethod.Post, _customsPaymentReportUrl))
            {
                msg.Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("payExInfoStr", data) });

                var response = await HttpClient.SendAsync(msg);

                var resp = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<PaymentReportResult>(resp);

                if (!response.IsSuccessStatusCode && !result.Success)
                {
                    _logger.Error($"海关上报支付数据：[上报失败] HTTPCODE：{response.StatusCode}， 请求数据 {data}，响应 {resp} ");
                }
                else
                {
                    _logger.Info($"海关上报支付数据：[上报成功] HTTPCODE：{response.StatusCode}， 请求数据 {data}，响应 {resp} ");
                }


            }

        }
    }

    /// <summary>
    /// 海关支付数据上报响应数据
    /// </summary>
    public class PaymentReportResult
    {

        [JsonIgnore]
        public bool Success => !string.IsNullOrEmpty(code) && code == "10000";

        public string code { get; set; }

        public string message { get; set; }

        public int total { get; set; }

        public long serviceTime { get; set; }

        [JsonIgnore]
        public DateTime ResponseTime => serviceTime.GetDateTimeFromUnixTimeStampMillis();
    }


    /// <summary>
    /// 海关支付数据上报请求数据
    /// </summary>
    public class PaymentReportData
    {
        public PaymentReportData()
        {
            SystemTime = DateTime.Now;
            payExchangeInfoLists = new List<PayExchangeInfoList>();
        }

        /// <summary>
        /// 上传任务Id
        /// </summary>
        [JsonIgnore]
        public int TaskId { get; set; }

        /// <summary>
        /// 海关总署请求SessionID
        /// </summary>
        public string sessionID { get; set; }

        /// <summary>
        /// 支付原始数据表头
        /// </summary>
        public PayExchangeInfoHead payExchangeInfoHead { get; set; }

        /// <summary>
        /// 支付原始数据表体
        /// </summary>
        public List<PayExchangeInfoList> payExchangeInfoLists { get; set; }

        [JsonIgnore]
        public DateTime SystemTime { get; set; }

        /// <summary>
        /// 返回系统的时间
        /// </summary>
        public string serviceTime => SystemTime.ToUnixTimeStampMillis().ToString();

        /// <summary>
        /// 证书编号
        /// </summary>
        public string certNo { get; set; }

        /// <summary>
        /// 签名结果值
        /// </summary>
        public string signValue { get; set; }

        public string GetSignString() //TODO Check this
        {
            var format = $"\"sessionID\":\"{sessionID}\"||\"payExchangeInfoHead\":\"{payExchangeInfoHead.ToSpecFormatJsonString()}\"||\"payExchangeInfoLists\":\"{payExchangeInfoLists.ToSpecFormatJsonString()}\"||\"serviceTime\":\"{serviceTime}\"";

            return format;
        }

    }

    static class JsonObjectExtensions
    {
        public static string ToSpecFormatJsonString(this object obj, JsonSerializerSettings settings = null)
        {
            var serializerSettings = settings ?? new JsonSerializerSettings();
            serializerSettings.Converters.Add(new SpecFormatJsonConventor());

            var data = JsonConvert.SerializeObject(obj, serializerSettings);

            var pattern = "\"totalAmount\":[1-9]\\d*\\.?\\d*";

            var matchResult = Regex.Match(data, pattern);

            var matchValue = matchResult.Value;

            if (matchResult.Success && matchValue.Contains(".") && matchValue.EndsWith("0"))
            {
                data = data.Replace(matchValue,
                    matchValue.Split('.')[1].Length == 1
                        ? matchValue.Remove(matchValue.Length - 2, 2)
                        : matchValue.Remove(matchValue.Length - 1, 1));
            }

            return data;
        }
    }

    /// <summary>
    /// 特殊Json转换。
    /// 转换Guid为大写形式
    /// 转换decimal移除尾数零
    /// </summary>
    class SpecFormatJsonConventor : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Guid)
            {
                var format = ((Guid)value).ToString("D").ToUpper();
                writer.WriteValue(format);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Guid);

        }

        public override bool CanRead => false;
    }



    /// <summary>
    /// 支付原始数据表头
    /// </summary>
    public class PayExchangeInfoHead
    {
        public PayExchangeInfoHead()
        {
            guid = Guid.NewGuid();
            currency = "142";
            note = "批量订单，测试订单优化,生成多个so订单";
        }

        /// <summary>
        /// 系统唯一序号	Guid	C..36	企业系统生成36位唯一序号（英文字母大写）。	是
        /// </summary>
        [JsonConverter(typeof(SpecFormatJsonConventor))]
        public Guid guid { get; set; }

        /// <summary>
        /// 原始请求	initalRequest	C..8000	跨境电商平台企业向支付企业发送的原始信息	是
        /// </summary>
        public string initalRequest { get; set; }

        /// <summary>
        /// 原始响应	initalResponse	C..8000	支付企业向跨境电商平台企业反馈的原始信息	是
        /// </summary>
        public string initalResponse { get; set; }

        /// <summary>
        /// 电商平台代码	ebpCode	C..18	电商平台的海关注册登记编号。	是
        /// </summary>
        public string ebpCode { get; set; }

        /// <summary>
        /// 支付企业代码	payCode	C..18	支付企业的海关注册登记编号。	是
        /// </summary>
        public string payCode { get; set; }

        /// <summary>
        /// 交易流水号	payTransactionId	C..60	交易唯一编号（可在央行认可的机构验证）	是
        /// </summary>
        public string payTransactionId { get; set; }

        /// <summary>
        /// 交易金额	totalAmount	N19,5	实际交易金额	是
        /// </summary>
        //[JsonConverter(typeof(SpecFormatJsonConventor))]
        public decimal totalAmount { get; set; }

        /// <summary>
        /// 币制	currency	C..4	实际交易币制（海关编码）	是
        /// </summary>
        public string currency { get; set; }

        /// <summary>
        /// 验核机构	verDept	C1	1-银联 2-网联 3-其他	是
        /// </summary>
        public string verDept { get; set; }

        /// <summary>
        /// 支付类型	payType	C1	用户支付的类型。1-APP 2-PC 3-扫码 4-其他	否
        /// </summary>
        public string payType { get; set; }

        /// <summary>
        /// 交易成功时间
        /// </summary>
        [JsonIgnore]
        public DateTime PaidTime { get; set; }

        /// <summary>
        /// 交易成功时间	tradingTime	C..14	交易支付时间。	是 ,格式：yyyyMMddHHmmss
        /// </summary>
        public string tradingTime => PaidTime.ToString("yyyyMMddHHmmss");

        /// <summary>
        /// 备注	note	C..1000		否
        /// </summary>
        public string note { get; set; }
    }

    /// <summary>
    /// 支付原始数据表体
    /// </summary>
    public class PayExchangeInfoList
    {
        public PayExchangeInfoList()
        {
            goodsInfo = new List<PayExchangeGoodsInfo>();
        }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string orderNo { get; set; }

        /// <summary>
        /// 商品信息
        /// </summary>
        public List<PayExchangeGoodsInfo> goodsInfo { get; set; }

        /// <summary>
        /// 收款账户，微信支付和支付宝账户
        /// </summary>
        public string recpAccount { get; set; }

        /// <summary>
        /// 收款企业代码
        /// </summary>
        public string recpCode { get; set; }

        /// <summary>
        /// 收款企业名称
        /// </summary>
        public string recpName { get; set; }
    }

    public class PayExchangeGoodsInfo
    {

        [JsonIgnore]
        public string orderNo { get; set; }

        /// <summary>
        /// 商品名称
        /// </summary>
        public string gname { get; set; }

        /// <summary>
        /// 商品展示链接地址
        /// </summary>
        public string itemLink { get; set; }
    }
}