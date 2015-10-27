using System;
using System.Collections.Generic;
using Sprache;

namespace d60.Cirqus.Identity
{
    public class KeyFormatParser
    {
        public KeyFormatParser(char separatorCharacter, string defaultUniquenessKind)
        {
            var Separator = Parse.Char(separatorCharacter);

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

            var SGuidKeyword =
                from term in Identifier
                where term == "sguid"
                select (KeyFormat.Term)new KeyFormat.SGuidKeyword();

            var AnyKeyword =
                from term in Identifier
                where term == "*"
                select (KeyFormat.Term)new KeyFormat.Placeholder("");

            var LiteralText =
                from text in Identifier
                select (KeyFormat.Term)new KeyFormat.LiteralText(text);

            var UniquenessTerms = Placeholder.XOr(GuidKeyword).XOr(SGuidKeyword).XOr(AnyKeyword);

            var Term =
                from term in UniquenessTerms.XOr(LiteralText)
                select term;

            var EmptySpecification =
                Parse.Return(new KeyFormat(UniquenessTerms.Parse(defaultUniquenessKind)));

            KeySpecification =
                EmptySpecification.XOr(
                    from head in Term
                    from tail in Separator.Then(x => Term).Many()
                    let terms = ConcatIfAll(
                        Cons(head, tail), 
                        x => x is KeyFormat.LiteralText, 
                        UniquenessTerms.Parse(defaultUniquenessKind))
                    select new KeyFormat(terms));
        }

        public readonly Parser<KeyFormat> KeySpecification;

        static IEnumerable<T> Cons<T>(T head, IEnumerable<T> rest)
        {
            yield return head;
            foreach (var item in rest)
                yield return item;
        }

        static IEnumerable<T> ConcatIfAll<T>(IEnumerable<T> list, Func<T, bool> test, T next)
        {
            var all = true;
            foreach (var item in list)
            {
                all = test(item) && all;
                yield return item;
            }

            if (all)
            {
                yield return next;
            }
        }

    }
}