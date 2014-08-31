using System;
using System.Linq;

namespace d60.Cirqus.Tests
{
    class SqlTestHelperBase
    {
        protected static string PossiblyAppendTeamcityAgentNumber(string databaseName)
        {
            var teamCityAgentNumber = Environment.GetEnvironmentVariable("tcagent");
            int number;

            if (string.IsNullOrWhiteSpace(teamCityAgentNumber) || !int.TryParse(teamCityAgentNumber, out number))
                return databaseName;

            return string.Format("{0}_agent{1}", databaseName, number);
        }

        public static string GetDatabaseName(string connectionString)
        {
            var relevantSetting = connectionString
                .Split(';')
                .Select(kvp =>
                {
                    var tokens = kvp.Split('=');

                    return new
                    {
                        Key = tokens[0],
                        Value = tokens.Length > 0 ? tokens[1] : null
                    };
                })
                .FirstOrDefault(a => string.Equals(a.Key, "database", StringComparison.InvariantCultureIgnoreCase));

            return relevantSetting != null ? relevantSetting.Value : null;
        }
    }
}