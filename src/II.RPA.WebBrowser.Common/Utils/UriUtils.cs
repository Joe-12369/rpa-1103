using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Utils
{
    public static class UriUtils
    {
        public static string ValidURL(string url)
        {
            string actualUrl = string.Empty;
            try
            {
                new Uri(url);
                actualUrl = url;
            }
            catch (UriFormatException)
            {
                if (new Regex("^[a-z,A-Z]:[\\/\\\\].*").IsMatch(url))
                {
                    actualUrl = url;
                }
                else
                {
                    actualUrl = "http://" + url;
                }
            }
            return actualUrl;
        }

    }
}
