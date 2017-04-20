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
using System.IO;
using System.Xml.Serialization;
using Claunia.PropertyList;
using Microsoft.Win32;

namespace osrepodbmgr
{
    public class SetSettings
    {
        public string TemporaryFolder;
        public string DatabasePath;
        public string RepositoryPath;
        public string UnArchiverPath;
    }

    public static class Settings
    {
        public static SetSettings Current;

        public static void LoadSettings()
        {
            Current = new SetSettings();
            DiscImageChef.Interop.PlatformID ptID = DiscImageChef.Interop.DetectOS.GetRealPlatformID();

            try
            {
                switch(ptID)
                {
                    case DiscImageChef.Interop.PlatformID.MacOSX:
                    case DiscImageChef.Interop.PlatformID.iOS:
                        {
                            string preferencesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Preferences");
                            string preferencesFilePath = Path.Combine(preferencesPath, "com.claunia.museum.osrepodbmgr.plist");

                            if(!File.Exists(preferencesFilePath))
                            {
                                SetDefaultSettings();
                                SaveSettings();
                            }

                            NSDictionary parsedPreferences = (NSDictionary)BinaryPropertyListParser.Parse(new FileInfo(preferencesFilePath));
                            if(parsedPreferences != null)
                            {
                                NSObject obj;

                                if(parsedPreferences.TryGetValue("TemporaryFolder", out obj))
                                {
                                    Current.TemporaryFolder = ((NSString)obj).ToString();
                                }
                                else
                                    Current.TemporaryFolder = Path.GetTempPath();

                                if(parsedPreferences.TryGetValue("DatabasePath", out obj))
                                {
                                    Current.DatabasePath = ((NSString)obj).ToString();
                                }
                                else
                                    Current.DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "osrepodbmgr.db");

                                if(parsedPreferences.TryGetValue("RepositoryPath", out obj))
                                {
                                    Current.RepositoryPath = ((NSString)obj).ToString();
                                }
                                else
                                    Current.RepositoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "osrepo");

                                if(parsedPreferences.TryGetValue("UnArchiverPath", out obj))
                                {
                                    Current.UnArchiverPath = ((NSString)obj).ToString();
                                }
                                else
                                    Current.UnArchiverPath = null;

                            }
                            else {
                                SetDefaultSettings();
                                SaveSettings();
                            }
                        }
                        break;
                    case DiscImageChef.Interop.PlatformID.Win32NT:
                    case DiscImageChef.Interop.PlatformID.Win32S:
                    case DiscImageChef.Interop.PlatformID.Win32Windows:
                    case DiscImageChef.Interop.PlatformID.WinCE:
                    case DiscImageChef.Interop.PlatformID.WindowsPhone:
                        {
                            RegistryKey parentKey = Registry.CurrentUser.OpenSubKey("SOFTWARE").OpenSubKey("Canary Islands Computer Museum");
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
                            Current.DatabasePath = (string)key.GetValue("DatabasePath");
                            Current.RepositoryPath = (string)key.GetValue("RepositoryPath");
                            Current.UnArchiverPath = (string)key.GetValue("UnArchiverPath");
                        }
                        break;
                    default:
                        {
                            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                            string settingsPath = Path.Combine(configPath, "OSRepoDBMgr.xml");

                            if(!Directory.Exists(configPath))
                            {
                                SetDefaultSettings();
                                SaveSettings();
                                return;
                            }

                            XmlSerializer xs = new XmlSerializer(Current.GetType());
                            StreamReader sr = new StreamReader(settingsPath);
                            Current = (SetSettings)xs.Deserialize(sr);
                        }
                        break;
                }
            }
            catch
            {
                SetDefaultSettings();
                SaveSettings();
            }
        }

        public static void SaveSettings()
        {
            try
            {
                DiscImageChef.Interop.PlatformID ptID = DiscImageChef.Interop.DetectOS.GetRealPlatformID();

                switch(ptID)
                {
                    case DiscImageChef.Interop.PlatformID.MacOSX:
                    case DiscImageChef.Interop.PlatformID.iOS:
                        {
                            NSDictionary root = new NSDictionary();
                            root.Add("TemporaryFolder", Current.TemporaryFolder);
                            root.Add("DatabasePath", Current.DatabasePath);
                            root.Add("RepositoryPath", Current.RepositoryPath);
                            root.Add("UnArchiverPath", Current.UnArchiverPath);

                            string preferencesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Preferences");
                            string preferencesFilePath = Path.Combine(preferencesPath, "com.claunia.museum.osrepodbmgr.plist");

                            FileStream fs = new FileStream(preferencesFilePath, FileMode.Create);
                            BinaryPropertyListWriter.Write(fs, root);
                            fs.Close();
                        }
                        break;
                    case DiscImageChef.Interop.PlatformID.Win32NT:
                    case DiscImageChef.Interop.PlatformID.Win32S:
                    case DiscImageChef.Interop.PlatformID.Win32Windows:
                    case DiscImageChef.Interop.PlatformID.WinCE:
                    case DiscImageChef.Interop.PlatformID.WindowsPhone:
                        {
                            RegistryKey parentKey = Registry.CurrentUser.OpenSubKey("SOFTWARE").CreateSubKey("Canary Islands Computer Museum");
                            RegistryKey key = parentKey.CreateSubKey("OSRepoDBMgr");

                            key.SetValue("TemporaryFolder", Current.TemporaryFolder);
                            key.SetValue("DatabasePath", Current.DatabasePath);
                            key.SetValue("RepositoryPath", Current.RepositoryPath);
                            key.SetValue("UnArchiverPath", Current.UnArchiverPath);
                        }
                        break;
                    default:
                        {
                            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                            string settingsPath = Path.Combine(configPath, "OSRepoDBMgr.xml");

                            if(!Directory.Exists(configPath))
                                Directory.CreateDirectory(configPath);

                            FileStream fs = new FileStream(settingsPath, FileMode.Create);
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
            }
        }

        public static void SetDefaultSettings()
        {
            Current = new SetSettings();
            Current.TemporaryFolder = Path.GetTempPath();
            Current.DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "osrepodbmgr.db");
            Current.RepositoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "osrepo");
            Current.UnArchiverPath = null;
        }
    }
}

