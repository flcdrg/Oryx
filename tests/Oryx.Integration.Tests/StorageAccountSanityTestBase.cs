﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Oryx.BuildScriptGenerator.Common;
using Microsoft.Oryx.Integration.Tests;
using Microsoft.Oryx.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Oryx.Integration.Tests
{
    public abstract class StorageAccountSanityTestBase
        : PlatformEndToEndTestsBase, IClassFixture<RepoRootDirTestFixture>
    {
        private readonly string _storageUrl;
        private readonly string _repoRootDir;

        private readonly string[] _debianFlavors = 
        {
            // TODO: PR2 OsTypes.DebianBullseye can be added once bullseye images are uploaded
            OsTypes.DebianBuster, OsTypes.DebianStretch, OsTypes.UbuntuFocalScm, /* OsTypes.DebianBullseye */
        };

        public StorageAccountSanityTestBase(
            string storageUrl,
            ITestOutputHelper output,
            TestTempDirTestFixture testTempDirTestFixture,
            RepoRootDirTestFixture repoRootDirTestFixture)
            : base(output, testTempDirTestFixture)
        {
            _storageUrl = storageUrl;
            _repoRootDir = repoRootDirTestFixture.RepoRootDirPath;
        }

        [Fact]
        public void DotNetCoreContainer_HasExpectedListOfBlobs()
        {
            var platformName = "dotnet";
            AssertExpectedListOfBlobs(platformName, "version", platformName);
        }

        [Fact]
        public void DotNetCoreContainer_HasExpectedDefaultVersion()
        {
            var platformName = "dotnet";
            AssertExpectedDefaultVersion(platformName, platformName);
        }

        [Fact]
        public void GolangCoreContainer_HasExpectedListOfBlobs()
        {
            var platformName = "golang";
            AssertExpectedListOfBlobs(platformName, "version", platformName);
        }

        [Fact]
        public void GolangContainer_HasExpectedDefaultVersion()
        {
            var platformName = "golang";
            AssertExpectedDefaultVersion(platformName, platformName);
        }

        [Fact]
        public void PythonContainer_HasExpectedListOfBlobs()
        {
            var platformName = "python";
            AssertExpectedListOfBlobs(platformName, "version", platformName);
        }

        [Fact]
        public void PythonContainer_HasExpectedDefaultVersion()
        {
            var platformName = "python";
            AssertExpectedDefaultVersion(platformName, platformName);
        }

        [Fact]
        public void NodeJSContainer_HasExpectedListOfBlobs()
        {
            // Arrange & Act
            var platformName = "nodejs";
            AssertExpectedListOfBlobs(platformName, "version", platformName);
        }

        [Fact]
        public void NodeJSContainer_HasExpectedDefaultVersion()
        {
            var platformName = "nodejs";
            AssertExpectedDefaultVersion(platformName, platformName);
        }
        
        [Fact]
        public void PhpComposerCoreContainer_HasExpectedListOfBlobs()
        {
            var platformName = "php-composer";
            AssertExpectedListOfBlobs(platformName, "version", "php", "composer");
        }

        [Fact]
        public void PhpContainer_HasExpectedListOfBlobs()
        {
            var platformName = "php";
            AssertExpectedListOfBlobs(platformName, "version", platformName);
        }

        [Fact]
        public void PhpComposerContainer_HasExpectedDefaultVersion()
        {
            var platformName = "php-composer";
            AssertExpectedDefaultVersion(platformName, "php", "composer");
        }

        [Fact]
        public void RubyContainer_HasExpectedListOfBlobs()
        {
            var platformName = "ruby";
            AssertExpectedListOfBlobs(platformName, "version", platformName);
        }

        [Fact]
        public void RubyContainer_HasExpectedDefaultVersion()
        {
            var platformName = "ruby";
            AssertExpectedDefaultVersion(platformName, platformName);
        }

        [Fact]
        public void JavaContainer_HasExpectedListOfBlobs()
        {
            var platformName = "java";
            AssertExpectedListOfBlobs(platformName, "version", platformName);
        }

        [Fact]
        public void JavaContainer_HasExpectedDefaultVersion()
        {
            var platformName = "java";
            AssertExpectedDefaultVersion(platformName, platformName);

        }

        [Fact]
        public void MavenContainer_HasExpectedListOfBlobs()
        {
            AssertExpectedListOfBlobs("maven", "version", "java", "maven");
        }

        [Fact]
        public void MavenContainer_HasExpectedDefaultVersion()
        {
            AssertExpectedDefaultVersion("maven", "java", "maven");
        }

        private void AssertExpectedDefaultVersion(string platformName, params string[] expectedPlatformPath)
        {
            foreach (var debianFlavor in _debianFlavors)
            {
                // Arrange & Act
                var actualVersion = GetDefaultVersionFromContainer(debianFlavor, platformName);
                var expectedVersion = GetDefaultVersion(debianFlavor, expectedPlatformPath);

                // Assert
                Assert.Equal(expectedVersion, actualVersion);
            }
        }

        private void AssertExpectedListOfBlobs(string platformName, string metadataElementName, params string[] expectedPlatformPath)
        {
            foreach (var debianFlavor in _debianFlavors)
            {
                // Arrange & Act
                var actualVersions = GetVersionsFromContainer(debianFlavor, platformName, metadataElementName);
                var expectedVersions = GetListOfVersionsToBuild(debianFlavor, expectedPlatformPath);

                // Assert
                foreach (var expectedVersion in expectedVersions)
                {
                    Assert.Contains(expectedVersion, actualVersions);
                }
            }
        }

        private XDocument GetMetadata(string platformName)
        {
            var url = string.Format(SdkStorageConstants.ContainerMetadataUrlFormat, _storageUrl, platformName);
            var blobList = _httpClient.GetStringAsync(url).Result;
            return XDocument.Parse(blobList);
        }

        private List<string> GetVersionsFromContainer(string debianFlavor, string platformName, string metadataElementName)
        {
            // TODO: PR2 configure this to account for the different debian flavors once the Version metadata has
            // been generated for each package
            var xdoc = GetMetadata(platformName);
            var supportedVersions = new List<string>();
            foreach (var blobElement in xdoc.XPathSelectElements($"//Blobs/Blob"))
            {
                var childElements = blobElement.Elements();
                if (debianFlavor == OsTypes.DebianStretch)
                {
                    var versionElement = childElements
                        .Where(e => string.Equals("Metadata", e.Name.LocalName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault()?.Elements()
                        .Where(e => string.Equals(metadataElementName, e.Name.LocalName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    if (versionElement != null)
                    {
                        supportedVersions.Add(versionElement.Value);
                    }
                }
                else
                {
                    var fileName = childElements
                        .Where(e => string.Equals("Name", e.Name.LocalName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    if (fileName != null)
                    {
                        var patternText = $"{platformName}-{debianFlavor}-(?<version>.*?).tar.gz";
                        Regex expression = new Regex(patternText);
                        Match match = expression.Match(fileName.Value);
                        if (match.Success)
                        {
                            var result = match.Groups["version"].Value;
                            supportedVersions.Add(result);
                        }
                    }
                }
            }

            return supportedVersions;
        }

        private string GetDefaultVersionFromContainer(string debianFlavor, string platformName)
        {
            // TODO: PR2 replace this with the defaultVersion.{debianFlavor}.txt once we actually have the blobs in the
            // storage account
            var defaultVersionContent = _httpClient
                .GetStringAsync($"{_storageUrl}/{platformName}/{SdkStorageConstants.DefaultVersionFileName}")
                .Result;

            string defaultVersion = null;
            using (var stringReader = new StringReader(defaultVersionContent))
            {
                string line;
                while ((line = stringReader.ReadLine()) != null)
                {
                    // Ignore any comments in the file
                    if (!line.StartsWith("#") || !line.StartsWith("//"))
                    {
                        defaultVersion = line.Trim();
                        break;
                    }
                }
            }
            return defaultVersion;
        }

        private List<string> GetListOfVersionsToBuild(string debianFlavor, params string[] platformPath)
        {
            var platformSubPath = Path.Combine(platformPath);
            var versionFile = Path.Combine(
                _repoRootDir,
                "platforms",
                platformSubPath,
                "versions",
                debianFlavor,
                SdkStorageConstants.VersionsToBuildFileName);
            if (!File.Exists(versionFile))
            {
                throw new InvalidOperationException($"Could not find file '{versionFile}'");
            }

            var versions = new List<string>();
            using (var streamReader = new StreamReader(versionFile))
            {
                string line = null;
                while ((line = streamReader.ReadLine()) != null)
                {
                    // Remove extraneous whitespace
                    line = line.Trim();

                    // ignore comments or empty lines
                    if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    var parts = line.Split(",");
                    versions.Add(parts[0].Trim());
                }
            }

            return versions;
        }

        private string GetDefaultVersion(string debianFlavor, params string[] platformPath)
        {
            var platformSubPath = Path.Combine(platformPath);
            var file = Path.Combine(
                _repoRootDir,
                "platforms",
                platformSubPath,
                "versions",
                debianFlavor,
                SdkStorageConstants.DefaultVersionFileName);
            if (!File.Exists(file))
            {
                throw new InvalidOperationException($"Could not file default version file '{file}'.");
            }

            string defaultVersion = null;
            using (var streamReader = new StreamReader(file))
            {
                string line = null;
                while ((line = streamReader.ReadLine()) != null)
                {
                    // Remove extraneous whitespace
                    line = line.Trim();

                    // ignore comments or empty lines
                    if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    defaultVersion = line.Trim();
                }
            }

            if (string.IsNullOrEmpty(defaultVersion))
            {
                throw new InvalidOperationException("Default version cannot be empty");
            }

            return defaultVersion;
        }
    }
}
