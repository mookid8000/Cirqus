using System.Linq;
using d60.Cirqus.Identity;
using NUnit.Framework;
using Shouldly;
using Sprache;

namespace d60.Cirqus.Tests.Identity
{
    public class IdParserTests
    {
        [Test]
        public void ParseEmpty()
        {
            var result = new KeyParser('-').KeySpecification.Parse("");

            result.Terms.Single().ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParseGuidKeyword()
        {
            new KeyParser('-').KeySpecification.Parse("guid")
                .Terms.Single().ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParsePlaceholder()
        {
            new KeyParser('-').KeySpecification.Parse("{hej}")
                .Terms.Single().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("hej");
        }

        [Test]
        public void ParseEmptyPlaceholder()
        {
            new KeyParser('-').KeySpecification.Parse("{}")
                .Terms.Single().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("");
        }

        [Test]
        public void ParseAsterisk()
        {
            new KeyParser('-').KeySpecification.Parse("*")
                .Terms.Single().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("");
        }


        [Test]
        public void ParseLiteralText()
        {
            new KeyParser('-').KeySpecification.Parse("hallo")
                .Terms.Single().ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
        }

        [Test]
        public void ParseLiteralTexts()
        {
            var terms = new KeyParser('-').KeySpecification.Parse("hallo-halli").Terms;
            terms.Count.ShouldBe(2);
            terms[0].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
            terms[1].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("halli");
        }

        [Test]
        public void ParseComplexExpression()
        {
            var terms = new KeyParser('-').KeySpecification.Parse("hallo-guid-hvaderder-{props}-guid").Terms;

            terms.Count.ShouldBe(5);
            terms[0].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
            terms[1].ShouldBeOfType<KeyFormat.GuidKeyword>();
            terms[2].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hvaderder");
            terms[3].ShouldBeOfType<KeyFormat.Placeholder>().Property.ShouldBe("props");
            terms[4].ShouldBeOfType<KeyFormat.GuidKeyword>();
        }
    }
}