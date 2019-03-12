﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Xml.Serialization;

namespace Swarmops.Database
{
    public partial class SwarmDb
    {
        /// <summary>
        ///     This is the IN-CODE revision of the database. The version we expect to be running against.
        /// </summary>
        public static int DbVersionExpected => 76;


        // This class is in horrible need of serious refactoring. For its simplicity in task, it's a horrible bowl of spaghetti.

        [Serializable]
        public class Configuration
        {
            private static Configuration _configuration;

            public Configuration()
            {
            }

            public Configuration (Credentials read, Credentials write, Credentials admin)
            {
                Read = read;
                Write = write;
                Admin = admin;
            }

            public Credentials Read { get; set; }
            public Credentials Write { get; set; }
            public Credentials Admin { get; set; }

            public static bool IsConfigured()
            {
                if (_configuration == null)
                {
                    try
                    {
                        Load();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }

                string testString = _configuration.Admin.Username;

                return !String.IsNullOrEmpty (testString);
            }


            public Configuration Get()
            {
                if (_configuration == null)
                {
                    Load();
                }

                return _configuration;
            }

            public static bool TestConfigurationWritable()
            {
                try
                {
                    using (
                        FileStream stream = new FileStream (GetConfigurationFileName(), FileMode.Append,
                            FileAccess.Write,
                            FileShare.ReadWrite))
                    {
                        // do nothing                                                       
                    }
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            }

            public static void Load()
            {
                XmlSerializer serializer = new XmlSerializer (typeof (Configuration));

                string configFileName = GetConfigurationFileName();

                using (
                    FileStream readFileStream = new FileStream (configFileName, FileMode.Open, FileAccess.Read,
                        FileShare.Read))
                {
                    _configuration = (Configuration) serializer.Deserialize (readFileStream);
                }
            }

            public static void Set (Configuration configuration)
            {
                XmlSerializer serializer = new XmlSerializer (typeof (Configuration));

                string fileName = GetConfigurationFileName();
                using (TextWriter writeFileStream = new StreamWriter (fileName))
                {
                    serializer.Serialize (writeFileStream, configuration);
                }

                _configuration = configuration;
            }

            public static string GetConfigurationFileName()
            {
                // Four possibilities here. Debug environment on Windows (dev), Console environment on Windows (dev),
                // Console/daemon production environment, and web production environment.

                // In both production environments, we should use /etc/swarmops/database.config.
                // In dev Web, we should use ~/database.config.
                // In dev console, we should use database.config.

                if (Path.DirectorySeparatorChar == '/')
                {
                    // Production process - just use the simple filename

                    return "/etc/swarmops/database.config";
                }
                if (Debugger.IsAttached)
                {
                    if (HttpContext.Current != null)
                    {
                        // Dev web process. This will throw if we're not in a HttpContext and trying to debug something else.
                        return HttpContext.Current.Server.MapPath ("~/database.config");
                    }
                    else
                    {
                        // Dev's debug console process.

                        string configLocation = "../Site/database.config";

                        // however, we don't know exactly how deep in the directory structure we are, so keep adding ../ until we
                        // correct level. We may hit the root if the config doesn't exist and that'll throw us out.

                        while (!File.Exists (configLocation))
                        {
                            configLocation = "../" + configLocation;
                        }
                        return configLocation;
                    }
                }
                throw new NotImplementedException ("Invalid state");

                // Dev console process

                // Each dev needs to set the working directory to the Console directory when debugging
                // return "database.config"; 
            }
        }

        [Serializable]
        public class Credentials
        {
            public Credentials()
            {
                // paramless ctor to enable serialization
            }

            public Credentials (string database, ServerSet servers, string username, string password)
            {
                Database = database;
                ServerSet = servers;
                Username = username;
                Password = password;
            }

            public string Database { get; set; }
            public ServerSet ServerSet { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [Serializable]
        public class ServerSet
        {
            public ServerSet()
            {
                // paramless ctor to enable serialization
                ServerPriorities = new List<string>();
            }

            public ServerSet (string singleServer)
            {
                ServerPriorities = new List<string>();
                ServerPriorities.Add (singleServer);
            }

            public List<string> ServerPriorities { get; set; }
            // array of semicolon-delimited hostnames; topmost string in array is first-priority servers, and so on.
        }
    }
}
