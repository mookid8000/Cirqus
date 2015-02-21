using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Testing
{
    public class EventFormatter
    {
        readonly TextFormatter formatter;

        public EventFormatter(TextFormatter formatter)
        {
            this.formatter = formatter;
        }

        public void Format(object @event)
        {
            var displayAttribute = @event.GetType().GetAttributeOrDefault<DisplayAttribute>() ?? new DisplayAttribute(null);
            Format(displayAttribute.Template, @event);
        }

        public void Format(string template, object @event)
        {
            template = template ?? @event.GetType().Name;

            foreach (var match in new Regex(@"\{[^\}]\}+").Matches(template).OfType<Match>()) {}

            var extras = new Dictionary<string, object>();
            var unused = new Dictionary<string, string>();

            var metaProperty = @event.GetType().GetProperty("Meta", typeof (Metadata));
            if (metaProperty != null)
            {
                var metadata = (Metadata) metaProperty.GetValue(@event);
                if (metadata.ContainsKey(DomainEvent.MetadataKeys.AggregateRootId))
                {
                    extras.Add("Id", metadata[DomainEvent.MetadataKeys.AggregateRootId]);
                }
            }

            var result = template;
            foreach (var property in extras
                .Select(property => new {Name = property.Key, property.Value})
                .Concat(@event.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.Name != "Meta")
                    .Select(property => new {property.Name, Value = property.GetValue(@event)})))
            {
                var placeholder = "{" + property.Name + "}";
                var value = property.Value != null ? property.Value.ToString() : "null";

                if (result.Contains(placeholder))
                {
                    result = result.Replace(placeholder, value);
                }
                else
                {
                    unused.Add(property.Name, value);
                }
            }

            formatter.Write(result);

            if (!unused.Any()) return;

            formatter.NewLine();
            formatter.Indent();
            foreach (KeyValuePair<string, string> pair in unused)
            {
                formatter.Write(pair.Key + ": " + pair.Value);
            }
            formatter.Unindent();
        }
    }

    public class TextFormatter
    {
        const string indent = "  ";

        readonly IWriter writer;

        int indentation;
        int cursor;
        int margin;
        string current;

        public TextFormatter(IWriter writer)
        {
            this.writer = writer;
        }

        public TextFormatter Indent()
        {
            indentation++;
            return this;
        }

        public TextFormatter Unindent()
        {
            if (indentation > 0)
            {
                indentation--;
            }
            return this;
        }

        public TextFormatter NewLine()
        {
            cursor = 0;
            margin++;
            writer.Write("\r\n");
            return this;
        }

        public TextFormatter Write(object obj, EventFormatter formatter)
        {
            formatter.Format(obj);
            return this;
        }

        public TextFormatter Write(string str)
        {
            if (cursor == 0)
            {
                writer.Write(string.Join("", Enumerable.Repeat(indent, indentation)));
            }

            margin = 0;
            cursor += str.Length;
            writer.Write(str);
            return this;
        }

        public TextFormatter Block(string header)
        {
            if (current == header)
                return this;

            if (current != null)
            {
                EndBlock();
            }

            current = header;

            if (cursor > 0 || margin > 0)
            {
                if (cursor > 0)
                {
                    NewLine();
                }

                while (margin < 2)
                {
                    NewLine();
                }
            }

            Write(header);
            NewLine();
            Indent();
            return this;
        }

        public TextFormatter EndBlock()
        {
            current = null;
            Unindent();
            return this;
        }
    }
}