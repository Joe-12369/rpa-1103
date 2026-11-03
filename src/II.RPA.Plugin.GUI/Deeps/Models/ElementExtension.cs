namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public static class ElementExtension
    {
        public static void GetChildren(this ElementModel element, int level = 1)
        {
            if (level <= 0 || element?.Automation == null)
                return;

            foreach (var child in element.Automation.FindAllChildren())
            {
                if (child == null) continue;

                var childModel = child.ToModel();
                element.Children.Add(childModel);
                childModel.GetChildren(level - 1);
            }
        }
    }
}
