using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Events
{
    public class DisplayAttribute : Attribute
    {
        public DisplayAttribute(string template)
        {
            Template = template;
        }

        public string Template { get; private set; }
    }

    public class EventFormatter
    {
        public string Render(object @event)
        {
            var displayAttribute = @event.GetType().GetAttributeOrDefault<DisplayAttribute>() ?? new DisplayAttribute(null);
            return Render(@event, displayAttribute.Template);
        }

        public string Render(object @event, string template)
        {
            template = template ?? @event.GetType().Name;

            foreach (var match in new Regex(@"\{[^\}]\}+").Matches(template).OfType<Match>())
            {
               
            }

            var extras = new Dictionary<string, object>();
            var unused = new Dictionary<string, string>();

            var metaProperty = @event.GetType().GetProperty("Meta", typeof(Metadata));
            if (metaProperty != null)
            {
                var metadata = (Metadata)metaProperty.GetValue(@event);
                if (metadata.ContainsKey(DomainEvent.MetadataKeys.AggregateRootId))
                {
                    extras.Add("Id", metadata[DomainEvent.MetadataKeys.AggregateRootId]);
                }
            }

            return null;
            //foreach (var property in extras
            //    .Sele ct(property => new { Name = property.Key, property.Value })
            //    .Concat(@event.GetType()
            //        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            //        .Where(x => x.Name != "Meta")
            //        .Select(property => new { property.Name, Value = property.GetValue(@event) })))
            //{
            //    var placeholder = "{" + property.Name + "}";
            //    var value = property.Value != null ? property.Value.ToString() : "null";

            //    if (result.Contains(placeholder))
            //    {
            //        result = result.Replace(placeholder, value);
            //    }
            //    else
            //    {
            //        unused.Add(property.Name, value);
            //    }
            //}

            //if (!unused.Any()) return result;

            //return unused.Aggregate(result, (current, property) =>
            //    string.Format("{0}{1}", current, ("\n\t" + property.Key + ": " + property.Value)));
        }
        
    }
}