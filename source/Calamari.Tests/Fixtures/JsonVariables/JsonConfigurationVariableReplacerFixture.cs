﻿using System;
using System.IO;
using Calamari.Integration.FileSystem;
using Calamari.Integration.JsonVariables;
using Calamari.Tests.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Octostache;

namespace Calamari.Tests.Fixtures.JsonVariables
{
    [TestFixture]
    public class JsonConfigurationVariableReplacerFixture : CalamariFixture
    {
        JsonConfigurationVariableReplacer configurationVariableReplacer;

        [SetUp]
        public void SetUp()
        {
            configurationVariableReplacer = new JsonConfigurationVariableReplacer();
        }

        [Test]
        public void ShouldReplaceInSimpleFile()
        {
            const string expected = 
                @"{" +
                "  \"MyMessage\": \"Hello world\"," +
                "  \"EmailSettings\": {" +
                "    \"SmtpPort\": \"23\"," +
                "    \"SmtpHost\": \"localhost\"," +
                "    \"DefaultRecipients\": {" +
                "      \"To\": \"paul@octopus.com\"," +
                "      \"Cc\": \"henrik@octopus.com\"" +
                "    }" +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("MyMessage", "Hello world");
            variables.Set("EmailSettings:SmtpHost", "localhost");
            variables.Set("EmailSettings:SmtpPort", "23");
            variables.Set("EmailSettings:DefaultRecipients:To", "paul@octopus.com");
            variables.Set("EmailSettings:DefaultRecipients:Cc", "henrik@octopus.com");

            var replaced = Replace(variables, existingFile: "appsettings.simple.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldIgnoreOctopusPrefix()
        {
            const string expected =
                @"{" +
                "  \"MyMessage\": \"Hello world!\"," +
                "  \"IThinkOctopusIsGreat\": \"Yes, I do!\"" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("MyMessage", "Hello world!");
            variables.Set("IThinkOctopusIsGreat", "Yes, I do!");
            variables.Set("OctopusRocks", "This is ignored");
            variables.Set("Octopus.Rocks", "So is this");

            var replaced = Replace(variables, existingFile: "appsettings.ignore-octopus.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldKeepExistingValues()
        {
            const string expected =
                @"{" +
                "  \"MyMessage\": \"Hello world!\"," +
                "  \"EmailSettings\": {" +
                "    \"SmtpPort\": \"24\"," +
                "    \"SmtpHost\": \"localhost\"," +
                "    \"DefaultRecipients\": {" +
                "      \"To\": \"paul@octopus.com\"," +
                "      \"Cc\": \"damo@octopus.com\"" +
                "    }" +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("MyMessage", "Hello world!");
            variables.Set("EmailSettings:SmtpPort", "24");
            variables.Set("EmailSettings:DefaultRecipients:Cc", "damo@octopus.com");

            var replaced = Replace(variables, existingFile: "appsettings.existing-expected.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldMatchAndReplaceIgnoringCase()
        {
            const string expected =
                @"{" +
                "  \"MyMessage\": \"Hello world\"," +
                "  \"EmailSettings\": {" +
                "    \"SmtpPort\": \"23\"," +
                "    \"SmtpHost\": \"localhost\"," +
                "    \"DefaultRecipients\": {" +
                "      \"To\": \"paul@octopus.com\"," +
                "      \"Cc\": \"henrik@octopus.com\"" +
                "    }" +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("mymessage", "Hello world");
            variables.Set("EmailSettings:SmtpHost", "localhost");
            variables.Set("EmailSettings:SmtpPort", "23");
            variables.Set("EmailSettings:Defaultrecipients:To", "paul@octopus.com");
            variables.Set("EmailSettings:defaultRecipients:Cc", "henrik@octopus.com");

            var replaced = Replace(variables, existingFile: "appsettings.simple.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldReplaceWithColonInName()
        {
            const string expected =
                @"{" +
                "  \"commandName\": \"web\"," +
                "  \"environmentVariables\": {" +
                "    \"Hosting:Environment\": \"Production\"," +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("EnvironmentVariables:Hosting:Environment", "Production");

            var replaced = Replace(variables, existingFile: "appsettings.colon-in-name.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldReplaceWholeObject()
        {
            const string expected =
                @"{" +
                "  \"MyMessage\": \"Hello world\"," +
                "  \"EmailSettings\": {" +
                "    \"SmtpPort\": \"23\"," +
                "    \"SmtpHost\": \"localhost\"," +
                "    \"DefaultRecipients\": {" +
                "      \"To\": \"rob@octopus.com\"," +
                "      \"Cc\": \"henrik@octopus.com\"" +
                "    }" +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("EmailSettings:DefaultRecipients", @"{""To"": ""rob@octopus.com"", ""Cc"": ""henrik@octopus.com""}");

            var replaced = Replace(variables, existingFile: "appsettings.simple.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldReplaceElementInArray()
        {
            const string expected =
                @"{" +
                "  \"MyMessage\": \"Hello world\"," +
                "  \"EmailSettings\": {" +
                "    \"SmtpPort\": 23," +
                "    \"UseProxy\": false," +
                "    \"SmtpHost\": \"localhost\"," +
                "    \"DefaultRecipients\": [" +
                "      \"paul@octopus.com\"," +
                "      \"henrik@octopus.com\"" +
                "    ]" +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("EmailSettings:DefaultRecipients:1", "henrik@octopus.com");

            var replaced = Replace(variables, existingFile: "appsettings.array.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldReplaceEntireArray()
        {
            const string expected =
                @"{" +
                "  \"MyMessage\": \"Hello world\"," +
                "  \"EmailSettings\": {" +
                "    \"SmtpPort\": 23," +
                "    \"UseProxy\": false," +
                "    \"SmtpHost\": \"localhost\"," +
                "    \"DefaultRecipients\": [" +
                "      \"mike@octopus.com\"," +
                "      \"henrik@octopus.com\"" +
                "    ]" +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("EmailSettings:DefaultRecipients", @"[""mike@octopus.com"", ""henrik@octopus.com""]");

            var replaced = Replace(variables, existingFile: "appsettings.array.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldReplaceNumber()
        {
            const string expected =
                @"{" +
                "  \"MyMessage\": \"Hello world\"," +
                "  \"EmailSettings\": {" +
                "    \"SmtpPort\": 8023," +
                "    \"UseProxy\": false," +
                "    \"SmtpHost\": \"localhost\"," +
                "    \"DefaultRecipients\": [" +
                "      \"paul@octopus.com\"," +
                "      \"mike@octopus.com\"" +
                "    ]" +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("EmailSettings:SmtpPort", "8023");

            var replaced = Replace(variables, existingFile: "appsettings.array.json");
            AssertJsonEquivalent(replaced, expected);
        }

        [Test]
        public void ShouldReplaceBoolean()
        {
            const string expected =
                @"{" +
                "  \"MyMessage\": \"Hello world\"," +
                "  \"EmailSettings\": {" +
                "    \"SmtpPort\": 23," +
                "    \"UseProxy\": true," +
                "    \"SmtpHost\": \"localhost\"," +
                "    \"DefaultRecipients\": [" +
                "      \"paul@octopus.com\"," +
                "      \"mike@octopus.com\"" +
                "    ]" +
                "  }" +
                "}";

            var variables = new VariableDictionary();
            variables.Set("EmailSettings:UseProxy", "true");

            var replaced = Replace(variables, existingFile: "appsettings.array.json");
            AssertJsonEquivalent(replaced, expected);
        }

        string Replace(VariableDictionary variables, string existingFile = null)
        {
            var temp = Path.GetTempFileName();
            if (existingFile != null)
                File.Copy(GetFixtureResouce("Samples", existingFile), temp, true);

            using (new TemporaryFile(temp))
            {
                configurationVariableReplacer.ModifyJsonFile(temp, variables);
                return File.ReadAllText(temp);
            }
        }

        void AssertJsonEquivalent(string replaced, string expected)
        {
            var replacedJson = JToken.Parse(replaced);
            var expectedJson = JToken.Parse(expected);

            if (!JToken.DeepEquals(replacedJson, expectedJson))
            {
                Console.WriteLine("Expected:");
                Console.WriteLine(expectedJson.ToString(Formatting.Indented));

                Console.WriteLine("Replaced:");
                Console.WriteLine(replacedJson.ToString(Formatting.Indented));

                Assert.Fail("Replaced JSON did not match expected JSON");                
            }
        } 
    }
}
