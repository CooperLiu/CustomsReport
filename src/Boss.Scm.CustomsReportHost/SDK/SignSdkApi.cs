using System.Runtime.InteropServices;

namespace Boss.Scm.CustomsReportHost.SDK
{
    public class SignSdkApi
    {
        /// <summary>
        /// 签名成功
        /// </summary>
        public const int SignSuccess = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="szCardID"></param>
        /// <param name="nCardIDLen"></param>
        /// <returns></returns>
        [DllImport(".\\SDK\\usercard_cert64\\Sign64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetCardID([MarshalAs(UnmanagedType.LPArray)] byte[] szCardID, ref int nCardIDLen);

        [DllImport(".\\SDK\\usercard_cert64\\Sign64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetCertNo([MarshalAs(UnmanagedType.LPArray)] byte[] szCertNo, ref int nCertNoLen);

        /// <summary>
        /// 用加密设备传入报文进行加签
        /// </summary>
        /// <param name="src">【in】src 待签名的原始数据</param>
        /// <param name="srcLen">【in】srcLen 待签名的原始数据的长度</param>
        /// <param name="sign">【out】sign签名数据(至少分配128字节)，!!固定分配172字节!!</param>
        /// <param name="signLen">【in，out】signLen 签名数据长度，应大于128个字节，输入时应等于szSignData实际分配的空间大小</param>
        /// <param name="pwd">【in】pwd	进行加签的卡密码</param>
        /// <returns></returns>
        [DllImport(".\\SDK\\usercard_cert64\\Sign64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Sign([MarshalAs(UnmanagedType.LPArray)] byte[] src, int srcLen, [MarshalAs(UnmanagedType.LPArray)] byte[] sign, ref int signLen, string pwd);

        /// <summary>
        /// 签名结果
        /// </summary>
        public static class SignResult
        {
            public static string GetResultMessage(int errorCode)
            {
                switch (errorCode)
                {
                    case 0: return nameof(SignSuccess);
                    case -1: return nameof(CardInitError);
                    case -2: return nameof(CardPwdError);
                    case -3: return nameof(SignFailed);
                    case -4: return nameof(PemEncodingError);
                }

                return "Unknown";
            }

            /// <summary>
            /// 签名成功
            /// </summary>
            public const int SignSuccess = 0;

            /// <summary>
            /// 卡初始化错
            /// </summary>
            public const int CardInitError = -1;

            /// <summary>
            /// 卡口令不正确
            /// </summary>
            public const int CardPwdError = -2;

            /// <summary>
            /// 签名失败
            /// </summary>
            public const int SignFailed = -3;

            /// <summary>
            /// PEM编码失败
            /// </summary>
            public const int PemEncodingError = -4;
        }

    }
}