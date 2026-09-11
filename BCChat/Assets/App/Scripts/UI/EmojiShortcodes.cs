using System.Text;

/// <summary>
/// Expands GitHub/gemoji-style :shortcodes: using the same alias table as BCChat C++.
/// </summary>
public static partial class EmojiShortcodes
{
    public static string Expand(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        StringBuilder result = new(text.Length);
        int cursor = 0;
        while (cursor < text.Length)
        {
            int start = text.IndexOf(':', cursor);
            if (start < 0)
            {
                result.Append(text, cursor, text.Length - cursor);
                break;
            }

            result.Append(text, cursor, start - cursor);
            int end = text.IndexOf(':', start + 1);
            if (end < 0)
            {
                result.Append(text, start, text.Length - start);
                break;
            }

            string name = text.Substring(start + 1, end - start - 1);
            if (!IsValidName(name) || !Aliases.TryGetValue(name, out string emoji))
            {
                // Preserve the unknown colon and continue scanning so URL
                // schemes and unknown tokens cannot hide a later shortcode.
                result.Append(':');
                cursor = start + 1;
                continue;
            }

            result.Append(emoji);
            cursor = end + 1;
        }

        return result.ToString();
    }

    private static bool IsValidName(string name)
    {
        if (name.Length == 0 || name.Length > 64)
        {
            return false;
        }

        foreach (char character in name)
        {
            if (!((character >= 'a' && character <= 'z') ||
                  (character >= 'A' && character <= 'Z') ||
                  (character >= '0' && character <= '9') ||
                  character == '_' || character == '+' || character == '-'))
            {
                return false;
            }
        }

        return true;
    }
}
