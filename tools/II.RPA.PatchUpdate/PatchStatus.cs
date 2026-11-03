using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.PatchUpdate
{
    public class PatchStatus
    {
        public string version { get; set; }
        public bool hasPatchUpdate { get; set; }
        public bool patchUpdated { get; set; }
        public bool displayPatchUpdatedInfo { get; set; }
        public int count { get; set; }
    }
}
