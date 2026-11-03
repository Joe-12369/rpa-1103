using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Deeps.Models;
using System;

namespace II.RPA.Plugin.GUI.Deeps
{
    public static class MapExtensions
    {
        public static string ToDtoJson(this ElementModel model)
        {
            var dto = ToDto(model);
            return dto.ToJson();
        }
        public static ElementDto ToDto(this ElementModel model)
        {
            if (string.IsNullOrEmpty(model.Id))
                throw new InvalidOperationException($"Null or empty is not allowed for '{nameof(model.Id)}'");
            return new ElementDto(model.Id, model.Name, model.Tag);
        }

        public static ElementModel ToModel(this AutomationElement automationElement)
        {
            var controlType = string.IsNullOrEmpty(automationElement.Properties.LocalizedControlType.ValueOrDefault) ? "unknown" : automationElement.Properties.LocalizedControlType.ValueOrDefault;
            var model = new ElementModel(automationElement)
            {
                Name = automationElement.Properties.Name.ValueOrDefault,
                Tag = controlType,
                Handle = automationElement.Properties.NativeWindowHandle.ValueOrDefault,
            };
            model.Region = model.Automation.Properties.BoundingRectangle.ValueOrDefault;
            return model;
        }
    }
}
