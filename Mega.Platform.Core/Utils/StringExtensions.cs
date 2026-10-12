using System.Text;

namespace Mega.Platform.Core.Utils;

public static class StringExtensions
{
    public static byte[] GetBytes(this string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    public static byte[] GetBytes(this string str, Encoding encoding)
    {
        return encoding.GetBytes(str);
    }
}
