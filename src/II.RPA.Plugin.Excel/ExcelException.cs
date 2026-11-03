using System;

namespace II.RPA.Plugin.Excel
{
    [Serializable]
    internal class ExcelException : Exception
    {
        public ExcelException(string message) : base(message)
        {
        }
    }
}
