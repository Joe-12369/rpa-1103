using System;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    /// <summary>
    /// Emergency production switches. No host configuration mutation is needed;
    /// environment variables are evaluated for each operation so a restarted plugin
    /// process immediately observes the customer's rollback setting.
    /// </summary>
    internal static class OwnerDrawFeatureSwitch
    {
        internal const string GlobalVariable = "II_RPA_OWNER_DRAW_ENABLED";
        internal const string PowerBuilderVariable = "II_RPA_OWNER_DRAW_PB_ENABLED";
        internal const string VclVariable = "II_RPA_OWNER_DRAW_VCL_ENABLED";

        internal static bool IsEnabled(string adapterId, out string status)
        {
            if (!ReadEnabled(GlobalVariable))
            {
                status = "Owner-draw is disabled by " + GlobalVariable + ".";
                return false;
            }
            string variable = string.Equals(adapterId,
                OwnerDrawSelectorCodec.PowerBuilderAdapterId,
                StringComparison.OrdinalIgnoreCase)
                ? PowerBuilderVariable : VclVariable;
            if (!ReadEnabled(variable))
            {
                status = "Owner-draw adapter " + adapterId + " is disabled by "
                    + variable + ".";
                return false;
            }
            status = "Owner-draw feature is enabled.";
            return true;
        }

        private static bool ReadEnabled(string variable)
        {
            string value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value)) return true;
            value = value.Trim();
            return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "off", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "no", StringComparison.OrdinalIgnoreCase);
        }
    }
}
