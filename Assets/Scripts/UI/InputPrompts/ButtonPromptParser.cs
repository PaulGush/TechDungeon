using System;
using System.Text;

namespace UI.InputPrompts
{
    public static class ButtonPromptParser
    {
        public const string SpriteAssetName = "InputPrompts";

        public static ButtonPromptDatabase Database { get; set; }

        public static Func<string, ActiveDevice, string> LabelResolver { get; set; }

        public static string Format(string template, ActiveDevice device)
        {
            if (string.IsNullOrEmpty(template)) return template;
            if (template.IndexOf('[') < 0) return template;

            var sb = new StringBuilder(template.Length + 32);
            int i = 0;
            while (i < template.Length)
            {
                char c = template[i];
                if (c == '[')
                {
                    int end = template.IndexOf(']', i + 1);
                    if (end < 0)
                    {
                        sb.Append(template, i, template.Length - i);
                        break;
                    }
                    string actionName = template.Substring(i + 1, end - i - 1);
                    AppendSpriteTag(sb, actionName, device);
                    i = end + 1;
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
        }

        private static void AppendSpriteTag(StringBuilder sb, string actionName, ActiveDevice device)
        {
            if (Database != null && Database.TryGet(actionName, out var entry) && entry.Get(device) != null)
            {
                string spriteName = ButtonPromptDatabase.SpriteName(actionName, device);
                sb.Append("<sprite=\"").Append(SpriteAssetName).Append("\" name=\"").Append(spriteName).Append("\">");
                return;
            }

            if (LabelResolver != null)
            {
                string label = LabelResolver(actionName, device);
                if (!string.IsNullOrEmpty(label))
                {
                    sb.Append('[').Append(label).Append(']');
                    return;
                }
            }

            sb.Append('[').Append(actionName).Append(']');
        }
    }
}
