using System;
using System.Threading.Tasks;
using Boss.Scm.Customs;
using Rebus.Handlers;
using System.Text;
using Boss.Scm.CustomsReportHost.SDK;
using NLog;

namespace Boss.Scm.CustomsReportHost
{
    public class CustomsReportHandler : IHandleMessages<CustomsPaymentReportRequestMqMessage>
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly CustomsReportSetting _settings;

        private readonly CustomsReportDataProvider _customsReportDataProvider;

        private readonly CustomsPaymentReport _reporter;

        public CustomsReportHandler()
        {
            var settingProvider = new CustomsReportSettingProvider();
            _settings = settingProvider.GetCustomsReportSetting();

            _reporter = new CustomsPaymentReport();

            _customsReportDataProvider = new CustomsReportDataProvider();

        }


        public async Task Handle(CustomsPaymentReportRequestMqMessage message)
        {
            try
            {

                var data = await _customsReportDataProvider.GetReportData(message.DeclaredOrderNo, _settings);


                var signStr = data.GetSignString();

                _logger.Info($"海关上报支付数据：[加签] 加签字符 {signStr}");

                var signBytes = Encoding.UTF8.GetBytes(signStr);

                var sign = new byte[172];
                var signLen = 200;

                var result = SignSdkApi.Sign(signBytes, signBytes.Length, sign, ref signLen, _settings.CertPassword);

                if (result == SignSdkApi.SignResult.SignSuccess)
                {
                    data.signValue = Encoding.UTF8.GetString(sign);

                    data.certNo = _settings.CertNo;

                    _logger.Info($"海关上报支付数据：[加签] 签名 {data.signValue}");


                    await _reporter.Report(data);
                }
                else
                {
                    _logger.Error($"海关上报支付数据：[签名错误]，MSG:{SignSdkApi.SignResult.GetResultMessage((int)result)}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"海关上报支付数据：[上报异常]，MSG:{ex.Message},\r\n Stack:{ex.StackTrace}");
            }

        }

    }
}