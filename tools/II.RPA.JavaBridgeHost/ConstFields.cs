using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.JavaBridgeHost
{
    public static class ConstFields
    {
        public const string PATTERN_EQUAL = "equal";
        public const string PATTERN_CONTAIN = "contain";
        public const string PATTERN_BEGIN = "begin";
        public const string PATTERN_END = "end";
        public const string PATTERN_REGULAR = "regular";
        public const string PATTERN_BIGGER = "bigger";
        public const string PATTERN_SMALLER = "smaller";
        //----------------------------------------------------
        public const string SELECTOR_NAME = "name";
        public const string SELECTOR_CLASS_NAME = "className";
        public const string SELECTOR_INDEX_IN_PARENT = "indexinparent";
        public const string SELECTOR_INDEX_IN_PARENT_NEW = "@index";
        public const string SELECTOR_INDEX = "index";
        public const string SELECTOR_ID = "id";
        public const string SELECTOR_ROLE = "role";
        public const string SELECTOR_TITLE = "title";
        public const string SELECTOR_POSITION_X = "x";
        public const string SELECTOR_POSITION_Y = "y";

        public const string SELECTOR_ROOT_FILE = "filename";
        public const string SELECTOR_ROOT_SELECTOR = "selector";
        public const string SELECTOR_ROOT_PROCESSNAME = "processname";

        public const string PROP_ACCURATE_TURE = "true";
        public const string PROP_ACCURATE_FALSE = "false";

        public const string PROP_NAME_FLAG = "Flag";

        public const string SELECTOR_ELEMENT_NAME_TAG = "name";
        public const string SELECTOR_ELEMENT_TYPE_TAG = "type";
        public const string SELECTOR_ELEMENT_SELECTOR_TAG = "selector";
        public const string SELECTOR_ELEMENT_PROPS_TAG = "props";

        public const string COMPARE_SYMBOL_STAR = "*";
        public const string SELECTOR_OLD_FORMAT_VERSION = "0.0";
        public const string SELECTOR_NEW_FORMAT_VERSION_1 = "1.0";

        public const string ROLE_VALUE_TABLE = "table";
        public const string ROLE_VALUE_TABLE_CELLINDEX = "cellIndex";
        public const string ROLE_VALUE_TABLE_COLUMN = "column";
        public const string ROLE_VALUE_TABLE_ROW = "row";
        public const string ROLE_VALUE_CHECK = "check";
        public const string ROLE_VALUE_RADIO = "radio";
        public const string ROLE_VALUE_COMBO = "combo";

        public const string FORMAT_ROLE_NAME = "{0}#{1}";
        public const string FORMAT_ROLE_IDX = "{0}({1})";

        public static bool EqualIgnoreCase(this string value1, string value2)
        {
            return value1.Equals(value2, StringComparison.OrdinalIgnoreCase);
        }

        public static IdParts GetRoleInfoInIdStr(string id)
        {
            if (id.Contains("#"))
            {
                string[] items = id.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                return new IdParts() { Role = items[0], Name = string.Join(string.Empty, items.Skip(1)) };
            }
            else if (id.Contains("(") && id.EndsWith(")"))
            {
                string[] items = id.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                return new IdParts() { Role = items[0], Index = items[1] };
            }
            else
            {
                return new IdParts() { Role = id };
            }
        }

    }
}
