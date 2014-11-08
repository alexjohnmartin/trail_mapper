using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using trail_mapper.ViewModels;

namespace trail_mapper
{
    internal class SaveHelper
    {
        public void CleanUpAutoSave(bool _autoSaved, DateTime _recordingStartTime)
        {
            if (_autoSaved)
            {
                try
                {
                    using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                        iso.DeleteFile("AutoSave-" + _recordingStartTime.ToString("yyyyMMdd-hhmmss"));
                }
                catch (Exception)
                {
                    //TODO:log error
                }
            }
        }

        public void SaveTrail(TrailMap trailMap)
        {
            var json = JsonConvert.SerializeObject(trailMap);
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = iso.CreateFile(trailMap.Id.ToString() + ".json"))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine(json);
                writer.Flush();
                writer.Close();
            }
        }
    }
}
