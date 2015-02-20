using System;
using EnergyProjects.Domain.Model;
using Shouldly;
using Xunit;

namespace EnergyProjects.Tests.Domain
{
    public class IdTests
    {
        const string guid_pattern = "^([0-9a-fA-F]){8}(-([0-9a-fA-F]){4}){3}-([0-9a-fA-F]){12}$";

        [Fact]
        public void NoFormatYieldsGuid()
        {
            var id = NewId("");
            
            id.ShouldNotBe(Guid.Empty.ToString());
            id.ShouldMatch(guid_pattern);
        }

        [Fact]
        public void KeywordGuidYieldsGuid()
        {
            var id = NewId("guid");

            id.ShouldNotBe(Guid.Empty.ToString());
            id.ShouldMatch(guid_pattern);
        } 

        [Fact]
        public void LiteralTextConstantYieldsTextAndGuid()
        {
            var id = NewId("prefix");
            
            id.ShouldStartWith("prefix/");
            id.ShouldNotBe(Guid.Empty.ToString());
            id.Split('/')[1].ShouldMatch(guid_pattern);
        }

        [Fact]
        public void LiteralTextConstantsYieldsTextsAndGuid()
        {
            var id = NewId("prefix/preprefix");
            
            id.ShouldStartWith("prefix/preprefix");
            id.ShouldNotBe(Guid.Empty.ToString());
            id.Split('/')[2].ShouldMatch(guid_pattern);
        }

        [Fact]
        public void PlaceholdersAreReplacedPositionally()
        {
            var id = NewId("{name}/{town}/{gender}", "asger", "mårslet", "mand");

            id.ShouldBe("asger/mårslet/mand");
        }

        [Fact]
        public void UsesAttributeToObtainFormat()
        {
            var id = (string)Id<GuidKeyedRoot>.New();

            id.ShouldStartWith("hest/");
            id.ShouldNotBe(Guid.Empty.ToString());
            id.Split('/')[1].ShouldMatch(guid_pattern);
        }

        [Fact]
        public void ImplicitlyConvertsFromString()
        {
            Id<GuidKeyedRoot>? test = null;
            Should.NotThrow(() =>
            {
                 test = "hest/21952ee6-d028-433f-8634-94d6473275f0";
            });

            test.ToString().ShouldBe("hest/21952ee6-d028-433f-8634-94d6473275f0");
        }

        [Fact]
        public void FailsWhenIdIsNull()
        {
            Should.Throw<InvalidOperationException>(() =>
            {
                Id<GuidKeyedRoot> test = null;
            });
        }

        [Fact]
        public void FailsWhenIdDoesNotMatchFormat()
        {
            Should.Throw<InvalidOperationException>(() =>
            {
                Id<GuidKeyedRoot> test = "hest/123456";
            });
        }

        [Fact]
        public void AppliesValuesToPlaceholders()
        {
            var id = Id<PlaceholderKeyedRoot>.Parse("hest/asger/2/10000000000");
            var root = new PlaceholderKeyedRoot();
            
            id.Apply(root);

            root.String.ShouldBe("asger");
            root.Int.ShouldBe(2);
            root.Long.ShouldBe(10000000000);
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

        [Key("hest/{String}/{Int}/{Long}")]
        public class PlaceholderKeyedRoot
        {
            public string String { get; set; }
            public int Int { get; set; }
            public long Long { get; set; }
        }
    }
}