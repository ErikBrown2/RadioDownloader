// Utility to automatically download radio programmes, using a plugin framework for provider specific implementation.
// Copyright © 2007-2010 Matt Robinson
//
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation; either version 2 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public
// License for more details.
//
// You should have received a copy of the GNU General Public License along with this program; if not, write
// to the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

namespace RadioDld
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Windows.Forms;

    public struct ProgrammeInfo
    {
        public string Name;
        public string Description;
        public Bitmap Image;
        public bool SingleEpisode;
    }

    public struct GetProgrammeInfoReturn
    {
        public ProgrammeInfo ProgrammeInfo;
        public bool Success;
        public Exception Exception;
    }

    public struct EpisodeInfo
    {
        public string Name;
        public string Description;
        public int? DurationSecs;
        public DateTime Date;
        public Bitmap Image;
        public Dictionary<string, string> ExtInfo;
    }

    public struct GetEpisodeInfoReturn
    {
        public EpisodeInfo EpisodeInfo;
        public bool Success;
    }

    public enum ProgressIcon
    {
        Downloading,
        Converting
    }

    public delegate void FindNewViewChangeEventHandler(object view);

    public delegate void FindNewExceptionEventHandler(Exception findExp, bool unhandled);

    public delegate void FoundNewEventHandler(string progExtId);

    public delegate void ProgressEventHandler(int percent, string statusText, ProgressIcon icon);

    public delegate void FinishedEventHandler(string fileExtension);

    public interface IRadioProvider
    {
        Guid ProviderId { get; }

        string ProviderName { get; }

        Bitmap ProviderIcon { get; }

        string ProviderDescription { get; }

        int ProgInfoUpdateFreqDays { get; }

        EventHandler GetShowOptionsHandler();

        Panel GetFindNewPanel(object view);

        GetProgrammeInfoReturn GetProgrammeInfo(string progExtId);

        string[] GetAvailableEpisodeIds(string progExtId);

        GetEpisodeInfoReturn GetEpisodeInfo(string progExtId, string episodeExtId);

        event FindNewViewChangeEventHandler FindNewViewChange;

        event FindNewExceptionEventHandler FindNewException;

        event FoundNewEventHandler FoundNew;

        event ProgressEventHandler Progress;

        event FinishedEventHandler Finished;

        void DownloadProgramme(string progExtId, string episodeExtId, ProgrammeInfo progInfo, EpisodeInfo epInfo, string finalName);
    }

    internal class Plugins
    {
        private const string interfaceName = "IRadioProvider";

        private Dictionary<Guid, AvailablePlugin> availablePlugins = new Dictionary<Guid, AvailablePlugin>();

        private struct AvailablePlugin
        {
            public string AssemblyPath;
            public string ClassName;
        }

        public bool PluginExists(Guid pluginID)
        {
            return this.availablePlugins.ContainsKey(pluginID);
        }

        public IRadioProvider GetPluginInstance(Guid pluginID)
        {
            if (this.PluginExists(pluginID))
            {
                return this.CreateInstance(this.availablePlugins[pluginID]);
            }
            else
            {
                return null;
            }
        }

        public Guid[] GetPluginIdList()
        {
            Guid[] pluginIDs = new Guid[this.availablePlugins.Keys.Count];
            this.availablePlugins.Keys.CopyTo(pluginIDs, 0);

            return pluginIDs;
        }

        // Next three functions are based on code from http://www.developerfusion.co.uk/show/4371/3/
        public Plugins(string path)
        {
            string[] dlls = null;
            Assembly thisDll = null;

            // Go through the DLLs in the specified path and try to load them
            dlls = Directory.GetFileSystemEntries(path, "*.dll");

            foreach (string dll in dlls)
            {
                try
                {
                    thisDll = Assembly.LoadFrom(dll);
                    this.ExamineAssembly(thisDll);
                }
                catch
                {
                    // Error loading DLL, we don't need to do anything special
                }
            }
        }

        private void ExamineAssembly(Assembly dll)
        {
            Type thisType = null;
            Type implInterface = null;
            AvailablePlugin pluginInfo = default(AvailablePlugin);

            // Loop through each type in the assembly
            foreach (Type thisType_loopVariable in dll.GetTypes())
            {
                thisType = thisType_loopVariable;

                // Only look at public types
                if (thisType.IsPublic == true)
                {
                    // Ignore abstract classes
                    if (!((thisType.Attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract))
                    {
                        // See if this type implements our interface
                        implInterface = thisType.GetInterface(interfaceName, true);

                        if (implInterface != null)
                        {
                            pluginInfo = new AvailablePlugin();
                            pluginInfo.AssemblyPath = dll.Location;
                            pluginInfo.ClassName = thisType.FullName;

                            try
                            {
                                IRadioProvider pluginInst = null;
                                pluginInst = this.CreateInstance(pluginInfo);
                                this.availablePlugins.Add(pluginInst.ProviderId, pluginInfo);
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
            }
        }

        private IRadioProvider CreateInstance(AvailablePlugin plugin)
        {
            Assembly dll = null;
            object pluginInst = null;

            try
            {
                // Load the assembly
                dll = Assembly.LoadFrom(plugin.AssemblyPath);

                // Create and return class instance
                pluginInst = dll.CreateInstance(plugin.ClassName);

                // Cast to an IRadioProvider and return
                return (IRadioProvider)pluginInst;
            }
            catch
            {
                return null;
            }
        }
    }
}
