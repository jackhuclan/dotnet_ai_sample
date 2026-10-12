namespace Mega.Platform.Core;

public class ErrorCodes
{
    public static class Sys
    {
        public const string SUCCESS = "SUCCESS";
        public const string FAIL = "FAIL";

        public const string UNKNOWN_PRODUCT_CODE = "UNKNOWN_PRODUCT";
        public const string UNKNOWN_PRODUCT_MESSAGE = "不支持的设备类型";

        public const string OFFLINE_CODE = "OFFLINE_CODE";
        public const string OFFLINE_MESSAGE = "设备已离线";

        public const string UNKNOWN_CODE = "UNKNOWN_CODE";
        public const string UNKNOWN_MESSAGE = "未知的错误";
    }

}
