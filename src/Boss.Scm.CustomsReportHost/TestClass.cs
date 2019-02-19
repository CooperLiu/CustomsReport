using System;
using System.Text;
using Boss.Scm.Customs;
using Boss.Scm.CustomsReportHost.SDK;
using Rebus.Activation;

namespace Boss.Scm.CustomsReportHost
{
    public class TestClass
    {
        private static CustomsReportSetting _testSetting = new CustomsReportSettingProvider().GetCustomsReportSetting();

        private readonly BuiltinHandlerActivator _ioc;
        public TestClass(BuiltinHandlerActivator ioc)
        {
            _ioc = ioc;
        }

        public void Trigger()
        {
            //_ioc.Bus.Publish(new CustomsPaymentReportRequestMqMessage()
            //{
            //    DeclaredOrderNo = "DD19011700128756",
            //    SessionId = "fe2374-8fnejf97-9bcd5678",
            //    RequestTime = DateTime.Now
            //});




            var provider = new CustomsReportDataProvider();

            var data = AsyncHelper.RunSync(() => provider.GetReportData("DD19011700128756", _testSetting));



            var signStr = data.GetSignString();

            var signBytes = Encoding.UTF8.GetBytes(signStr);

            var sign = new byte[172];
            var signLen = 200;



            var result = SignSdkApi.Sign(signBytes, signBytes.Length, sign, ref signLen, _testSetting.CertPassword);

            if (result == SignSdkApi.SignResult.SignSuccess)
            {
                data.signValue = Encoding.UTF8.GetString(sign);

                data.certNo = _testSetting.CertNo;

                var reporter = new CustomsPaymentReport();

                AsyncHelper.RunSync(() => reporter.Report(data));
            }


            Console.WriteLine($"签名 {Encoding.UTF8.GetString(sign)}");


        }
    }
}