namespace Mega.Platform.Core;

public class MqttTopics
{
    public const string OnlineTopicTemplate = "mega/mqtt/login/p/{0}/d/{1}";
    public const string EventTopicTemplate = "mega/mqtt/event/p/{0}/d/{1}";
    public const string StatusTopicTemplate = "mega/mqtt/status/p/{0}/d/{1}";
    public const string AlarmTopicTemplate = "mega/mqtt/alarm/p/{0}/d/{1}";
    public const string PropertyTopicTemplate = "mega/mqtt/property/p/{0}/d/{1}";
    public const string ServiceTopicTemplate = "mega/mqtt/sevice/p/{0}/d/{1}";

    public static string GetSubscribeTopic(string format)
    {
        return string.Format(format, "+", "+");
    }

    public static string GetSubscribeTopic(string format, string productId, string deviceId)
    {
        return string.Format(format, productId, deviceId);
    }
}
