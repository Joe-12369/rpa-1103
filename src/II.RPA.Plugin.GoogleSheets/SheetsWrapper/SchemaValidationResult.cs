using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.SheetsWrapper
{
    public class SchemaValidationResult
    {
        public bool IsValid { get; set; }

        public string ErrorMessage { get; set; }
    }
}
