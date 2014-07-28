using System.Text.RegularExpressions;
using d60.EventSorcerer.MongoDb.Events;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.MongoDb
{
    [TestFixture]
    public class TestMongoDbSerializer
    {
        [Test]
        public void VerifyPropertyRegexes()
        {
            VerifyMatch("\"_t\":", MongoDbSerializer.BsonTypePropertyRegex);
            VerifyMatch("\"_t\" :", MongoDbSerializer.BsonTypePropertyRegex);
            VerifyMatch("\"_t\"  :", MongoDbSerializer.BsonTypePropertyRegex);
            VerifyMatch("\"_t\"\t:", MongoDbSerializer.BsonTypePropertyRegex);
            VerifyMatch("\"_t\"}", MongoDbSerializer.BsonTypePropertyRegex, shouldMatch: false);
            VerifyMatch("\"_t\" }", MongoDbSerializer.BsonTypePropertyRegex, shouldMatch: false);
            VerifyMatch("\"_t\"\t}", MongoDbSerializer.BsonTypePropertyRegex, shouldMatch: false);

            VerifyMatch("\"$type\":", MongoDbSerializer.JsonDotNetTypePropertyRegex);
            VerifyMatch("\"$type\" :", MongoDbSerializer.JsonDotNetTypePropertyRegex);
            VerifyMatch("\"$type\"  :", MongoDbSerializer.JsonDotNetTypePropertyRegex);
            VerifyMatch("\"$type\"\t:", MongoDbSerializer.JsonDotNetTypePropertyRegex);
            VerifyMatch("\"$type\"}", MongoDbSerializer.JsonDotNetTypePropertyRegex, shouldMatch: false);
            VerifyMatch("\"$type\" }", MongoDbSerializer.JsonDotNetTypePropertyRegex, shouldMatch: false);
            VerifyMatch("\"$type\"\t}", MongoDbSerializer.JsonDotNetTypePropertyRegex, shouldMatch: false);
        }

        static void VerifyMatch(string input, string regex, bool shouldMatch = true)
        {
            if (shouldMatch)
            {
                Assert.That(Regex.IsMatch(input, regex), "Expected '{0}' to be matched by '{1}'", input, regex);
            }
            else
            {
                Assert.That(!Regex.IsMatch(input, regex), "Did NOT expect '{0}' to be matched by '{1}'", input, regex);
            }
        }

    }
}