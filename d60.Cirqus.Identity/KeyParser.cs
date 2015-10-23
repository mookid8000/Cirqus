using System.Collections.Generic;
using Sprache;

namespace d60.Cirqus.Identity
{
    public class KeyParser
    {
        public KeyParser(char separatorCharacter)
        {
            Parser<char> Separator = Parse.Char(separatorCharacter);

            var Identifier =
                from identifier in Parse.AnyChar.Except(Separator).Many().Text()
                where identifier != ""
                select identifier;

            var PlaceholderBegin = Parse.Char('{');
            var PlaceholderEnd = Parse.Char('}');

            var Placeholder =
                from begin in PlaceholderBegin
                from property in Parse.AnyChar.Except(PlaceholderEnd).Many().Text()
                from end in PlaceholderEnd
                select (KeyFormat.Term)new KeyFormat.Placeholder(property);

            var GuidKeyword =
                from term in Identifier
                where term == "guid"
                select (KeyFormat.Term)new KeyFormat.GuidKeyword();

            var AnyKeyword =
                from term in Identifier
                where term == "*"
                select (KeyFormat.Term)new KeyFormat.Placeholder("");

            var LiteralText =
                from text in Identifier
                select (KeyFormat.Term)new KeyFormat.LiteralText(text);

            var Term =
                from term in Placeholder.XOr(GuidKeyword).XOr(AnyKeyword).XOr(LiteralText)
                select term;

            var EmptySpecification =
                Parse.Return(new KeyFormat(new KeyFormat.GuidKeyword()));

            KeySpecification =
                EmptySpecification.XOr(
                    from head in Term
                    from tail in Separator.Then(x => Term).Many()
                    select new KeyFormat(Cons(head, tail)));
        }

        public readonly Parser<KeyFormat> KeySpecification;

        static IEnumerable<T> Cons<T>(T head, IEnumerable<T> rest)
        {
            yield return head;
            foreach (var item in rest)
                yield return item;
        }
    }
}