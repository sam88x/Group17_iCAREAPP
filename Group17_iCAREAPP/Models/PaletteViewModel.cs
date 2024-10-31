using System.Collections.Generic;

namespace Group17_iCAREAPP.Models.ViewModels
{
    public class PaletteViewModel
    {
        public IEnumerable<DocumentMetadata> Documents { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
        public string PatientSearchQuery { get; set; }
        public string SortBy { get; set; }
        public string FilterBy { get; set; }
        public string ViewMode { get; set; }
        public bool ShowOnlyMyPatients { get; set; }
        public string CurrentUserId { get; set; }

        public PaletteViewModel()
        {
            Documents = new List<DocumentMetadata>();
            ViewMode = "grid"; // Default view mode
            SortBy = "date";   // Default sort
        }
    }
}