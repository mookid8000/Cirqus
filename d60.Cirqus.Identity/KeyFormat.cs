using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.RegularExpressions;
using Sprache;

namespace d60.Cirqus.Identity
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
                    .Match((Placeholder t) =>
                    {
                        if (placeholderIndex >= args.Length)
                        {
                            if (!string.IsNullOrEmpty(t.Property))
                            {
                                throw new InvalidOperationException(string.Format(
                                    "You did not supply a value for the placeholder '{0}' at position {1}", 
                                    t.Property, placeholderIndex));
                            }
                            
                            throw new InvalidOperationException(string.Format(
                                "You did not supply a value for the *-placeholder or {{}}-placeholder at position {0}", 
                                placeholderIndex));
                        }

                        return args[placeholderIndex++].ToString();
                    })
                    .OrThrow(new ArgumentOutOfRangeException()))));
        }

        public void Apply(object target, string id)
        {
            var match = pattern.Match(id);

            var i = 1;
            foreach (var placeholder in Terms.OfType<Placeholder>())
            {
                if (string.IsNullOrEmpty(placeholder.Property))
                    continue;

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

        public string Get(string key, string id)
        {
            var match = pattern.Match(id);

            var i = 1;
            foreach (var placeholder in Terms.OfType<Placeholder>())
            {
                if (placeholder.Property == key)
                {
                    return match.Groups[i].Value;
                }

                i++;
            }

            throw new IndexOutOfRangeException();
        }

        public override string ToString()
        {
            return string.Join(@"/", Terms.Select(term =>
                new Switch<string>(term)
                    .Match((LiteralText t) => t.Text)
                    .Match((GuidKeyword t) => "guid")
                    .Match((Placeholder t) => string.IsNullOrEmpty(t.Property) ? "*" : "{" + t.Property + "}")
                    .OrThrow(new ArgumentOutOfRangeException())));
        }

        protected bool Equals(KeyFormat other)
        {
            return pattern.ToString().Equals(other.pattern.ToString());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KeyFormat) obj);
        }

        public override int GetHashCode()
        {
            return pattern.GetHashCode();
        }

        Regex ToRegex()
        {
            return new Regex(string.Join(@"/", Terms.Select(term =>
                new Switch<string>(term)
                    .Match((LiteralText t) => t.Text)
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