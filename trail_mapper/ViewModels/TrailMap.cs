using System.Collections.Generic;

namespace trail_mapper.ViewModels
{
    public class TrailMap
    {
        public string Name { get; set; }
        public IList<HistoryItem> History { get; set; }
    }
}
