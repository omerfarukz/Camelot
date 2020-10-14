﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Camelot.Services.Abstractions;
using Camelot.Services.Abstractions.Models;

namespace Camelot.Services.Linux
{
    public class LinuxSoftwareService : ISoftwareService
    {
        public async Task<IEnumerable<SoftwareModel>> GetAllInstalledSoftwares()
        {
            var installedSoftwares = new List<SoftwareModel>();

            foreach (var desktopFilePath in Directory.GetFiles("/usr/share/applications/", "*.desktop"))
            {
                await using var desktopFile = File.OpenRead(desktopFilePath);

                var desktopEntry = new IniFileReader().ReadFile(desktopFile);
                if (desktopEntry["Desktop Entry:Type"] != "Application")
                {
                    continue;
                }

                installedSoftwares.Add(new SoftwareModel
                {
                    DisplayName = desktopEntry["Desktop Entry:Name"],
                    DisplayIcon = desktopEntry["Desktop Entry:Icon"],
                    InstallLocation = desktopEntry["Desktop Entry:Exec"]
                });
            }

            return installedSoftwares;
        }

        private class IniFileReader
        {
            public Dictionary<string, string> ReadFile(Stream stream)
            {
                const string keyDelimiter = ":";

                var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                using var reader = new StreamReader(stream);
                var sectionPrefix = string.Empty;

                while (reader.Peek() != -1)
                {
                    var rawLine = reader.ReadLine();
                    var line = rawLine?.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    switch (line[0])
                    {
                        case ';':
                        case '#':
                        case '/':
                            continue;
                        case '[' when line[^1] == ']':
                            sectionPrefix = line.Substring(1, line.Length - 2) + keyDelimiter;
                            continue;
                    }

                    var separator = line.IndexOf('=');
                    if (separator < 0)
                    {
                        throw new FormatException("Unrecognized line format");
                    }

                    var key = sectionPrefix + line.Substring(0, separator).Trim();
                    var value = line.Substring(separator + 1).Trim();

                    if (value.Length > 1 && value[0] == '"' && value[^1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (data.ContainsKey(key))
                    {
                        throw new FormatException("Duplicated key");
                    }

                    data[key] = value;
                }

                return data;
            }
        }
    }
}