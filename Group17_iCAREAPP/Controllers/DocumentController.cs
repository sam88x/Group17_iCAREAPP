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

namespace Group17_iCAREAPP.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        public ActionResult Create(string patientId)
        {
            try
            {
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

                var patient = db.PatientRecord
                    .Include("iCAREWorker")
                    .FirstOrDefault(p => p.ID == patientId);

                if (patient == null)
                {
                    TempData["Error"] = "Patient not found.";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new CreateDocumentViewModel
                {
                    PatientID = patientId,
                    PatientName = patient.name,
                    TreatmentArea = patient.treatmentArea,
                    BedID = patient.bedID,
                    IsDoctor = worker.UserRole.ID == "DR001"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
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

                // Check if nurse is trying to create a prescription
                if (worker.UserRole.ID != "DR001" && model.DocumentType == "Prescription")
                {
                    ModelState.AddModelError("DocumentType", "Only doctors can create prescriptions.");
                    return View(model);
                }

                // Create document metadata
                var docMeta = new DocumentMetadata
                {
                    docID = Guid.NewGuid().ToString(),
                    docName = model.DocumentTitle,
                    dateOfCreation = DateTime.Now,
                    version = 1,
                    userID = worker.ID,
                    patientID = model.PatientID
                };

                // Generate and save PDF
                var pdfPath = GeneratePDF(model, worker, docMeta.docID);

                db.DocumentMetadata.Add(docMeta);
                db.SaveChanges();

                TempData["Success"] = "Document created successfully.";
                return RedirectToAction("Details", "PatientRecord", new { id = model.PatientID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error creating document.";
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Details(string id)
        {
            try
            {
                var document = db.DocumentMetadata
                    .Include("iCAREWorker.iCAREUser")
                    .Include("PatientRecord")
                    .Include("ModificationHistory")
                    .FirstOrDefault(d => d.docID == id);

                if (document == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Document not found for id: {id}");
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction("Index", "MyBoard");
                }

                System.Diagnostics.Debug.WriteLine($"Document found: {document.docName}");
                return View(document);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Details: {ex.Message}");
                TempData["Error"] = "Error loading document details.";
                return RedirectToAction("Index", "MyBoard");
            }
        }

        [HttpGet]
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
                    return Json(new object[] { }, JsonRequestBehavior.AllowGet);
                }

                var suggestions = db.DrugsDictionary
                    .Where(d => d.name.Contains(term))
                    .Select(d => new { d.name })
                    .Take(5)
                    .ToList();

                return Json(suggestions, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new object[] { }, JsonRequestBehavior.AllowGet);
            }
        }

        private string GeneratePDF(object model, iCAREWorker worker, string filePath)
        {
            string content;
            string title;
            string documentType;
            string patientName;
            string patientId;
            string treatmentArea;
            bool isEditMode = false;

            // Extract values based on model type
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
                throw new ArgumentException("Invalid model type");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var doc = new Document())
            {
                try
                {
                    PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
                    doc.Open();

                    // Add metadata
                    doc.AddAuthor(worker.iCAREUser.name);
                    doc.AddCreationDate();
                    doc.AddTitle(title);
                    doc.AddSubject(documentType);

                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    var greyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);

                    // Add original creation info if it's an edit
                    if (isEditMode)
                    {
                        Paragraph originalInfo = new Paragraph();
                        originalInfo.Add(new Chunk($"Original Document Title: {title}\n", greyFont));
                        originalInfo.Add(new Chunk($"Created by: {worker.iCAREUser.name}\n", greyFont));
                        originalInfo.Add(new Chunk($"Document Type: {documentType}\n", greyFont));
                        //originalInfo.Add(new Chunk($"Created on:" {}))
                        originalInfo.Add(new Chunk($"Last Modified: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n", greyFont));
                        originalInfo.Add(new Chunk($"Modified by: {worker.iCAREUser.name}\n", greyFont));
                        doc.Add(originalInfo);
                        doc.Add(new Paragraph(new string('_', 90)));
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

                        doc.Add(new Paragraph(new string('_', 90)));
                        doc.Add(new Paragraph("\n"));
                    }

                    if (documentType == "Prescription")
                    {
                        var warning = new Paragraph("PRESCRIPTION DOCUMENT", headerFont);
                        warning.Alignment = Element.ALIGN_CENTER;
                        doc.Add(warning);
                        doc.Add(new Paragraph("\n"));
                    }

                    // Add main content
                    doc.Add(new Paragraph(content, contentFont));

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
                        doc.Add(new Paragraph(new string('_', 90)));
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
        public ActionResult View(string id)
        {
            try
            {
                var document = db.DocumentMetadata
                    .Include("iCAREWorker.iCAREUser")
                    .Include("PatientRecord")
                    .FirstOrDefault(d => d.docID == id);

                if (document == null)
                {
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction("Index", "MyBoard");
                }

                // Get the latest version number
                var version = document.version ?? 1;

                // Construct filename with version
                var fileName = $"Doc_{document.docID}_v{version}.pdf";
                var filePath = Path.Combine(Server.MapPath("~/App_Data/Documents"), fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "Document file not found.";
                    return RedirectToAction("Index", "MyBoard");
                }

                // Return the PDF file with inline content disposition
                return File(filePath, "application/pdf");  // Removed the filename parameter
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading document.";
                return RedirectToAction("Index", "MyBoard");
            }
        }

        public ActionResult Edit(string id)
        {
            try
            {
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
                if (System.IO.File.Exists(filePath))
                {
                    // Extract only the content section, not the header
                    using (var reader = new iTextSharp.text.pdf.PdfReader(filePath))
                    {
                        for (int page = 1; page <= reader.NumberOfPages; page++)
                        {
                            content += iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, page);
                        }

                        // Clean up the extracted content
                        // Remove header information up to the first line of underscores
                        int separatorIndex = content.IndexOf(new string('_', 90));
                        if (separatorIndex != -1)
                        {
                            content = content.Substring(separatorIndex + 90).Trim();
                        }

                        // If it's a prescription, remove the "PRESCRIPTION DOCUMENT" header
                        content = content.Replace("PRESCRIPTION DOCUMENT", "").Trim();

                        // Remove the footer if it exists (everything after the last line of underscores)
                        separatorIndex = content.LastIndexOf(new string('_', 90));
                        if (separatorIndex != -1)
                        {
                            content = content.Substring(0, separatorIndex).Trim();
                        }

                        // Clean up any extra whitespace
                        content = content.Trim();
                    }
                }
                content = CleanExtractedContent(content);
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
                    DocumentType = GetDocumentType(content) // Helper method to determine document type
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading document for editing.";
                return RedirectToAction("Index", "MyBoard");
            }
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

                    var document = db.DocumentMetadata.Find(model.DocumentId);
                    if (document == null)
                    {
                        TempData["Error"] = "Document not found.";
                        return RedirectToAction("Index", "MyBoard");
                    }

                    // Update metadata
                    document.docName = model.DocumentTitle;
                    document.version = (document.version ?? 1) + 1;

                    // Get the next ID for ModificationHistory
                    int nextId = 1;
                    if (db.ModificationHistory.Any())
                    {
                        nextId = db.ModificationHistory.Max(m => m.ID) + 1;
                    }

                    // Create modification history with explicit ID
                    var modification = new ModificationHistory
                    {
                        ID = nextId,
                        docID = document.docID,
                        datOfModification = DateTime.Now,  // Make sure this matches your DB column name exactly
                        description = $"Document edited: {model.DocumentTitle} (Version {document.version})",
                        modifiedByUserID = worker.iCAREUser.ID
                    };

                    db.ModificationHistory.Add(modification);

                    // Generate new PDF version
                    var fileName = $"Doc_{document.docID}_v{document.version}.pdf";
                    var filePath = Path.Combine(Server.MapPath("~/App_Data/Documents"), fileName);

                    GeneratePDF(model, worker, filePath);

                    // Save all changes
                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Document updated successfully.";
                    return RedirectToAction("Details", "Document", new { id = model.DocumentId });
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"DbUpdateException: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    TempData["Error"] = "Error updating document in database. Please try again.";
                    return View(model);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"General Exception: {ex.Message}");
                    TempData["Error"] = "Error updating document: " + ex.Message;
                    return View(model);
                }
            }
        }

        private string CleanExtractedContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Find the last instance of the header separator
            int lastSeparatorIndex = content.LastIndexOf(new string('_', 90));
            if (lastSeparatorIndex != -1)
            {
                // Find the first instance of the separator after all the headers
                int startIndex = content.IndexOf(new string('_', 90));
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

        private string GetDocumentType(string content)
        {
            // Simple logic to determine document type based on content
            if (content.Contains("PRESCRIPTION") || content.Contains("Rx:"))
                return "Prescription";
            return "General";
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