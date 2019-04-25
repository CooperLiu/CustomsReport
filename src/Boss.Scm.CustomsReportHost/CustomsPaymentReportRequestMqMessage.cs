using System;

namespace Boss.Scm.CustomsReportHost
{
    public class CustomsPaymentReportRequestMqMessage
    {
        public string DeclaredOrderNo { get; set; }

        public string OrderNo { get; set; }

        public string SessionId { get; set; }

        public DateTime RequestTime { get; set; }
    }
}