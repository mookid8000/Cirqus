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

            return $"{databaseName}_agent{number}";
        }

        public static string GetDatabaseName(string connectionString)
        {
            var relevantSetting = connectionString
                .Split(';')
                .Select(pair => pair.Trim())
                .Select(kvp =>
                {
                    var tokens = kvp.Split('=');

                    return new
                    {
                        Key = tokens.First().Trim(),
                        Value = tokens.LastOrDefault()?.Trim()
                    };
                })
                .FirstOrDefault(a => string.Equals(a.Key, "database", StringComparison.InvariantCultureIgnoreCase));

            return relevantSetting?.Value;
        }
    }
}