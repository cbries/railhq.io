// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Program.cs

using System;
using System.IO;

namespace Converter.Rocrail
{
    public class ImportRocrail
    {
        private InputFiles _inputFiles = new();

        public bool ExecuteData(string data, string outputDirectory, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var converter = new XmlConverter();
                converter.RunData(data, outputDirectory);
                return true;

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool Execute(string sourceDirectory, string outputDirectory, out string errorMessage)
        {
            if (string.IsNullOrEmpty(sourceDirectory))
            {
                errorMessage = "source directory not set";
                return false;
            }

            if (string.IsNullOrEmpty(outputDirectory))
            {
                errorMessage = "output directory not set";
                return false;
            }

            try
            {
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }

            var xmlFiles = Directory.GetFiles(sourceDirectory, "*.xml", SearchOption.TopDirectoryOnly);
            if (xmlFiles.Length == 0)
            {
                errorMessage = $"no input files in '{sourceDirectory}'";
                return false;
            }

            var inputFiles = InputFiles.Instance(sourceDirectory, out errorMessage);
            if (!string.IsNullOrEmpty(errorMessage))
                return false;

            errorMessage = string.Empty;

            try
            {
                var converter = new XmlConverter();
                converter.Run(inputFiles.PlanPath, outputDirectory);
                return true;

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
