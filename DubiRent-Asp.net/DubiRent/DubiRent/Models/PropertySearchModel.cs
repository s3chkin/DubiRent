namespace DubiRent.Models
{
    public class PropertySearchModel
    {
        public string? Title { get; set; }
        public string? Address { get; set; }
        public int? LocationId { get; set; }
        public string? LocationName { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinSquareMeters { get; set; }
        public int? MaxSquareMeters { get; set; }
        public string? SortBy { get; set; }
    }
}

