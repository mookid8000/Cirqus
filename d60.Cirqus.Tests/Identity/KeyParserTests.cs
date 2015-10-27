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
            GetKeyFormat("").Terms.Single().ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParseLiteralTextOnlyAddsDefaultUniquesnessTerm()
        {
            var terms = GetKeyFormat("prefix").Terms;

            terms[0].ShouldBeOfType<KeyFormat.LiteralText>();
            terms[1].ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParseLiteralTextOnlyAddsConfiguredDefaultUniquenessTerm()
        {
            var terms = GetKeyFormat("prefix", defaultUniquenessTerm: "sguid").Terms;

            terms[0].ShouldBeOfType<KeyFormat.LiteralText>();
            terms[1].ShouldBeOfType<KeyFormat.SGuidKeyword>();
        }

        [Test]
        public void ParseGuidKeyword()
        {
            GetKeyFormat("prefix-guid")
                .Terms.Last().ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParseSGuidKeyword()
        {
            GetKeyFormat("prefix-sguid")
                .Terms.Last().ShouldBeOfType<KeyFormat.SGuidKeyword>();
        }

        [Test]
        public void ParsePlaceholder()
        {
            GetKeyFormat("prefix-{hej}")
                .Terms.Last().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("hej");
        }

        [Test]
        public void ParseEmptyPlaceholder()
        {
            GetKeyFormat("prefix-{}")
                .Terms.Last().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("");
        }

        [Test]
        public void ParseAsterisk()
        {
            GetKeyFormat("prefix-*")
                .Terms.Last().ShouldBeOfType<KeyFormat.Placeholder>()
                .Property.ShouldBe("");
        }

        [Test]
        public void ParseLiteralText()
        {
            var terms = GetKeyFormat("hallo").Terms;
            terms[0].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
            terms[1].ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParseLiteralTextWithOtherDefaultUniquenessTerm()
        {
            var terms = GetKeyFormat("hallo", defaultUniquenessTerm: "sguid").Terms;
            terms[0].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
            terms[1].ShouldBeOfType<KeyFormat.SGuidKeyword>();
        }

        [Test]
        public void ParseOnlyLiteralTextsAddsAUniqnessTerm()
        {
            var terms = GetKeyFormat("hallo-halli").Terms;
            terms.Count.ShouldBe(3);
            terms[0].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
            terms[1].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("halli");
            terms[2].ShouldBeOfType<KeyFormat.GuidKeyword>();
        }

        [Test]
        public void ParseComplexExpression()
        {
            var terms = GetKeyFormat("hallo-guid-hvaderder-{props}-sguid").Terms;

            terms.Count.ShouldBe(5);
            terms[0].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hallo");
            terms[1].ShouldBeOfType<KeyFormat.GuidKeyword>();
            terms[2].ShouldBeOfType<KeyFormat.LiteralText>().Text.ShouldBe("hvaderder");
            terms[3].ShouldBeOfType<KeyFormat.Placeholder>().Property.ShouldBe("props");
            terms[4].ShouldBeOfType<KeyFormat.SGuidKeyword>();
        }

        [Test]
        public void FailsWithUnknownUniquenessTerm()
        {
            Should.Throw<ParseException>(() => GetKeyFormat("hallo", defaultUniquenessTerm: "skovshoved"));
        }

        KeyFormat GetKeyFormat(string format, char separatorCharacter = '-', string defaultUniquenessTerm = "guid")
        {
            return new KeyFormatParser(separatorCharacter, defaultUniquenessTerm).Execute(format);
        }
    }
}