using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Mega.Platform.Web.Controllers;

/// <summary>
/// 前端按 OpenAI 格式发送 <c>{ "role", "content" }</c>。
/// <see cref="ChatMessage"/> 的正文在 <c>contents</c> 上，<c>Text</c> 又被忽略，直接绑定会丢掉消息文本。
/// </summary>
public sealed class ChatMessageListJsonConverter : JsonConverter<IList<ChatMessage>>
{
    public override IList<ChatMessage> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return [];
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var messages = new List<ChatMessage>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            messages.Add(ReadMessage(item, options));
        }

        return messages;
    }

    public override void Write(Utf8JsonWriter writer, IList<ChatMessage> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var message in value)
        {
            writer.WriteStartObject();
            writer.WriteString("role", message.Role.Value);
            writer.WriteString("content", message.Text);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static ChatMessage ReadMessage(JsonElement item, JsonSerializerOptions options)
    {
        var role = new ChatRole(ReadString(item, "role") ?? ChatRole.User.Value);
        if (TryGetProperty(item, "content", out var content))
        {
            return new ChatMessage(role, ReadContent(content));
        }

        if (TryGetProperty(item, "contents", out _))
        {
            var native = JsonSerializer.Deserialize<ChatMessage>(item.GetRawText(), options);
            if (native != null)
            {
                native.Role = role;
                return native;
            }
        }

        return new ChatMessage(role, string.Empty);
    }

    private static string ReadContent(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var part in content.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.String)
            {
                var text = part.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    parts.Add(text);
                }

                continue;
            }

            var partText = ReadString(part, "text");
            if (!string.IsNullOrEmpty(partText))
            {
                parts.Add(partText);
            }
        }

        return string.Join(string.Empty, parts);
    }

    private static string? ReadString(JsonElement item, string name)
    {
        return TryGetProperty(item, name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool TryGetProperty(JsonElement item, string name, out JsonElement value)
    {
        if (item.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in item.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
