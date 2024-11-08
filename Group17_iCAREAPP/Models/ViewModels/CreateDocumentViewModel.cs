// Models/ViewModels/CreateDocumentViewModel.cs
/* The CreateDocumentViewModel class is the viewmodel that manages the data required for the create Document form. 
 * This class contains the patient's Id, document's title, content, type, ImageFile and DrugList.
 */


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;  // Add this for SelectListItem

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

        [Display(Name = "Upload Image")]
        public HttpPostedFileBase ImageFile { get; set; }

        public bool IsImageUpload { get; set; }

        public List<SelectListItem> DrugsList { get; set; }

        // Combined constructor
        public CreateDocumentViewModel()
        {
            DocumentType = "General";
            Content = string.Empty;
            DrugsList = new List<SelectListItem>();
            IsImageUpload = false;
        }
    }
}