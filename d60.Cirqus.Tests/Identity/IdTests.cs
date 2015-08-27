using System;
using d60.Cirqus.Identity;
using NUnit.Framework;
using Shouldly;
using Sprache;

namespace d60.Cirqus.Tests.Identity
{
    public class IdTests
    {
        const string guid_pattern = "^([0-9a-fA-F]){8}(-([0-9a-fA-F]){4}){3}-([0-9a-fA-F]){12}$";

        [Test]
        public void NoFormatYieldsGuid()
        {
            var id = NewId("");
            
            id.ShouldNotBe(Guid.Empty.ToString());
            id.ShouldMatch(guid_pattern);
        }

        [Test]
        public void KeywordGuidYieldsGuid()
        {
            var id = NewId("guid");

            id.ShouldNotBe(Guid.Empty.ToString());
            id.ShouldMatch(guid_pattern);
        }

        [Test]
        public void LiteralTextConstantYieldsTextAndGuid()
        {
            var id = NewId("prefix");
            
            id.ShouldStartWith("prefix/");
            id.ShouldNotBe(Guid.Empty.ToString());
            id.Split('/')[1].ShouldMatch(guid_pattern);
        }

        [Test]
        public void LiteralTextConstantsYieldsTextsAndGuid()
        {
            var id = NewId("prefix/preprefix");
            
            id.ShouldStartWith("prefix/preprefix");
            id.ShouldNotBe(Guid.Empty.ToString());
            id.Split('/')[2].ShouldMatch(guid_pattern);
        }

        [Test]
        public void PlaceholdersAreReplacedPositionally()
        {
            var id = NewId("{name}/{town}/{gender}", "asger", "mårslet", "mand");

            id.ShouldBe("asger/mårslet/mand");
        }

        [Test]
        public void FailsOnMissingPlaceholderValues()
        {
            Should.Throw<InvalidOperationException>(() => NewId("{name}/{town}/{gender}", "asger", "mårslet"))
                .Message.ShouldBe("You did not supply a value for the placeholder 'gender' at position 2");
        }

        [Test]
        public void WorksWithEmptyPlaceholders()
        {
            var id = NewId("*/{}", "mårslet", "mand");

            id.ShouldBe("mårslet/mand");
        }

        [Test]
        public void FailsOnMissingPlaceholderValuesForEmptyPlaceholders()
        {
            Should.Throw<InvalidOperationException>(() => NewId("*/{}", "asger"))
                .Message.ShouldBe("You did not supply a value for the *-placeholder or {}-placeholder at position 1");
        }

        [Test]
        public void UsesAttributeToObtainFormat()
        {
            var id = (string)Id<GuidKeyedRoot>.New();

            id.ShouldStartWith("hest/");
            id.ShouldNotBe(Guid.Empty.ToString());
            id.Split('/')[1].ShouldMatch(guid_pattern);
        }

        [Test]
        public void FailsWhenIdIsNull()
        {
            Should.Throw<InvalidOperationException>(() =>
            {
                var test = (Id<GuidKeyedRoot>)null;
            });
        }

        [Test]
        public void CanConvertNullToNullableIds()
        {
            Should.NotThrow(() =>
            {
                Id<GuidKeyedRoot>? test = null;
            });
        }

        [Test]
        public void FailsWhenIdDoesNotMatchFormatForGuid()
        {
            Should.Throw<InvalidOperationException>(() =>
            {
                Id<GuidKeyedRoot>.Parse("hest/123456");
            });
        }

        [Test]
        public void FailsWhenIdDoesNotMatchFormatForLiteralText()
        {
            Should.Throw<InvalidOperationException>(() =>
            {
                Id<GuidKeyedRoot>.Parse("ko/21952ee6-d028-433f-8634-94d6473275f0");
            });
        }

        [Test]
        public void AppliesValuesToPlaceholders()
        {
            var id = Id<PlaceholderKeyedRoot>.Parse("hest/asger/2/10000000000/anything");
            var root = new PlaceholderKeyedRoot();
            
            id.Apply(root);

            root.String.ShouldBe("asger");
            root.Int.ShouldBe(2);
            root.Long.ShouldBe(10000000000);
        }

        [Test]
        public void IdentityEquality()
        {
            var id1 = Id<GuidKeyedRoot>.Parse("hest/21952ee6-d028-433f-8634-94d6473275f0");
            var id2 = Id<GuidKeyedRoot>.Parse("hest/21952ee6-d028-433f-8634-94d6473275f0");
            id1.ShouldBe(id2);
        }

        static string NewId(string input, params object[] args)
        {
            var format = KeyFormat.FromAttribute(new KeyAttribute(input));
            return format.Compile<object>(args);
        }

        [Key("hest/guid")]
        public class GuidKeyedRoot
        {
            
        }

        [Key("hest/{String}/{Int}/{Long}/*")]
        public class PlaceholderKeyedRoot
        {
            public string String { get; set; }
            public int Int { get; set; }
            public long Long { get; set; }
        }
    }
}