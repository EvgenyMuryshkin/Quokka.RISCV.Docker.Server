﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Quokka.RISCV.Integration.Tests.Tools
{
    public class TestPath
    {
        public static string SolutionFolder(string start = null)
        {
            if (start == null)
                start = Environment.CurrentDirectory;

            if (!Directory.Exists(start))
                return null;

            if (Directory.EnumerateFiles(start, "*.sln").Any())
                return start;

            return SolutionFolder(Path.GetDirectoryName(start));
        }

        public static string IntegrationTestsFolder => Path.Combine(
            SolutionFolder(Environment.CurrentDirectory),
            "Quokka.RISCV.Integration.Tests");

        public static string IntermediateFolder => Path.Combine(
            IntegrationTestsFolder,
            "Intermediate");

        public static string FirmwareSourceFolder => Path.Combine(
            IntermediateFolder,
            "FirmwareSource");

        public static string FirmwareOutputFolder => Path.Combine(
            IntermediateFolder,
            "FirmwareOutput");

        public static string HardwareSourceFolder => Path.Combine(
            IntermediateFolder,
            "HardwareSource");

        public static string SourcePath => Path.Combine(
            SolutionFolder(),
            "Quokka.RISCV.Integration.Tests",
            "CSharp2CPPTranslatorTests",
            "Source");
    }
}
