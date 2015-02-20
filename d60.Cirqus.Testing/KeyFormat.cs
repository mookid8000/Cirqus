using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sprache;

namespace EnergyProjects.Domain.Model
{
    public class KeyFormat
    {
        readonly Regex pattern;

        public KeyFormat(params Term[] terms) : this((IEnumerable<Term>)terms) { }

        public KeyFormat(IEnumerable<Term> terms)
        {
            Terms = terms.ToList();

            pattern = ToRegex();
        }

        public IReadOnlyList<Term> Terms { get; private set; }

        public static KeyFormat FromAttribute(KeyAttribute attribute)
        {
            return InjectGuidsInConstantIds(
                KeyParser.KeySpecification.Parse(
                    attribute.Format));
        }

        public bool Matches(string id)
        {
            return pattern.IsMatch(id);
        }

        public Id<T> Compile<T>(params object[] args)
        {
            var placeholderIndex = 0;
            return new Id<T>(this, string.Join("/", Terms.Select(term =>
                new Switch<string>(term)
                    .Match((LiteralText t) => t.Text)
                    .Match((GuidKeyword t) => Guid.NewGuid().ToString())
                    .Match((Placeholder t) => args[placeholderIndex++].ToString())
                    .OrThrow(new ArgumentOutOfRangeException()))));
        }

        public void Apply(object target, string id)
        {
            var match = pattern.Match(id);

            var i = 1;
            foreach (var placeholder in Terms.OfType<Placeholder>())
            {
                var property = target.GetType().GetProperty(placeholder.Property);
                var value = match.Groups[i].Value;

                var convertedValue =
                    new Switch<object>(property.PropertyType)
                        .Match<string>(() => value)
                        .Match<int>(() => int.Parse(value))
                        .Match<long>(() => long.Parse(value))
                        .OrThrow(new ArgumentOutOfRangeException());

                property.SetValue(target, convertedValue);

                i++;
            }
        }

        public override string ToString()
        {
            return string.Join(@"/", Terms.Select(term =>
                new Switch<string>(term)
                    .Match((LiteralText t) => t.Text)
                    .Match((GuidKeyword t) => "guid")
                    .Match((Placeholder t) => "{" + t.Property + "}")
                    .OrThrow(new ArgumentOutOfRangeException())));
        }

        Regex ToRegex()
        {
            return new Regex(string.Join(@"/", Terms.Select(term =>
                new Switch<string>(term)
                    .Match((LiteralText t) => @"[^/]+")
                    .Match((GuidKeyword t) => "([0-9a-fA-F]){8}(-([0-9a-fA-F]){4}){3}-([0-9a-fA-F]){12}")
                    .Match((Placeholder t) => @"([^/]+)")
                    .OrThrow(new ArgumentOutOfRangeException()))));
        }

        static KeyFormat InjectGuidsInConstantIds(KeyFormat result)
        {
            return result.Terms.All(x => x is LiteralText)
                ? new KeyFormat(result.Terms.Concat(new[] { new GuidKeyword() }))
                : result;
        }

        public interface Term { }

        public class LiteralText : Term
        {
            public LiteralText(string text)
            {
                Text = text;
            }

            public string Text { get; private set; }
        }

        public class GuidKeyword : Term {}

        public class Placeholder : Term
        {
            public Placeholder(string property)
            {
                Property = property;
            }

            public string Property { get; private set; }
        }

    }
}