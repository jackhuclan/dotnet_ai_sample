namespace Mega.Platform.Core;

public class MqttTopics
{
    public const string ALL = "mega/#";
    public const string OnlineTopicTemplate = "mega/login/p/{0}/d/{1}";
    public const string EventTopicTemplate = "mega/event/p/{0}/d/{1}";
    public const string StatusTopicTemplate = "mega/status/p/{0}/d/{1}";
    public const string AlarmTopicTemplate = "mega/alarm/p/{0}/d/{1}";
    public const string PropertyTopicTemplate = "mega/property/p/{0}/d/{1}";
    public const string ServiceTopicTemplate = "mega/service/p/{0}/d/{1}";
    public const string ServiceReplyTopicTemplate = "mega/service/p/{0}/d/{1}/reply";

    public static string GetSubscribeTopic(string format)
    {
        return string.Format(format, "+", "+");
    }

    public static string GetSubscribeTopic(string format, string productId, string deviceId)
    {
        return string.Format(format, productId, deviceId);
    }

    public static string GetReplySubscribeTopic(string format)
    {
        return string.Format(format, "+", "+");
    }
}
