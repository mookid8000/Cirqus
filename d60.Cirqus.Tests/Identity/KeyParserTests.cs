using System.Linq;
using d60.Cirqus.Identity;
using NUnit.Framework;
using Shouldly;
using Sprache;

namespace d60.Cirqus.Tests.Identity
{
    public class KeyParserTests
    {
        [Test]
        public void ParseEmpty()
        {
            var result = new KeyFormatParser('-').KeySpecification.Parse("");

            result.Terms.Single().ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParseGuidKeyword()
        {
            new KeyFormatParser('-').KeySpecification.Parse("guid")
                .Terms.Single().ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParseSGuidKeyword()
        {
            new KeyFormatParser('-').KeySpecification.Parse("sguid")
                .Terms.Single().ShouldBeOfType<KeyFormat.SGuidKeyword>();
        }

        [Test]
        public void ParsePlaceholder()
        {
            new KeyFormatParser('-').KeySpecification.Parse("{hej}")
                .Terms.Single().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("hej");
        }

        [Test]
        public void ParseEmptyPlaceholder()
        {
            new KeyFormatParser('-').KeySpecification.Parse("{}")
                .Terms.Single().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("");
        }

        [Test]
        public void ParseAsterisk()
        {
            new KeyFormatParser('-').KeySpecification.Parse("*")
                .Terms.Single().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("");
        }


        [Test]
        public void ParseLiteralText()
        {
            new KeyFormatParser('-').KeySpecification.Parse("hallo")
                .Terms.Single().ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
        }

        [Test]
        public void ParseLiteralTexts()
        {
            var terms = new KeyFormatParser('-').KeySpecification.Parse("hallo-halli").Terms;
            terms.Count.ShouldBe(2);
            terms[0].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
            terms[1].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("halli");
        }

        [Test]
        public void ParseComplexExpression()
        {
            var terms = new KeyFormatParser('-').KeySpecification.Parse("hallo-guid-hvaderder-{props}-sguid").Terms;

            terms.Count.ShouldBe(5);
            terms[0].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
            terms[1].ShouldBeOfType<KeyFormat.GuidKeyword>();
            terms[2].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hvaderder");
            terms[3].ShouldBeOfType<KeyFormat.Placeholder>().Property.ShouldBe("props");
            terms[4].ShouldBeOfType<KeyFormat.SGuidKeyword>();
        }
    }
}