using II.RPA.WinAutomation.CommonUtility.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class LanguageChangedEventArgs : EventArgs
    {
        public LanguageCultureEnum OldLanguage { get; }
        public LanguageCultureEnum NewLanguage { get; }

        public LanguageChangedEventArgs(LanguageCultureEnum oldLang, LanguageCultureEnum newLang)
        {
            OldLanguage = oldLang;
            NewLanguage = newLang;
        }
    }
}
