using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Group17_iCAREAPP.Models;
using Group17_iCAREAPP.Models.ViewModels;
using System.Data.Entity.Infrastructure;
using System.Collections.Generic;
using System.Text;

namespace Group17_iCAREAPP.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // Creating a new document
        public ActionResult Create(string patientId)
        {
            try
            {
                // Look to identify the user's name who is currently logged in as the worker
                var currentUser = User.Identity.Name;
                var worker = db.iCAREWorker
                    .Include("iCAREUser")
                    .Include("UserRole")
                    .FirstOrDefault(w => w.iCAREUser.UserPassword.userName == currentUser);

                // Check on if the worker is found in the database
                if (worker == null)
                {
                    TempData["Error"] = "Worker profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Looks for the patient id of the patient of interest
                var patient = db.PatientRecord
                    .Include("iCAREWorker")
                    .FirstOrDefault(p => p.ID == patientId);

                // Checks that the patient exists
                if (patient == null)
                {
                    TempData["Error"] = "Patient not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Get all drugs from database
                var drugs = db.DrugsDictionary
                    .OrderBy(d => d.name)
                    .Select(d => new SelectListItem
                    {
                        Text = d.name,
                        Value = d.name
                    })
                    .ToList();

                // Stores all the infromation about the document for the view
                var viewModel = new CreateDocumentViewModel
                {
                    PatientID = patientId,
                    PatientName = patient.name,
                    TreatmentArea = patient.treatmentArea,
                    BedID = patient.bedID,
                    IsDoctor = worker.UserRole.ID == "DR001", // Checks the role and whether or not they are doctors
                    DrugsList = drugs  // Full list of drugs in database
                };

                // Used for debugging
                System.Diagnostics.Debug.WriteLine($"Number of drugs loaded: {drugs.Count}");
                foreach (var drug in drugs.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"Drug: {drug.Text}");
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // If the document form can't be created, redirect to home
                TempData["Error"] = "Error loading document creation form.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateDocumentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Finds the worker profile
                var currentUser = User.Identity.Name;
                var worker = db.iCAREWorker
                    .Include("iCAREUser")
                    .Include("UserRole")
                    .FirstOrDefault(w => w.iCAREUser.UserPassword.userName == currentUser);

                if (worker == null)
                {
                    TempData["Error"] = "Worker profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if nurse is trying to create a prescription, should not be needed
                // View solves this issue
                if (worker.UserRole.ID != "DR001" && model.DocumentType == "Prescription")
                {
                    ModelState.AddModelError("DocumentType", "Only doctors can create prescriptions.");
                    return View(model);
                }

                // Create document metadata
                var docMeta = new DocumentMetadata
                {
                    docID = Guid.NewGuid().ToString(), // Creates a unique identifier
                    docName = model.DocumentTitle,
                    dateOfCreation = DateTime.Now,
                    version = 1, // Document version always starts at 1
                    userID = worker.ID,
                    patientID = model.PatientID
                };

                // Checks if the document being created is an image type
                if (model.DocumentType == "Image" && model.ImageFile != null)
                {
                    // Generate PDF
                    var fileName = $"Doc_{docMeta.docID}_v{docMeta.version}.pdf";
                    // Stores all documents in the "~/App_Data/Documents" folder
                    var filePath = Path.Combine(Server.MapPath("~/App_Data/Documents"), fileName);

                    using (var doc = new Document())
                    {
                        // Creating a new PDF
                        PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
                        doc.Open();

                        // Add metadata from above
                        doc.AddAuthor(worker.iCAREUser.name);
                        doc.AddCreationDate();
                        doc.AddTitle(docMeta.docName);
                        doc.AddSubject("Image Document");

                        // Add header to the document
                        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                        var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                        // Adds information to header about the document metadata
                        doc.Add(new Paragraph(docMeta.docName, titleFont));
                        doc.Add(new Paragraph($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", contentFont));
                        doc.Add(new Paragraph($"Author: {worker.iCAREUser.name} ({worker.profession})", contentFont));
                        doc.Add(new Paragraph($"Patient: {model.PatientName} (ID: {model.PatientID})", contentFont));

                        // Include treatment area if present
                        if (!string.IsNullOrEmpty(model.TreatmentArea))
                        {
                            doc.Add(new Paragraph($"Treatment Area: {model.TreatmentArea}", contentFont));
                        }

                        doc.Add(new Paragraph(new string('_', 75)));
                        doc.Add(new Paragraph("\n"));

                        // Add the image
                        var image = iTextSharp.text.Image.GetInstance(model.ImageFile.InputStream);

                        // Scale image to fit page
                        float maxWidth = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;
                        float maxHeight = doc.PageSize.Height - doc.TopMargin - doc.BottomMargin;
                        if (image.Width > maxWidth || image.Height > maxHeight)
                        {
                            float scale = Math.Min(maxWidth / image.Width, maxHeight / image.Height);
                            image.ScalePercent(scale * 100);
                        }

                        // Center the image
                        image.Alignment = Element.ALIGN_CENTER;
                        doc.Add(image);

                        doc.Close();
                    }
                }
                else
                {
                    // Generate normal PDF document
                    var fileName = $"Doc_{docMeta.docID}_v{docMeta.version}.pdf";
                    // Saved in same location as the images
                    var filePath = Path.Combine(Server.MapPath("~/App_Data/Documents"), fileName);
                    // Generates PDF with that information
                    GeneratePDF(model, worker, docMeta, filePath);
                }

                db.DocumentMetadata.Add(docMeta);
                db.SaveChanges();

                // Success message
                TempData["Success"] = "Document created successfully.";
                return RedirectToAction("Details", "PatientRecord", new { id = model.PatientID });
            }
            catch (Exception ex)
            {
                // All errors result in this message
                TempData["Error"] = "Error creating document: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        // Displays information about the document
        public ActionResult Details(string id)
        {
            try
            {
                // Finds the document associated with worker, patient, and version
                var document = db.DocumentMetadata
                    .Include("iCAREWorker.iCAREUser")
                    .Include("PatientRecord")
                    .Include("ModificationHistory")
                    .FirstOrDefault(d => d.docID == id);

                if (document == null)
                {
                    //System.Diagnostics.Debug.WriteLine($"Document not found for id: {id}");
                    // Error if the document can't be found
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction("Index", "MyBoard");
                }

                //System.Diagnostics.Debug.WriteLine($"Document found: {document.docName}");
                return View(document);
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error in Details: {ex.Message}");
                TempData["Error"] = "Error loading document details.";
                return RedirectToAction("Index", "MyBoard");
            }
        }

        [HttpGet]
        // Method helps with drug dictionary implementation
        public JsonResult GetDrugSuggestions(string term)
        {
            try
            {
                // Only show drug suggestions for doctors
                var worker = db.iCAREWorker
                    .Include("UserRole")
                    .FirstOrDefault(w => w.iCAREUser.UserPassword.userName == User.Identity.Name);

                if (worker?.UserRole.ID != "DR001")
                {
                    // Returns empty object if not a doctor
                    return Json(new object[] { }, JsonRequestBehavior.AllowGet);
                }

                // Suggestions contains all the drugs' names in a list
                var suggestions = db.DrugsDictionary
                    .Where(d => d.name.Contains(term))
                    .Select(d => new { d.name })
                    .Take(5)
                    .ToList();

                // Doctors can see full list of drugs
                return Json(suggestions, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new object[] { }, JsonRequestBehavior.AllowGet);
            }
        }

        // Method generates a PDF for text documents
        private string GeneratePDF(object model, iCAREWorker worker, DocumentMetadata doc123, string filePath)
        {
            // Local variables to aid in implementation
            string content;
            string title;
            string documentType;
            string patientName;
            string patientId;
            string treatmentArea;
            bool isEditMode = false;

            // Extract values based on model type
            // Either a create model or an edit model
            if (model is CreateDocumentViewModel createModel)
            {
                content = createModel.Content;
                title = createModel.DocumentTitle;
                documentType = createModel.DocumentType;
                patientName = createModel.PatientName;
                patientId = createModel.PatientID;
                treatmentArea = createModel.TreatmentArea;
                isEditMode = false;
            }
            else if (model is EditDocumentViewModel editModel)
            {
                content = editModel.Content;
                title = editModel.DocumentTitle;
                documentType = editModel.DocumentType;
                patientName = editModel.PatientName;
                patientId = editModel.PatientID;
                treatmentArea = editModel.TreatmentArea;
                isEditMode = true;
            }
            else
            {
                // If the model is neither type, throw error
                throw new ArgumentException("Invalid model type");
            }

            // Ensure directory exists
            // Should always be the same path
            var directory = Path.GetDirectoryName(filePath);
            // Creates directory on first call
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var doc = new Document())
            {
                try
                {
                    // Opens PDF to begin writing
                    PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
                    doc.Open();

                    // Add metadata
                    doc.AddAuthor(worker.iCAREUser.name);
                    doc.AddCreationDate();
                    doc.AddTitle(title);
                    doc.AddSubject(documentType);

                    // Sets consistent font and size
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    var greyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);

                    // Add original creation info if it's an edit
                    if (isEditMode)
                    {
                        Paragraph originalInfo = new Paragraph();
                        originalInfo.Add(new Chunk($"Original Document Title: {title}\n", greyFont));
                        originalInfo.Add(new Chunk($"Created by: {doc123.iCAREWorker.iCAREUser.name}\n", greyFont));
                        originalInfo.Add(new Chunk($"Document Type: {documentType}\n", greyFont));
                        originalInfo.Add(new Chunk($"Created on: {doc123.dateOfCreation:yyyy-MM-dd}\n", greyFont));
                        originalInfo.Add(new Chunk($"Last Modified: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n", greyFont));
                        originalInfo.Add(new Chunk($"Modified by: {worker.iCAREUser.name}\n", greyFont));
                        doc.Add(originalInfo);
                        doc.Add(new Paragraph(new string('_', 75)));
                        doc.Add(Chunk.NEWLINE);
                    }

                    // Add header
                    doc.Add(new Paragraph(title, titleFont));
                    if (!isEditMode)
                    {
                        doc.Add(new Paragraph($"Document Type: {documentType}", headerFont));
                        doc.Add(new Paragraph($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", contentFont));
                        doc.Add(new Paragraph($"Author: {worker.iCAREUser.name} ({worker.profession})", contentFont));
                        doc.Add(new Paragraph($"Patient: {patientName} (ID: {patientId})", contentFont));

                        if (!string.IsNullOrEmpty(treatmentArea))
                        {
                            doc.Add(new Paragraph($"Treatment Area: {treatmentArea}", contentFont));
                        }

                        doc.Add(new Paragraph(new string('_', 75)));
                        doc.Add(new Paragraph("\n"));
                    }

                    // Special creation instructions if the document is a prescription
                    if (documentType == "Prescription")
                    {
                        var warning = new Paragraph("PRESCRIPTION DOCUMENT", headerFont);
                        warning.Alignment = Element.ALIGN_CENTER;
                        doc.Add(warning);
                        doc.Add(new Paragraph("\n"));
                    }

                    // Add main content
                    doc.Add(new Paragraph(content, contentFont));

                    // Indicates the doctor that writes the prescription
                    if (documentType == "Prescription")
                    {
                        doc.Add(new Paragraph("\n"));
                        var footer = new Paragraph($"Prescribed by Dr. {worker.iCAREUser.name}", headerFont);
                        footer.Alignment = Element.ALIGN_RIGHT;
                        doc.Add(footer);
                    }

                    // Add edit footer if it's an edited document
                    if (isEditMode)
                    {
                        doc.Add(new Paragraph("\n"));
                        doc.Add(new Paragraph(new string('_', 75)));
                        var editInfo = new Paragraph($"Edited by {worker.iCAREUser.name} on {DateTime.Now:yyyy-MM-dd HH:mm:ss}", greyFont);
                        editInfo.Alignment = Element.ALIGN_RIGHT;
                        doc.Add(editInfo);
                    }
                }
                finally
                {
                    if (doc.IsOpen())
                    {
                        doc.Close();
                    }
                }
            }

            return Path.GetFileName(filePath);
        }

        // Enables user to look at the document
        public ActionResult View(string id)
        {
            try
            {
                // Finds the document id that has the patient and worker
                var document = db.DocumentMetadata
                    .Include("iCAREWorker.iCAREUser")
                    .Include("PatientRecord")
                    .FirstOrDefault(d => d.docID == id);

                // Check to see if the document exists
                if (document == null)
                {
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction("Index", "MyBoard");
                }

                // Construct filename - for uploaded files, don't use version number
                string fileName;
                if (document.docName.StartsWith("Uploaded:"))  // Add this prefix when uploading files
                {
                    fileName = $"Doc_{document.docID}.pdf";
                }
                else
                {
                    // For regular documents, use version
                    var version = document.version ?? 1;
                    fileName = $"Doc_{document.docID}_v{version}.pdf";
                }

                // All files directed to this path, so that is where we look
                var filePath = Path.Combine(Server.MapPath("~/App_Data/Documents"), fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "Document file not found.";
                    return RedirectToAction("Index", "MyBoard");
                }

                // Return the PDF file
                return File(filePath, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading document.";
                return RedirectToAction("Index", "MyBoard");
            }
        }

        // Allows user to edit text documents
        public ActionResult Edit(string id)
        {
            try
            {
                // Looking for document
                var document = db.DocumentMetadata
                    .Include("iCAREWorker.iCAREUser")
                    .Include("PatientRecord")
                    .FirstOrDefault(d => d.docID == id);

                if (document == null)
                {
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction("Index", "MyBoard");
                }

                // Read the current version's PDF
                var version = document.version ?? 1;
                var fileName = $"Doc_{document.docID}_v{version}.pdf";
                var filePath = Path.Combine(Server.MapPath("~/App_Data/Documents"), fileName);

                string content = "";
                // First check that the file exists
                if (System.IO.File.Exists(filePath))
                {
                    using (var reader = new iTextSharp.text.pdf.PdfReader(filePath))
                    {
                        StringBuilder contentBuilder = new StringBuilder();
                        for (int page = 1; page <= reader.NumberOfPages; page++)
                        {
                            // Checks all the text on all possible pages and parses it to a StringBuilder type
                            contentBuilder.Append(iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, page));
                            if (page < reader.NumberOfPages)
                            {
                                contentBuilder.Append("\n");
                            }
                        }
                        content = contentBuilder.ToString();
                    }

                    // Extract and clean the content
                    content = ExtractMainContent(content);

                    // Debug logging
                    System.Diagnostics.Debug.WriteLine("Extracted content:");
                    System.Diagnostics.Debug.WriteLine(content);
                }

                // Creates model for documents that are to be edited
                var viewModel = new EditDocumentViewModel
                {
                    DocumentId = document.docID,
                    PatientID = document.patientID,
                    DocumentTitle = document.docName,
                    Content = content,
                    PatientName = document.PatientRecord.name,
                    TreatmentArea = document.PatientRecord.treatmentArea,
                    BedID = document.PatientRecord.bedID,
                    Version = document.version ?? 1,
                    IsDoctor = User.IsInRole("Doctor"),
                    DocumentType = DetermineDocumentType(document.docName, content)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading document for editing: " + ex.Message;
                return RedirectToAction("Index", "MyBoard");
            }
        }

        // Method pulls out the information from the PDF, omitting the header
        private string ExtractMainContent(string fullContent)
        {
            try
            {
                // Split content into lines for better processing
                var lines = fullContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(l => l.Trim())
                                     .Where(l => !string.IsNullOrWhiteSpace(l))
                                     .ToList();

                // Find the indexes of separator lines
                int firstSeparatorIndex = -1;
                int lastSeparatorIndex = -1;

                // Looks for space between the lines that separate content from header
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Count(c => c == '_') >= 75)
                    {
                        if (firstSeparatorIndex == -1)
                            firstSeparatorIndex = i;
                        else
                            lastSeparatorIndex = i;
                    }
                }

                // If found both separators
                if (firstSeparatorIndex != -1 && lastSeparatorIndex != -1 && lastSeparatorIndex > firstSeparatorIndex)
                {
                    // Get content between separators, excluding the separators themselves
                    var contentLines = lines.Skip(firstSeparatorIndex + 1)
                                          .Take(lastSeparatorIndex - firstSeparatorIndex - 1)
                                          .Where(line => !line.StartsWith("Document Type:"))
                                          .Where(line => !line.StartsWith("Date:"))
                                          .Where(line => !line.StartsWith("Author:"))
                                          .Where(line => !line.StartsWith("Patient:"))
                                          .Where(line => !line.StartsWith("Treatment Area:"))
                                          .Where(line => !line.Contains("PRESCRIPTION DOCUMENT"))
                                          .Where(line => !line.StartsWith("Prescribed by"))
                                          .ToList();

                    return string.Join("\n", contentLines).Trim();
                }

                // Can also look for the index at the end of the header
                var headerEndIndex = lines.FindIndex(l =>
                    l.StartsWith("Treatment Area:") ||
                    l.StartsWith("Patient:"));

                if (headerEndIndex != -1 && headerEndIndex + 1 < lines.Count)
                {
                    var contentLines = lines.Skip(headerEndIndex + 1)
                                          .Where(line => !line.All(c => c == '_'))
                                          .Where(line => !line.StartsWith("Prescribed by"))
                                          .ToList();

                    return string.Join("\n", contentLines).Trim();
                }

                // If all else fails, return cleaned content
                return CleanContent(fullContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting content: {ex.Message}");
                return CleanContent(fullContent);
            }
        }

        // Method cleans the file content to be edited
        private string CleanContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Finds all the lines that are not part of the header
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(l => l.Trim())
                              .Where(l => !string.IsNullOrWhiteSpace(l))
                              .Where(l => !l.All(c => c == '_'))
                              .Where(l => !l.StartsWith("Document Type:"))
                              .Where(l => !l.StartsWith("Date:"))
                              .Where(l => !l.StartsWith("Author:"))
                              .Where(l => !l.StartsWith("Patient:"))
                              .Where(l => !l.StartsWith("Treatment Area:"))
                              .Where(l => !l.Contains("PRESCRIPTION DOCUMENT"))
                              .Where(l => !l.StartsWith("Prescribed by"))
                              .ToList();

            // Remove any remaining header-like content from the beginning
            while (lines.Count > 0 &&
                   (lines[0].Contains(":") ||
                    lines[0].Contains("____") ||
                    lines[0].Contains("Document") ||
                    lines[0].Contains("Date")))
            {
                lines.RemoveAt(0);
            }

            return string.Join("\n", lines).Trim();
        }

        // Helper method to return the document type
        private string DetermineDocumentType(string title, string content)
        {
            // Document that is typed is either a prescription or a general note
            if (title.IndexOf("Prescription", StringComparison.OrdinalIgnoreCase) >= 0 ||
                content.IndexOf("PRESCRIPTION DOCUMENT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                content.IndexOf("Prescribed by Dr.", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Prescription";
            }
            return "General";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditDocumentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Collect the worker information
                    var currentUser = User.Identity.Name;
                    var worker = db.iCAREWorker
                        .Include("iCAREUser")
                        .Include("UserRole")
                        .FirstOrDefault(w => w.iCAREUser.UserPassword.userName == currentUser);

                    if (worker == null)
                    {
                        TempData["Error"] = "Worker profile not found.";
                        return RedirectToAction("Index", "Home");
                    }

                    // Collects the document information
                    var document = db.DocumentMetadata
                        .Include("iCAREWorker.iCAREUser")
                        .FirstOrDefault(d => d.docID == model.DocumentId);

                    if (document == null)
                    {
                        TempData["Error"] = "Document not found.";
                        return RedirectToAction("Index", "MyBoard");
                    }

                    // Check permissions for prescription documents
                    if (model.DocumentType == "Prescription" && worker.UserRole.ID != "DR001")
                    {
                        TempData["Error"] = "Only doctors can edit prescription documents.";
                        return RedirectToAction("Details", "Document", new { id = model.DocumentId });
                    }

                    // Update metadata
                    document.docName = model.DocumentTitle;
                    document.version = (document.version ?? 1) + 1;

                    // Create modification history
                    var modification = new ModificationHistory
                    {
                        ID = db.ModificationHistory.Any() ? db.ModificationHistory.Max(m => m.ID) + 1 : 1,
                        docID = document.docID,
                        datOfModification = DateTime.Now,
                        description = $"Document edited: {model.DocumentTitle} (Version {document.version})",
                        modifiedByUserID = worker.iCAREUser.ID
                    };

                    db.ModificationHistory.Add(modification);

                    // Generate new PDF version
                    var fileName = $"Doc_{document.docID}_v{document.version}.pdf";
                    var filePath = Path.Combine(Server.MapPath("~/App_Data/Documents"), fileName);
                    GeneratePDF(model, worker, document, filePath);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Document updated successfully.";
                    return RedirectToAction("Details", "Document", new { id = model.DocumentId });
                }
                catch (Exception ex)
                {
                    // Returns to previous verison if any errors
                    transaction.Rollback();
                    TempData["Error"] = "Error updating document: " + ex.Message;
                    return View(model);
                }
            }
        }

        // Method cleans up the content in the PDF to be edited
        private string CleanExtractedContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Find the last instance of the header separator
            int lastSeparatorIndex = content.LastIndexOf(new string('_', 75));
            if (lastSeparatorIndex != -1)
            {
                // Find the first instance of the separator after all the headers
                int startIndex = content.IndexOf(new string('_', 75));
                if (startIndex != -1)
                {
                    // Get the content between the first and last separators
                    string mainContent = content.Substring(startIndex + 90, lastSeparatorIndex - startIndex - 90);

                    // Remove "PRESCRIPTION DOCUMENT" if present
                    mainContent = mainContent.Replace("PRESCRIPTION DOCUMENT", "").Trim();

                    // Clean up any extra whitespace
                    return mainContent.Trim();
                }
            }

            return content.Trim();
        }

        // Helper method returns the type of the document
        private string GetDocumentType(string content)
        {
            // Simple logic to determine document type based on content
            if (content.Contains("PRESCRIPTION") || content.Contains("Rx:"))
                return "Prescription";
            return "General";
        }

        [HttpPost]
        //Method works to easily upload files to pdfs
        public ActionResult UploadFile()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("Starting file upload...");

                var file = Request.Files["file"];
                var patientId = Request["PatientID"];
                var documentTitle = Request["documentTitle"];

                // Log received data
                // System.Diagnostics.Debug.WriteLine($"PatientID: {patientId}");
                // System.Diagnostics.Debug.WriteLine($"Document Title: {documentTitle}");
                // System.Diagnostics.Debug.WriteLine($"File present: {(file != null)}");
                if (file != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"File size: {file.ContentLength}");
                    // System.Diagnostics.Debug.WriteLine($"File type: {file.ContentType}");
                }

                // If file does not exist or is of 0 length
                if (file == null || file.ContentLength == 0)
                {
                    return Json(new { success = false, message = "No file was selected." });
                }
                // If the patient information is not there
                if (string.IsNullOrEmpty(patientId))
                {
                    return Json(new { success = false, message = "Patient ID is required." });
                }
                // If the title of the document is not included
                if (string.IsNullOrEmpty(documentTitle))
                {
                    documentTitle = Path.GetFileNameWithoutExtension(file.FileName);
                }
                // Special title for uploaded documents
                documentTitle = "Uploaded: " + documentTitle;

                var currentUser = User.Identity.Name;
                //System.Diagnostics.Debug.WriteLine($"Current User: {currentUser}");

                var worker = db.iCAREWorker
                    .Include("iCAREUser")
                    .FirstOrDefault(w => w.iCAREUser.UserPassword.userName == currentUser);
                // If the worker is not found in the database
                if (worker == null)
                {
                    return Json(new { success = false, message = "Worker profile not found." });
                }

                //System.Diagnostics.Debug.WriteLine($"Worker found: {worker.ID}");

                // Create document metadata
                var docMeta = new DocumentMetadata
                {
                    docID = Guid.NewGuid().ToString(), // Specific id for document
                    docName = documentTitle,
                    dateOfCreation = DateTime.Now,
                    version = 1,
                    userID = worker.ID,
                    patientID = patientId
                };

                // Ensure the Documents directory exists
                var documentsPath = Server.MapPath("~/App_Data/Documents");
                if (!Directory.Exists(documentsPath))
                {
                    // System.Diagnostics.Debug.WriteLine($"Creating directory: {documentsPath}");
                    Directory.CreateDirectory(documentsPath);
                }

                var fileName = $"Doc_{docMeta.docID}.pdf";
                var filePath = Path.Combine(documentsPath, fileName);

                // System.Diagnostics.Debug.WriteLine($"Saving file to: {filePath}");

                try
                {
                    if (file.ContentType.StartsWith("image/"))
                    {
                        // System.Diagnostics.Debug.WriteLine("Converting image to PDF...");
                        using (var doc = new Document())
                        {
                            // Opens up a new PDF to be written to
                            PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
                            doc.Open();

                            var image = iTextSharp.text.Image.GetInstance(file.InputStream);
                            // Scales the image to the PDF
                            float maxWidth = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;
                            float maxHeight = doc.PageSize.Height - doc.TopMargin - doc.BottomMargin;
                            if (image.Width > maxWidth || image.Height > maxHeight)
                            {
                                float scale = Math.Min(maxWidth / image.Width, maxHeight / image.Height);
                                image.ScalePercent(scale * 100);
                            }
                            // Aligns the image in the center of the document
                            image.Alignment = Element.ALIGN_CENTER;
                            doc.Add(image);
                            doc.Close();
                        }
                    }
                    else if (file.ContentType.Equals("application/pdf"))
                    {
                        // System.Diagnostics.Debug.WriteLine("Saving PDF directly...");
                        file.SaveAs(filePath);
                    }
                    else
                    {
                        return Json(new { success = false, message = $"Unsupported file type: {file.ContentType}" });
                    }

                    // System.Diagnostics.Debug.WriteLine("Saving to database...");
                    db.DocumentMetadata.Add(docMeta);
                    db.SaveChanges();

                    // System.Diagnostics.Debug.WriteLine("Upload complete!");
                    // If successful, sends it back to patient details
                    return Json(new
                    {
                        success = true,
                        redirectUrl = Url.Action("Details", "PatientRecord", new { id = patientId })
                    });
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"Error saving file: {ex.Message}");
                    // System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    return Json(new { success = false, message = $"Error saving file: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Upload error: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Upload error: {ex.Message}" });
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}