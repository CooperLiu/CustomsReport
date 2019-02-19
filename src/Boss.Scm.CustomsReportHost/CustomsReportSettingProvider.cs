namespace Boss.Scm.CustomsReportHost
{
    public class CustomsReportSettingProvider
    {
        public CustomsReportSetting GetCustomsReportSetting()
        {
            return CustomsReportSetting.Current;
        }
    }

    public class CustomsReportSetting
    {
        /// <summary>
        /// 证书序列号
        /// </summary>
        public string CertNo { get; set; }

        /// <summary>
        /// 证书路径
        /// </summary>
        public string CertFilePath { get; set; }

        /// <summary>
        /// 证书密码
        /// </summary>
        public string CertPassword { get; set; }

        /// <summary>
        /// 企业名称
        /// </summary>
        public string CorpName { get; set; }

        /// <summary>
        /// 企业统一社会信用代码
        /// </summary>
        public string CorpCode { get; set; }

        public static CustomsReportSetting Current = new CustomsReportSetting()
        {
            CertNo = "XXXX",
            CertFilePath = "XXXX",
            CertPassword = "XXXX",
            CorpName = "XXXX",
            CorpCode = "XXXX",
        };
    }
}