using System.Collections.Generic;
using Sprache;

namespace EnergyProjects.Domain.Model
{
    public static class KeyParser
    {
        static readonly Parser<char> Separator = Parse.Char('/');

        static readonly Parser<string> Identifier =
            from identifier in Parse.AnyChar.Except(Separator).Many().Text()
            where identifier != ""
            select identifier;

        static readonly Parser<char> PlaceholderBegin = Parse.Char('{');
        static readonly Parser<char> PlaceholderEnd = Parse.Char('}');

        static readonly Parser<KeyFormat.Term> Placeholder =
            from begin in PlaceholderBegin
            from property in Parse.AnyChar.Except(PlaceholderEnd).Many().Text()
            from end in PlaceholderEnd
            select new KeyFormat.Placeholder(property);

        static readonly Parser<KeyFormat.Term> GuidKeyword =
            from term in Identifier
            where term == "guid"
            select new KeyFormat.GuidKeyword();

        static readonly Parser<KeyFormat.Term> LiteralText =
            from text in Identifier
            select new KeyFormat.LiteralText(text);

        public static readonly Parser<KeyFormat.Term> Term =
            from term in Placeholder.XOr(GuidKeyword).XOr(LiteralText)
            select term;

        public static readonly Parser<KeyFormat> EmptySpecification =
            Parse.Return(new KeyFormat(new KeyFormat.GuidKeyword()));

        public static readonly Parser<KeyFormat> KeySpecification =
            EmptySpecification.XOr(
                from head in Term
                from tail in Separator.Then(x => Term).Many()
                select new KeyFormat(Cons(head, tail)));

        static IEnumerable<T> Cons<T>(T head, IEnumerable<T> rest)
        {
            yield return head;
            foreach (var item in rest)
                yield return item;
        }
    }
}