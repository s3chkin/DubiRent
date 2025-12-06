namespace DubiRent.Models
{
    public class BreadcrumbItem
    {
        public string Text { get; set; } = string.Empty;
        public string? Action { get; set; }
        public string? Controller { get; set; }
        public string? Url { get; set; }
        public bool IsActive { get; set; } = false;
    }
}

