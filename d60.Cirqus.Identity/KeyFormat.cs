using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text.RegularExpressions;
using Sprache;

namespace d60.Cirqus.Identity
{
    public class KeyFormat
    {
        const string guidPattern = "[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}";
        const string sguidPattern = "[A-Za-z0-9\\-_]{22}";

        static readonly IDictionary<Type, KeyFormat> formatByType;

        static KeyFormat()
        {
            formatByType = new ConcurrentDictionary<Type, KeyFormat>();

            SeparatorCharacter = '-';
            DefaultUniquenessTerm = "guid";
        }

        public static char SeparatorCharacter { get; set; }
        public static string DefaultUniquenessTerm { get; set; }

        readonly Regex pattern;

        public KeyFormat(params Term[] terms) : this((IEnumerable<Term>)terms) { }

        public KeyFormat(IEnumerable<Term> terms)
        {
            Terms = terms.ToList();

            pattern = ToRegex();
        }

        public IReadOnlyList<Term> Terms { get; private set; }

        public static void For<T>(string input)
        {
            var format = FromString(input);

            if (!format.Terms.Any(x => x is LiteralText))
                throw new ParseException("Format must contain a unique text identifying the type of id.");

            formatByType.Add(typeof(T), format);
        }

        public static Type GetTypeById(string id)
        {
            return formatByType.Single(x => x.Value.Matches(id)).Key;
        }

        public static KeyFormat Get<T>()
        {
            KeyFormat value;
            if (!formatByType.TryGetValue(typeof (T), out value))
            {
                return FromString("");
            }

            return value;
        }

        public static KeyFormat FromString(string format)
        {
            return new KeyFormatParser(SeparatorCharacter, DefaultUniquenessTerm).Execute(format);
        }

        public bool Matches(string id)
        {
            return pattern.IsMatch(id);
        }

        public Id<T> Compile<T>(params object[] args)
        {
            var placeholderIndex = 0;
            return new Id<T>(this, string.Join(SeparatorCharacter.ToString(),
                Terms.Select(term =>
                    new Switch<string>(term)
                        .Match((LiteralText t) => t.Text)
                        .Match((GuidKeyword t) => Guid.NewGuid().ToString())
                        .Match((SGuidKeyword t) => ToSGuid(Guid.NewGuid()))
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

        public string Normalize(string id)
        {
            var match = pattern.Match(id);

            if (!match.Success)
            {
                throw new InvalidOperationException(string.Format(
                    "The string '{0}' does not match the expected input '{1}'.", id, ToString()));
            }

            return string.Join(SeparatorCharacter.ToString(),
                Terms.Select((term, i) =>
                {
                    var value = match.Groups[i+1].Value;
                    return new Switch<string>(term)
                        .Match((LiteralText t) => t.Text)
                        .Match((GuidKeyword t) => value)
                        .Match((SGuidKeyword t) =>
                        {
                            Guid guid;
                            return Guid.TryParse(value, out guid) ? ToSGuid(guid) : value;
                        })
                        .Match((Placeholder t) => value)
                        .OrThrow(new ArgumentOutOfRangeException());
                }));

        }

        public void Apply(object target, string id)
        {
            var match = pattern.Match(id);

            var i = 0;
            foreach (var term in Terms)
            {
                i++;

                var placeholder = term as Placeholder;
                if (placeholder == null || string.IsNullOrEmpty(placeholder.Property))
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
            }
        }

        public string Get(string key, string id)
        {
            var match = pattern.Match(id);

            var i = 0;
            foreach (var term in Terms)
            {
                i++;

                var placeholder = term as Placeholder;
                if (placeholder == null || string.IsNullOrEmpty(placeholder.Property))
                    continue;

                if (placeholder.Property == key)
                {
                    return match.Groups[i].Value;
                }
            }

            throw new IndexOutOfRangeException();
        }

        public override string ToString()
        {
            return string.Join(SeparatorCharacter.ToString(),
                Terms.Select(term =>
                    new Switch<string>(term)
                        .Match((LiteralText t) => t.Text)
                        .Match((GuidKeyword t) => "guid")
                        .Match((SGuidKeyword t) => "sguid")
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
            return pattern.ToString().GetHashCode();
        }

        public static string ToSGuid(Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 22);
        }

        public static Guid FromSGuid(string sguid)
        {
            return new Guid(
                Convert.FromBase64String(sguid
                    .Replace("_", "/")
                    .Replace("-", "+") + "=="));
        }

        Regex ToRegex()
        {
            return new Regex(string.Format("^{0}$", string.Join(SeparatorCharacter.ToString(),
                Terms.Select(term =>
                    new Switch<string>(term)
                        .Match((LiteralText t) => t.Text)
                        .Match((GuidKeyword t) => guidPattern)
                        .Match((SGuidKeyword t) => "(?:" + guidPattern + ")|(?:" + sguidPattern + ")")
                        .Match((Placeholder t) => @"[^/]+")
                        .OrThrow(new ArgumentOutOfRangeException()))
                    .Select(x => string.Format("({0})", x)))));
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

        public class SGuidKeyword : Term {}

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