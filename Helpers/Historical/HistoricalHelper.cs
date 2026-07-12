using API_PAYSIM.Models;

namespace API_PAYSIM.Helpers.Historical
{
    public class HistoricalHelper
    {
        public int Page {  get; set; }
        public int Count {  get; set; }
        public List<HistoricalModel> Historicals { get; set; } = new();
    }
}
