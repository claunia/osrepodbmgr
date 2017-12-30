//
//  Author:
//    Natalia Portillo claunia@claunia.com
//
//  Copyright (c) 2017, © Canary Islands Computer Museum
//
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Claunia.PropertyList;
using DiscImageChef.Interop;
using Microsoft.Win32;
using PlatformID = DiscImageChef.Interop.PlatformID;

namespace osrepodbmgr.Core
{
    public class SetSettings
    {
        public string   ClamdHost;
        public bool     ClamdIsLocal;
        public ushort   ClamdPort;
        public AlgoEnum CompressionAlgorithm;
        public string   DatabasePath;
        public string   RepositoryPath;
        public string   TemporaryFolder;
        public string   UnArchiverPath;
        public bool     UseAntivirus;
        public bool     UseClamd;
        public bool     UseVirusTotal;
        public string   VirusTotalKey;
    }

    public static class Settings
    {
        public static SetSettings Current;

        public static void LoadSettings()
        {
            Current         = new SetSettings();
            PlatformID ptId = DetectOS.GetRealPlatformID();

            FileStream   prefsFs = null;
            StreamReader prefsSr = null;

            try
            {
                switch(ptId)
                {
                    case PlatformID.MacOSX:
                    case PlatformID.iOS:
                    {
                        string preferencesPath =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
                                         "Preferences");
                        string preferencesFilePath =
                            Path.Combine(preferencesPath, "com.claunia.museum.osrepodbmgr.plist");

                        if(!File.Exists(preferencesFilePath))
                        {
                            SetDefaultSettings();
                            SaveSettings();
                        }

                        prefsFs                        = new FileStream(preferencesFilePath, FileMode.Open);
                        NSDictionary parsedPreferences = (NSDictionary)BinaryPropertyListParser.Parse(prefsFs);
                        if(parsedPreferences != null)
                        {
                            Current.TemporaryFolder = parsedPreferences.TryGetValue("TemporaryFolder", out NSObject obj)
                                                          ? ((NSString)obj).ToString()
                                                          : Path.GetTempPath();

                            Current.DatabasePath = parsedPreferences.TryGetValue("DatabasePath", out obj)
                                                       ? ((NSString)obj).ToString()
                                                       : Path
                                                          .Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                                                   "osrepodbmgr.db");

                            Current.RepositoryPath = parsedPreferences.TryGetValue("RepositoryPath", out obj)
                                                         ? ((NSString)obj).ToString()
                                                         : Path
                                                            .Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                                                     "osrepo");

                            Current.UnArchiverPath = parsedPreferences.TryGetValue("UnArchiverPath", out obj)
                                                         ? ((NSString)obj).ToString()
                                                         : null;

                            if(parsedPreferences.TryGetValue("CompressionAlgorithm", out obj))
                            {
                                if(!Enum.TryParse(((NSString)obj).ToString(), true, out Current.CompressionAlgorithm))
                                    Current.CompressionAlgorithm = AlgoEnum.GZip;
                            }
                            else Current.CompressionAlgorithm = AlgoEnum.GZip;

                            Current.UseAntivirus = parsedPreferences.TryGetValue("UseAntivirus", out obj) &&
                                                   ((NSNumber)obj).ToBool();

                            Current.UseClamd = parsedPreferences.TryGetValue("UseClamd", out obj) &&
                                               ((NSNumber)obj).ToBool();

                            Current.ClamdHost = parsedPreferences.TryGetValue("ClamdHost", out obj)
                                                    ? ((NSString)obj).ToString()
                                                    : null;

                            if(parsedPreferences.TryGetValue("ClamdPort", out obj))
                                Current.ClamdPort  = (ushort)((NSNumber)obj).ToLong();
                            else Current.ClamdPort = 3310;

                            Current.ClamdIsLocal = parsedPreferences.TryGetValue("ClamdIsLocal", out obj) &&
                                                   ((NSNumber)obj).ToBool();

                            Current.ClamdIsLocal = parsedPreferences.TryGetValue("UseVirusTotal", out obj) &&
                                                   ((NSNumber)obj).ToBool();

                            Current.ClamdHost = parsedPreferences.TryGetValue("VirusTotalKey", out obj)
                                                    ? ((NSString)obj).ToString()
                                                    : null;

                            prefsFs.Close();
                        }
                        else
                        {
                            prefsFs.Close();

                            SetDefaultSettings();
                            SaveSettings();
                        }
                    }
                        break;
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                    case PlatformID.WindowsPhone:
                    {
                        RegistryKey parentKey = Registry
                                               .CurrentUser.OpenSubKey("SOFTWARE")
                                              ?.OpenSubKey("Canary Islands Computer Museum");
                        if(parentKey == null)
                        {
                            SetDefaultSettings();
                            SaveSettings();
                            return;
                        }

                        RegistryKey key = parentKey.OpenSubKey("OSRepoDBMgr");
                        if(key == null)
                        {
                            SetDefaultSettings();
                            SaveSettings();
                            return;
                        }

                        Current.TemporaryFolder = (string)key.GetValue("TemporaryFolder");
                        Current.DatabasePath    = (string)key.GetValue("DatabasePath");
                        Current.RepositoryPath  = (string)key.GetValue("RepositoryPath");
                        Current.UnArchiverPath  = (string)key.GetValue("UnArchiverPath");
                        if(!Enum.TryParse((string)key.GetValue("CompressionAlgorithm"), true,
                                          out Current.CompressionAlgorithm))
                            Current.CompressionAlgorithm = AlgoEnum.GZip;
                        Current.UseAntivirus             = (bool)key.GetValue("UseAntivirus");
                        Current.UseClamd                 = (bool)key.GetValue("UseClamd");
                        Current.ClamdHost                = (string)key.GetValue("ClamdHost");
                        Current.ClamdPort                = (ushort)key.GetValue("ClamdPort");
                        Current.ClamdIsLocal             = (bool)key.GetValue("ClamdIsLocal");
                        Current.UseVirusTotal            = (bool)key.GetValue("UseVirusTotal");
                        Current.VirusTotalKey            = (string)key.GetValue("VirusTotalKey");
                    }
                        break;
                    default:
                    {
                        string configPath =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                        string settingsPath =
                            Path.Combine(configPath, "OSRepoDBMgr.xml");

                        if(!Directory.Exists(configPath))
                        {
                            SetDefaultSettings();
                            SaveSettings();
                            return;
                        }

                        XmlSerializer xs = new XmlSerializer(Current.GetType());
                        prefsSr          = new StreamReader(settingsPath);
                        Current          = (SetSettings)xs.Deserialize(prefsSr);
                        prefsSr.Close();
                    }
                        break;
                }
            }
            catch
            {
                prefsFs?.Close();
                prefsSr?.Close();

                SetDefaultSettings();
                SaveSettings();
            }
        }

        public static void SaveSettings()
        {
            try
            {
                PlatformID ptId = DetectOS.GetRealPlatformID();

                switch(ptId)
                {
                    case PlatformID.MacOSX:
                    case PlatformID.iOS:
                    {
                        NSDictionary root = new NSDictionary
                        {
                            {"TemporaryFolder", Current.TemporaryFolder},
                            {"DatabasePath", Current.DatabasePath},
                            {"RepositoryPath", Current.RepositoryPath},
                            {"UnArchiverPath", Current.UnArchiverPath},
                            {"CompressionAlgorithm", Current.CompressionAlgorithm.ToString()},
                            {"UseAntivirus", Current.UseAntivirus},
                            {"UseClamd", Current.UseClamd},
                            {"ClamdHost", Current.ClamdHost},
                            {"ClamdPort", Current.ClamdPort},
                            {"ClamdIsLocal", Current.ClamdIsLocal},
                            {"UseVirusTotal", Current.UseVirusTotal},
                            {"VirusTotalKey", Current.VirusTotalKey}
                        };

                        string preferencesPath =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
                                         "Preferences");
                        string preferencesFilePath =
                            Path.Combine(preferencesPath, "com.claunia.museum.osrepodbmgr.plist");

                        FileStream fs = new FileStream(preferencesFilePath, FileMode.Create);
                        BinaryPropertyListWriter.Write(fs, root);
                        fs.Close();
                    }
                        break;
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                    case PlatformID.WindowsPhone:
                    {
                        RegistryKey parentKey = Registry
                                               .CurrentUser.OpenSubKey("SOFTWARE", true)
                                              ?.CreateSubKey("Canary Islands Computer Museum");
                        RegistryKey key = parentKey?.CreateSubKey("OSRepoDBMgr");

                        if(key != null)
                        {
                            key.SetValue("TemporaryFolder",                                   Current.TemporaryFolder);
                            key.SetValue("DatabasePath",                                      Current.DatabasePath);
                            key.SetValue("RepositoryPath",                                    Current.RepositoryPath);
                            if(Current.UnArchiverPath != null) key.SetValue("UnArchiverPath", Current.UnArchiverPath);
                            key.SetValue("CompressionAlgorithm",
                                         Current.CompressionAlgorithm);
                            key.SetValue("UseAntivirus",  Current.UseAntivirus);
                            key.SetValue("UseClamd",      Current.UseClamd);
                            key.SetValue("ClamdHost",     Current.ClamdHost);
                            key.SetValue("ClamdPort",     Current.ClamdPort);
                            key.SetValue("ClamdIsLocal",  Current.ClamdIsLocal);
                            key.SetValue("UseVirusTotal", Current.UseVirusTotal);
                            key.SetValue("VirusTotalKey", Current.VirusTotalKey);
                        }
                    }
                        break;
                    default:
                    {
                        string configPath =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                        string settingsPath =
                            Path.Combine(configPath, "OSRepoDBMgr.xml");

                        if(!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                        FileStream    fs = new FileStream(settingsPath, FileMode.Create);
                        XmlSerializer xs = new XmlSerializer(Current.GetType());
                        xs.Serialize(fs, Current);
                        fs.Close();
                    }
                        break;
                }
            }
            #pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
            catch
                #pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
            {
                if(Debugger.IsAttached) throw;
            }
        }

        static void SetDefaultSettings()
        {
            Current = new SetSettings
            {
                TemporaryFolder = Path.GetTempPath(),
                DatabasePath    =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "osrepodbmgr.db"),
                RepositoryPath =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "osrepo"),
                UnArchiverPath       = null,
                CompressionAlgorithm = AlgoEnum.GZip,
                UseAntivirus         = false,
                UseClamd             = false,
                ClamdHost            = null,
                ClamdPort            = 3310,
                ClamdIsLocal         = false,
                UseVirusTotal        = false,
                VirusTotalKey        = null
            };
        }
    }
}