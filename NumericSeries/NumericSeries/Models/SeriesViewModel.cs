namespace NumericSeries.Models
{
    public class SeriesViewModel
    {
        public string Series { get; set; } = string.Empty;
        public int N { get; set; }
        public long Result { get; set; }
        public string? NumAnterior { get; set; } 
        public string NumSiguiente { get; set; } = string.Empty;
    }
}
