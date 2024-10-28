using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Group17_iCAREAPP.Models.ViewModels
{
    public class PaletteViewModel
    {
        public List<DocumentMetadata> Documents { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be positive")]
        public int CurrentPage { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Total pages must be positive")]
        public int TotalPages { get; set; }

        [DisplayName("Search")]
        public string SearchQuery { get; set; }

        [DisplayName("Sort By")]
        public string SortBy { get; set; }

        [DisplayName("Filter By")]
        public string FilterBy { get; set; }

        public PaletteViewModel()
        {
            Documents = new List<DocumentMetadata>();
            CurrentPage = 1;
            TotalPages = 1;
            SortBy = "date";
            FilterBy = "all";
        }
    }
}