using System.Buffers;
using System.Text;

namespace Mega.Platform.Core.Utils;

public static class ReadOnlySequenceExtensions
{
    public static string ToStr(this ReadOnlySequence<byte> segment)
    {
        if (segment.IsEmpty) return string.Empty;
        return Encoding.UTF8.GetString(segment);
    }
}
