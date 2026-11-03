using FlaUI.Core.AutomationElements;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public class ElementModel : BaseAutomationModel
    {
        private ElementModel()
        { }
        public ElementModel(AutomationElement automationElement) 
        {
            Automation = automationElement;
        }
        public override bool Equals(object obj)
        {
            try
            {
                if (Automation != null
                    && obj is ElementModel elementModel
                    && elementModel.Automation != null)
                {
                    return Automation.Equals(elementModel.Automation);
                }
            }
            catch
            {
                //
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            if (Automation != null)
            {
                return Automation.GetHashCode();
            }
            return base.GetHashCode();
        }
    }

    public class HandleEquality : IEqualityComparer<ElementModel>
    {
        public bool Equals(ElementModel x, ElementModel y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            if (x.Handle.HasValue 
                && x.Handle.Value != IntPtr.Zero
                && y.Handle.HasValue 
                && y.Handle.Value != IntPtr.Zero)
            {
                return x.Handle.Value == y.Handle.Value;
            }

            return false;
        }

        public int GetHashCode(ElementModel obj)
        {
            if (obj is null) return 0;

            if (obj.Handle.HasValue && obj.Handle.Value != IntPtr.Zero)
            {
                return obj.Handle.Value.GetHashCode();
            }
            return 0;
        }
    }
}
