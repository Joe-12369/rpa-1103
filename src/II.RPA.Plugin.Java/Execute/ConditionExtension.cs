using II.RPA.Plugin.Core;
using System;
using II.RPA.Plugin.Core.CompareMode;

namespace II.RPA.Plugin.Java.Execute
{
    public static class ConditionExtension
    {
        private const int MATCHES_ARRAY_SIZE = 16;
        private const char ANSI_DOS_STAR = '>';
        private const char ANSI_DOS_QM = '<';
        private const char DOS_DOT = '"';
        internal static bool ConiditionMatch(this JavaSelectorItemProperty source, string propValue)
        {
            if (source.Value.Contains(ConstFields.COMPARE_SYMBOL_STAR) && 
                source.SelectorVersion == ConstFields.SELECTOR_OLD_FORMAT_VERSION)
            {
                return FitsMask(propValue, source.Value);

            }
            else
            {
                if (CormpareFactory.IsDecimalRule(source.Pattern))
                {
                    var rule = CormpareFactory.GenerateDecimalMode(source.Pattern);
                    decimal d1 = 0;
                    decimal d2 = 0;
                    if (rule != null && decimal.TryParse(source.Value, out d1) && decimal.TryParse(propValue, out d2))
                    {
                        return rule.Compare(d1, d2);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var rule = CormpareFactory.GenerateStringMode(source.Pattern);
                    if (rule != null)
                    {
                        return rule.Compare(propValue, source.Value);
                    }
                    return false;
                }
            }
        }
        public static bool FitsMask(string name, string mask)
        {
            if (mask == name) return true;
            return StrictMatchPattern(name, mask);
        }

        /// <summary>
        /// 表达式匹配
        ///
        /// * matches 0 or more characters.
        ///
        /// ? matches exactly 1 character.
        ///
        /// DOS_STAR matches 0 or more characters until encountering and matching
        ///                the final . in the name.
        ///
        ///  DOS_QM matches any single character, or upon encountering a period or
        ///                end of name string, advances the expression to the end of the
        ///                set of contiguous DOS_QMs.
        ///
        /// DOS_DOT matches either a . or zero characters beyond name string.
        /// </summary>
        /// <param name="name">Supplies the input name to check for</param>
        /// <param name="expression">Supplies the input expression to check against</param>
        /// <returns>TRUE if Name is an element in the set of strings denoted by the input Expression and FALSE otherwise</returns>
        public static bool StrictMatchPattern(string name, string expression)
        {
            int nameOffset;
            int exprOffset;
            int length;

            int srcCount;
            int destCount;
            int previousDestCount;
            int matchesCount;

            char nameChar = '\0';
            char exprChar = '\0';

            int[] previousMatches = new int[MATCHES_ARRAY_SIZE];
            int[] currentMatches = new int[MATCHES_ARRAY_SIZE];

            int maxState;
            int currentState;

            bool nameFinished = false;
            if (name == null || name.Length == 0 || expression == null || expression.Length == 0)
            {
                return false;
            }

            //
            //  * 或 *.*的情况
            //

            if (expression.Equals("*") || expression.Equals("*.*"))
            {
                return true;
            }
            //*X的情形
            if (expression[0] == '*' && expression.IndexOf('*', 1) == -1)
            {
                int rightLength = expression.Length - 1;
                if (name.Length >= rightLength && String.Compare(expression, 1, name, name.Length - rightLength, rightLength, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            previousMatches[0] = 0;
            matchesCount = 1;

            nameOffset = 0;
            maxState = expression.Length * 2;

            while (!nameFinished)
            {
                if (nameOffset < name.Length)
                {
                    nameChar = name[nameOffset];
                    length = 1;
                    nameOffset++;
                }
                else
                {
                    nameFinished = true;

                    //
                    //  if we have already exhasted the expression, C#.  Don't
                    //  continue.
                    //
                    if (previousMatches[matchesCount - 1] == maxState)
                    {
                        break;
                    }
                }

                //
                //  Now, for each of the previous stored expression matches, see what
                //  we can do with this name character.
                //
                srcCount = 0;
                destCount = 0;
                previousDestCount = 0;

                while (srcCount < matchesCount)
                {

                    //
                    //  We have to carry on our expression analysis as far as possible
                    //  for each character of name, so we loop here until the
                    //  expression stops matching.  A clue here is that expression
                    //  cases that can match zero or more characters end with a
                    //  continue, while those that can accept only a single character
                    //  end with a break.
                    //
                    exprOffset = ((previousMatches[srcCount++] + 1) / 2);
                    length = 0;

                    while (true)
                    {
                        if (exprOffset == expression.Length)
                        {
                            break;
                        }

                        //
                        //  The first time through the loop we don't want
                        //  to increment ExprOffset.
                        //

                        exprOffset += length;

                        currentState = exprOffset * 2;

                        if (exprOffset == expression.Length)
                        {
                            currentMatches[destCount++] = maxState;
                            break;
                        }

                        exprChar = expression[exprOffset];
                        length = 1;

                        //
                        //  Before we get started, we have to check for something
                        //  really gross.  We may be about to exhaust the local
                        //  space for ExpressionMatches[][], so we have to allocate
                        //  some pool if this is the case.  Yuk!
                        //

                        if (destCount >= MATCHES_ARRAY_SIZE - 2)
                        {
                            int newSize = currentMatches.Length * 2;
                            int[] tmp = new int[newSize];
                            Array.Copy(currentMatches, tmp, currentMatches.Length);
                            currentMatches = tmp;

                            tmp = new int[newSize];
                            Array.Copy(previousMatches, tmp, previousMatches.Length);
                            previousMatches = tmp;
                        }

                        //
                        //  * matches any character zero or more times.
                        //

                        if (exprChar == '*')
                        {
                            currentMatches[destCount++] = currentState;
                            currentMatches[destCount++] = (currentState + 1);
                            continue;
                        }

                        //
                        //  DOS_STAR matches any character except . zero or more times.
                        //

                        if (exprChar == ANSI_DOS_STAR)
                        {
                            bool iCanEatADot = false;

                            //
                            //  If we are at a period, determine if we are allowed to
                            //  consume it, ie. make sure it is not the last one.
                            //
                            if (!nameFinished && (nameChar == '.'))
                            {
                                char tmpChar;
                                int offset;

                                int nameLength = name.Length;
                                for (offset = nameOffset; offset < nameLength; offset++)
                                {
                                    tmpChar = name[offset];
                                    length = 1;

                                    if (tmpChar == '.')
                                    {
                                        iCanEatADot = true;
                                        break;
                                    }
                                }
                            }

                            if (nameFinished || (nameChar != '.') || iCanEatADot)
                            {
                                currentMatches[destCount++] = currentState;
                                currentMatches[destCount++] = (currentState + 1);
                                continue;
                            }
                            else
                            {

                                //
                                //  We are at a period.  We can only match zero
                                //  characters (ie. the epsilon transition).
                                //
                                currentMatches[destCount++] = (currentState + 1);
                                continue;
                            }
                        }

                        //
                        //  The following expreesion characters all match by consuming
                        //  a character, thus force the expression, and thus state
                        //  forward.
                        //
                        currentState += length * 2;

                        //
                        //  DOS_QM is the most complicated.  If the name is finished,
                        //  we can match zero characters.  If this name is a '.', we
                        //  don't match, but look at the next expression.  Otherwise
                        //  we match a single character.
                        //
                        if (exprChar == ANSI_DOS_QM)
                        {

                            if (nameFinished || (nameChar == '.'))
                            {
                                continue;
                            }

                            currentMatches[destCount++] = currentState;
                            break;
                        }

                        //
                        //  A DOS_DOT can match either a period, or zero characters
                        //  beyond the end of name.
                        //
                        if (exprChar == DOS_DOT)
                        {

                            if (nameFinished)
                            {
                                continue;
                            }

                            if (nameChar == '.')
                            {
                                currentMatches[destCount++] = currentState;
                                break;
                            }
                        }

                        //
                        //  From this point on a name character is required to even
                        //  continue, let alone make a match.
                        //
                        if (nameFinished)
                        {
                            break;
                        }

                        //
                        //  If this expression was a '?' we can match it once.
                        //
                        if (exprChar == '?')
                        {
                            currentMatches[destCount++] = currentState;
                            break;
                        }

                        //
                        //  Finally, check if the expression char matches the name char
                        //
                        if (exprChar == nameChar)
                        {
                            currentMatches[destCount++] = currentState;
                            break;
                        }

                        //
                        //  The expression didn't match so go look at the next
                        //  previous match.
                        //

                        break;
                    }


                    //
                    //  Prevent duplication in the destination array.
                    //
                    //  Each of the arrays is montonically increasing and non-
                    //  duplicating, thus we skip over any source element in the src
                    //  array if we just added the same element to the destination
                    //  array.  This guarentees non-duplication in the dest. array.
                    //

                    if ((srcCount < matchesCount) && (previousDestCount < destCount))
                    {
                        while (previousDestCount < destCount)
                        {
                            int previousLength = previousMatches.Length;
                            while ((srcCount < previousLength) && (previousMatches[srcCount] < currentMatches[previousDestCount]))
                            {
                                srcCount += 1;
                            }
                            previousDestCount += 1;
                        }
                    }
                }

                //
                //  If we found no matches in the just finished itteration, it's time
                //  to bail.
                //

                if (destCount == 0)
                {
                    return false;
                }

                //
                //  Swap the meaning the two arrays
                //

                {
                    int[] tmp;

                    tmp = previousMatches;

                    previousMatches = currentMatches;

                    currentMatches = tmp;
                }

                matchesCount = destCount;
            }

            currentState = previousMatches[matchesCount - 1];

            return currentState == maxState;
        }

    }
}
