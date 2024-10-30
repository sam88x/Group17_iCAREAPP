// Models/ViewModels/CreateDocumentViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace Group17_iCAREAPP.Models.ViewModels
{
    public class CreateDocumentViewModel
    {
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

        // For displaying patient info
        public string PatientName { get; set; }
        public string TreatmentArea { get; set; }
        public string BedID { get; set; }
        public bool IsDoctor { get; set; }

        public CreateDocumentViewModel()
        {
            DocumentType = "General";
            Content = string.Empty;
        }

        [Display(Name = "Upload Image")]
        public HttpPostedFileBase ImageFile { get; set; }

        public bool IsImageUpload { get; set; }
    }
}