using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;

namespace Boss.Scm.CustomsReportHost
{
    public class CustomsReportDataProvider
    {


        public async Task<PaymentReportData> GetReportData(string orderNo, CustomsReportSetting setting)
        {
            var _testData = new PaymentReportData()
            {
                TaskId = 100,
                sessionID = $"fe2374-8fnejf97-{DateTime.Now.ToString("hhmmss")}",
                payExchangeInfoHead = new PayExchangeInfoHead()
                {
                    guid = Guid.Parse("AB9BBD80-8A14-4FA9-8B5B-EF692DB75CDA"),
                    initalRequest = "https://openapi.alipay.com/gateway.do?timestamp=2013-01-0108:08:08&method=alipay.trade.pay&app_id=13580&sign_type=RSA2&sign=ERITJKEIJKJHKKKKKKKHJEREEEEEEEEEEE&version=1.0&charset=GBK",
                    initalResponse = "ok",
                    ebpCode = "1106963386",
                    payCode = "312226T001",
                    payTransactionId = "2018121222001354081010726129",
                    totalAmount = 100,
                    currency = "142",
                    verDept = "3",
                    payType = "1",
                    PaidTime = DateTime.Parse("2018/12/12 04:18:03")
                },
                payExchangeInfoLists = new List<PayExchangeInfoList>()
                {
                    new PayExchangeInfoList()
                    {
                        orderNo = orderNo,
                        goodsInfo = new List<PayExchangeGoodsInfo>()
                        {
                            new PayExchangeGoodsInfo(){ gname = "lhy-gnsku3",itemLink = "http://m.yunjiweidian.com/yunjibuyer/static/vue-buyer/idc/index.html#/detail?itemId=999761&shopId=453"},
                            new PayExchangeGoodsInfo(){ gname = "lhy-gnsku2",itemLink = "http://m.yunjiweidian.com/yunjibuyer/static/vue-buyer/idc/index.html#/detail?itemId=999760&shopId=453"}
                        },
                        recpAccount = "OSA571908863132601",
                        recpCode = "",
                        recpName="YUNJIHONGKONGLIMITED"
                    }
                },
                SystemTime = DateTime.Parse("2018/12/12 04:18:03"),
                certNo = setting.CertNo
            };


            return await Task.FromResult(_testData);
        }


    }
}