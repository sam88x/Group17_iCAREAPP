// Models/ViewModels/EditDocumentViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Group17_iCAREAPP.Models.ViewModels
{
    public class EditDocumentViewModel
    {
        public string DocumentId { get; set; }

        [Required]
        public string PatientID { get; set; }

        [Required]
        [Display(Name = "Document Title")]
        public string DocumentTitle { get; set; }

        [Required]
        [Display(Name = "Document Content")]
        public string Content { get; set; }

        [Required]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; }

        // For displaying info
        public string PatientName { get; set; }
        public string TreatmentArea { get; set; }
        public string BedID { get; set; }
        public int Version { get; set; }
        public bool IsDoctor { get; set; }

        public EditDocumentViewModel()
        {
            DocumentType = "General";
            Content = string.Empty;
            Version = 1;
        }
    }
}