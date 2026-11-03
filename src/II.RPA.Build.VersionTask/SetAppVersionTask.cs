using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace II.RPA.Build.VersionTask
{
    public class SetAppVersionTask : Task
    {
        [Microsoft.Build.Framework.Required]
        public string MainVersion
        {
            get;
            set;
        }
        [Microsoft.Build.Framework.Required]
        public string BuildVersion
        {
            get;
            set;
        }
        [Microsoft.Build.Framework.Output]
        public string FullVersionNumber
        {
            get;
            set;
        }
        [Microsoft.Build.Framework.Output]
        public string MajorNumber
        {
            get;
            set;
        }
        [Microsoft.Build.Framework.Output]
        public string MinorNumber
        {
            get;
            set;
        }
        [Microsoft.Build.Framework.Output]
        public string RevisionNumber
        {
            get;
            set;
        }
        [Microsoft.Build.Framework.Output]
        public string BuildNumber
        {
            get;
            set;
        }
        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MainVersion);
                Log.LogMessage(BuildVersion);
                FullVersionNumber = $"{MainVersion}.{BuildVersion}";
                Log.LogMessage(FullVersionNumber);
                BuildNumber = BuildVersion;
                var versionArray = MainVersion.Split('.');
                if (versionArray.Count() == 3)
                {
                    MajorNumber = versionArray[0];
                    Log.LogMessage(MajorNumber);
                    MinorNumber = versionArray[1];
                    Log.LogMessage(MinorNumber);
                    RevisionNumber = versionArray[2];
                    Log.LogMessage(RevisionNumber);
                    return true;
                }
                else
                {
                    Log.LogMessage($"{MainVersion} is not correct version number");
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
