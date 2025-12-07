// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using Converter.Rocrail;

namespace ConverterCli
{
    internal class Program
    {
        static void Usage()
        {
            Console.WriteLine($"converterCli.exe plan.xml-Directory output-Directory");
        }

        public class ExceptionUsage : Exception
        {

        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 2) throw new ExceptionUsage();

                var p0 = args[0]?.Trim().Trim('"').Trim();
                var p1 = args[1]?.Trim().Trim('"').Trim();

                if (string.IsNullOrEmpty(p0)) throw new ExceptionUsage();
                if (string.IsNullOrEmpty(p1)) throw new ExceptionUsage();

                var importer = new ImportRocrail();
                var res = importer.Execute(p0, p1, out var errorMessage);
                if (!res)
                {
                    Console.WriteLine($"Converter failed: {errorMessage}");
                }
            }
            catch (ExceptionUsage)
            {
                Usage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Converter failed: {ex.Message}");
            }
        }
    }
}
