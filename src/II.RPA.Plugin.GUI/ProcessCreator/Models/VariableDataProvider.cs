using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace II.RPA.Plugin.GUI.ProcessCreator.Models
{
    public class DummyVariableDataProvider
    {
        public static IEnumerable<RootModel> LoadDataFromJson() 
        {
            using (var fileStream = System.IO.File.Open(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", "variable.json"), FileMode.Open))
            {
                using (var sw = new StreamReader(fileStream))
                {
                    using (var jw = new JsonTextReader(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        return serializer.Deserialize<IEnumerable<RootModel>>(jw);
                    }
                }
            }
        }
    }
}
