// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Utils;
using Bicep.LanguageServer.Handlers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Bicep.LangServer.UnitTests.Handlers
{
    [TestClass]
    public class BicepDisableLinterRuleHandlerTests
    {
        [NotNull]
        public TestContext? TestContext { get; set; }

        private static readonly MockRepository Repository = new(MockBehavior.Strict);
        private static readonly ISerializer Serializer = Repository.Create<ISerializer>().Object;
        private BicepDisableLinterRuleHandler BicepDisableLinterRuleHandler = new(Serializer);

        [TestMethod]
        public void DisableLinterRule_WithInvalidBicepConfig_ShouldThrow()
        {

            string bicepConfig = @"{
              ""analyzers"": {
                ""core"": {
                  ""verbose"": false,
                  ""enabled"": true,
                  ""rules"": {
                    ""no-unused-params"": {
                      ""level"": ""warning""
            }";

            Action disableLinterRule = () => BicepDisableLinterRuleHandler.DisableLinterRule(bicepConfig, "no-unused-params");

            disableLinterRule.Should().Throw<Exception>().WithMessage("File bicepconfig.json already exists and is invalid. If overwriting the file is intended, delete it manually and retry disable linter rule lightBulb option again");
        }

        [TestMethod]
        public void DisableLinterRule_WithRuleEnabledInBicepConfig_ShouldTurnOffRule()
        {

            string bicepConfigFileContents = @"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
          ""level"": ""warning""
        }
      }
    }
  }
}";

            string actual = BicepDisableLinterRuleHandler.DisableLinterRule(bicepConfigFileContents, "no-unused-params");

            actual.Should().BeEquivalentToIgnoringNewlines(@"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
          ""level"": ""off""
        }
      }
    }
  }
}");
        }

        [TestMethod]
        public void DisableLinterRule_WithRuleDisabledInBicepConfig_DoesNothing()
        {

            string bicepConfigFileContents = @"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
          ""level"": ""off""
        }
      }
    }
  }
}";

            string actual = BicepDisableLinterRuleHandler.DisableLinterRule(bicepConfigFileContents, "no-unused-params");

            actual.Should().BeEquivalentToIgnoringNewlines(@"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
          ""level"": ""off""
        }
      }
    }
  }
}");
        }

        [TestMethod]
        public void DisableLinterRule_WithNoRuleInBicepConfig_ShouldAddAnEntryInBicepConfig()
        {

            string bicepConfigFileContents = @"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
      }
    }
  }
}";

            string actual = BicepDisableLinterRuleHandler.DisableLinterRule(bicepConfigFileContents, "no-unused-params");

            actual.Should().BeEquivalentToIgnoringNewlines(@"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
          ""level"": ""off""
        }
      }
    }
  }
}");
        }

        [TestMethod]
        public void DisableLinterRule_WithNoLevelFieldInRule_ShouldAddAnEntryInBicepConfig()
        {

            string bicepConfigFileContents = @"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
        }
      }
    }
  }
}";

            string actual = BicepDisableLinterRuleHandler.DisableLinterRule(bicepConfigFileContents, "no-unused-params");

            actual.Should().BeEquivalentToIgnoringNewlines(@"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
          ""level"": ""off""
        }
      }
    }
  }
}");
        }

        [TestMethod]
        public void GetBicepConfigSettingsFilePathAndContents_WithInvalidBicepConfigSettingsFilePath_ShouldCreateBicepConfigFileUsingDefaultSettings()
        {
            DocumentUri documentUri = DocumentUri.FromFileSystemPath("/path/to/main.bicep");
            (string bicepConfigSettingsFilePath, string bicepConfigContents) = BicepDisableLinterRuleHandler.GetBicepConfigSettingsFilePathAndContents(documentUri, "no-unused-params", string.Empty);

            bicepConfigSettingsFilePath.Should().Be(@"\path\to\bicepconfig.json");
            bicepConfigContents.Should().BeEquivalentToIgnoringNewlines(@"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-hardcoded-env-urls"": {
          ""level"": ""warning"",
          ""disallowedhosts"": [
            ""gallery.azure.com"",
            ""management.core.windows.net"",
            ""management.azure.com"",
            ""database.windows.net"",
            ""core.windows.net"",
            ""login.microsoftonline.com"",
            ""graph.windows.net"",
            ""trafficmanager.net"",
            ""datalake.azure.net"",
            ""azuredatalakestore.net"",
            ""azuredatalakeanalytics.net"",
            ""vault.azure.net"",
            ""api.loganalytics.io"",
            ""asazure.windows.net"",
            ""region.asazure.windows.net"",
            ""batch.core.windows.net""
          ],
          ""excludedhosts"": [
            ""schema.management.azure.com""
          ]
        },
        ""no-unused-params"": {
          ""level"": ""off""
        }
      }
    }
  }
}");
        }

        [TestMethod]
        public void GetBicepConfigSettingsFilePathAndContents_WithNonExistentBicepConfigSettingsFile_ShouldCreateBicepConfigFileUsingDefaultSettings()
        {
            DocumentUri documentUri = DocumentUri.FromFileSystemPath("/path/to/main.bicep");
            (string bicepConfigSettingsFilePath, string bicepConfigContents) = BicepDisableLinterRuleHandler.GetBicepConfigSettingsFilePathAndContents(documentUri, "no-unused-params", @"\path\to\bicepconfig.json");

            bicepConfigSettingsFilePath.Should().Be(@"\path\to\bicepconfig.json");
            bicepConfigContents.Should().BeEquivalentToIgnoringNewlines(@"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-hardcoded-env-urls"": {
          ""level"": ""warning"",
          ""disallowedhosts"": [
            ""gallery.azure.com"",
            ""management.core.windows.net"",
            ""management.azure.com"",
            ""database.windows.net"",
            ""core.windows.net"",
            ""login.microsoftonline.com"",
            ""graph.windows.net"",
            ""trafficmanager.net"",
            ""datalake.azure.net"",
            ""azuredatalakestore.net"",
            ""azuredatalakeanalytics.net"",
            ""vault.azure.net"",
            ""api.loganalytics.io"",
            ""asazure.windows.net"",
            ""region.asazure.windows.net"",
            ""batch.core.windows.net""
          ],
          ""excludedhosts"": [
            ""schema.management.azure.com""
          ]
        },
        ""no-unused-params"": {
          ""level"": ""off""
        }
      }
    }
  }
}");
        }

        [TestMethod]
        public void GetBicepConfigSettingsFilePathAndContents_WithValidBicepConfigSettingsFile_ShouldReturnUpdatedBicepConfigFile()
        {
            string testOutputPath = Path.Combine(TestContext.ResultsDirectory, Guid.NewGuid().ToString());
            string bicepConfigFileContents = @"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
          ""level"": ""warning""
        }
      }
    }
  }
}";
            string bicepConfigFilePath = FileHelper.SaveResultFile(TestContext, "bicepconfig.json", bicepConfigFileContents, testOutputPath, Encoding.UTF8);

            DocumentUri documentUri = DocumentUri.FromFileSystemPath("/path/to/main.bicep");
            (string bicepConfigSettingsFilePath, string bicepConfigContents) = BicepDisableLinterRuleHandler.GetBicepConfigSettingsFilePathAndContents(documentUri, "no-unused-params", bicepConfigFilePath);

            bicepConfigSettingsFilePath.Should().Be(bicepConfigFilePath);
            bicepConfigContents.Should().BeEquivalentToIgnoringNewlines(@"{
  ""analyzers"": {
    ""core"": {
      ""verbose"": false,
      ""enabled"": true,
      ""rules"": {
        ""no-unused-params"": {
          ""level"": ""off""
        }
      }
    }
  }
}");
        }

    }
}