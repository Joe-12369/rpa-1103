using System.Text;

namespace II.RPA.Plugin.IE.V2
{
    //6543221
    public struct HTMLRectEX
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public class RegMatchTextChange
    {
        public static void ChangeText(ref string str1)
        {
            string strReg = "\r\n";
            strReg += "+?[]()";
            strReg += '{';
            strReg += '}';
            strReg += "^|$";
            strReg += '\\';
            char[] reg = strReg.ToCharArray();
            for (int i = 0; i < reg.Length; i++)
            {
                string str11 = str1.Replace(reg[i], ' ');
                str1 = str11;
            }

            int nStartIndex = 0;
            int nPosT = -1;
            do
            {
                nPosT = str1.IndexOf('*', nStartIndex);
                if (nPosT > 0)
                {
                    if (str1[nPosT - 1] != '.')
                    {
                        str1 = str1.Remove(nPosT, 1);
                        str1 = str1.Insert(nPosT, " ");
                    }
                }
                else if (nPosT == 0)
                {
                    str1 = str1.Remove(nPosT, 1);
                    str1 = str1.Insert(nPosT, " ");
                }
                else
                {
                    break;
                }

                nStartIndex = nPosT + 1;
            } while (true);

            nStartIndex = 0;
            nPosT = -1;
            do
            {
                nPosT = str1.IndexOf('.', nStartIndex);
                if (nPosT >= 0 && nPosT != str1.Length - 1)
                {
                    if (str1[nPosT + 1] != '*')
                    {
                        str1 = str1.Remove(nPosT, 1);
                        str1 = str1.Insert(nPosT, " ");
                    }
                }
                else if (nPosT == str1.Length - 1)
                {
                    str1 = str1.Remove(nPosT, 1);
                    str1 = str1.Insert(nPosT, " ");
                }
                else
                {
                    break;
                }

                nStartIndex = nPosT + 1;
            } while (true);
        }

        public static void ChangeStringBuildText(ref StringBuilder str1)
        {
            string strReg = "\r\n";
            strReg += "+?[]()";
            strReg += '{';
            strReg += '}';
            strReg += "^|$";
            strReg += '\\';
            char[] reg = strReg.ToCharArray();

            for (int i = 0; i < reg.Length; i++)
            {
                StringBuilder str11 = str1.Replace(reg[i], ' ');
                str1 = str11;
            }

            string str21 = str1.ToString();
            int nStartIndex = 0;
            int nPosT = -1;
            do
            {
                nPosT = str21.IndexOf('*', nStartIndex);
                if (nPosT > 0)
                {
                    if (str21[nPosT - 1] != '.')
                    {
                        str21 = str21.Remove(nPosT, 1);
                        str21 = str21.Insert(nPosT, " ");
                    }
                }
                else if (nPosT == 0)
                {
                    str21 = str21.Remove(nPosT, 1);
                    str21 = str21.Insert(nPosT, " ");
                }
                else
                {
                    break;
                }

                nStartIndex = nPosT + 1;
            } while (true);

            str1 = new StringBuilder(str21);

            str21 = str1.ToString();
            nStartIndex = 0;
            nPosT = -1;
            do
            {
                nPosT = str21.IndexOf('.', nStartIndex);
                if (nPosT >= 0 && nPosT != str21.Length - 1)
                {
                    if (str21[nPosT + 1] != '*')
                    {
                        str21 = str21.Remove(nPosT, 1);
                        str21 = str21.Insert(nPosT, " ");
                    }
                }
                else if (nPosT == str1.Length - 1)
                {
                    str21 = str21.Remove(nPosT, 1);
                    str21 = str21.Insert(nPosT, " ");
                }
                else
                {
                    break;
                }

                nStartIndex = nPosT + 1;
            } while (true);

            str1 = new StringBuilder(str21);
        }
        public static void DoChangeTitleIE(ref string strTitle)
        {
            int nPos1 = strTitle.IndexOf(" - Internet Explorer");
            if (nPos1 >= 0)
            {
                string strTitleT = strTitle.Substring(0, nPos1);
                strTitle = strTitleT;
            }
            else
            {
                int nPos2 = strTitle.IndexOf(" - Windows Internet Explorer");
                if (nPos2 >= 0)
                {
                    string strTitleT = strTitle.Substring(0, nPos2);
                    strTitle = strTitleT;
                }
            }
        }
        public static void DoChangeBuildStringTitleIE(ref StringBuilder strTitle)
        {
            int nPos1 = strTitle.ToString().IndexOf(" - Internet Explorer");
            if (nPos1 >= 0)
            {
                string strTitleT = strTitle.ToString().Substring(0, nPos1);
                strTitle = new StringBuilder(strTitleT);
            }
            else
            {
                int nPos2 = strTitle.ToString().IndexOf(" - Windows Internet Explorer");
                if (nPos2 >= 0)
                {
                    string strTitleT = strTitle.ToString().Substring(0, nPos2);
                    strTitle = new StringBuilder(strTitleT);
                }
            }
        }
    }
}
