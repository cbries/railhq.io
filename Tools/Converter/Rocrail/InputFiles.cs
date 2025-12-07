// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.IO;
using System.Linq;

namespace Converter.Rocrail;

public class InputFiles
{
    public static InputFiles Instance(string sourceDirectory, out string errorMessage)
    {
        var instance = new InputFiles();
        var r = instance.Query(sourceDirectory, out errorMessage);
        //if (!r) ;
        return instance;
    }

    public string OccPath { get; private set; }
    public string PlanPath { get; private set; }

    public static string FindXmlFile(string[] xmlFiles, string targetFileName)
    {
        return xmlFiles.FirstOrDefault(file => Path.GetFileName(file).Equals(targetFileName, StringComparison.OrdinalIgnoreCase));
    }

    public bool Query(string sourceDirectory, out string errorMessage)
    {
        errorMessage = string.Empty;

        var xmlFiles = Directory.GetFiles(sourceDirectory, "*.xml", SearchOption.TopDirectoryOnly);
        if (xmlFiles.Length == 0)
        {
            errorMessage = $"no input files in '{sourceDirectory}'";
            return false;
        }

        OccPath = FindXmlFile(xmlFiles, "occ.xml");
        PlanPath = FindXmlFile(xmlFiles, "plan.xml");

        return true;
    }
}