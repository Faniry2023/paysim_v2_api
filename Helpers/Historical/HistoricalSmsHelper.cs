using API_PAYSIM.Models;

namespace API_PAYSIM.Helpers.Historical
{
    public class HistoricalSmsHelper
    {
        public int Page {  get; set; }
        public int Count { get; set; }
        public decimal? Balance { get; set; }

        public List<HistoricalSmsModel> HistoricalSms { get; set; } = new();
    }
}
