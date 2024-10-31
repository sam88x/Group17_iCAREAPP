using System;
using System.Linq;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;
using Group17_iCAREAPP.Models.ViewModels;
using System.Data.Entity;

namespace Group17_iCAREAPP.Controllers
{
    [Authorize(Roles = "Doctor,Nurse")]
    public class PaletteController : Controller
    {
        private readonly Group17_iCAREDBEntities _context;
        private const int ItemsPerPage = 12;

        public PaletteController()
        {
            _context = new Group17_iCAREDBEntities();
        }

        public ActionResult Index(string searchQuery = "", string patientSearchQuery = "",
            string sortBy = "date", string filterBy = "all", int page = 1,
            string viewMode = "grid", bool showOnlyMyPatients = false)
        {
            ViewBag.Title = "Palette";

            // Get current user
            var currentUser = User.Identity.Name;
            var worker = _context.iCAREWorker
                .Include(w => w.iCAREUser)
                .FirstOrDefault(w => w.iCAREUser.UserPassword.userName == currentUser);

            if (worker == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get base query
            var query = _context.DocumentMetadata
                .Include(d => d.PatientRecord)
                .Include(d => d.iCAREWorker.iCAREUser)
                .Where(d => d.userID == worker.ID); // Only show documents created by this worker

            // Apply document name search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(d => d.docName.Contains(searchQuery));
            }

            // Apply patient name search filter
            if (!string.IsNullOrEmpty(patientSearchQuery))
            {
                query = query.Where(d => d.PatientRecord.name.Contains(patientSearchQuery));
            }

            // Filter by my patients only
            if (showOnlyMyPatients)
            {
                var myPatientIds = _context.TreatmentRecord
                    .Where(t => t.workerID == worker.ID)
                    .Select(t => t.patientID)
                    .Distinct()
                    .ToList();

                query = query.Where(d => myPatientIds.Contains(d.patientID));
            }

            // Apply sorting
            switch (sortBy.ToLower())
            {
                case "name":
                    query = query.OrderBy(d => d.docName);
                    break;
                case "version":
                    query = query.OrderByDescending(d => d.version);
                    break;
                case "patient":
                    query = query.OrderBy(d => d.PatientRecord.name);
                    break;
                case "date":
                default:
                    query = query.OrderByDescending(d => d.dateOfCreation);
                    break;
            }

            // Calculate pagination
            int totalItems = query.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)ItemsPerPage));
            page = Math.Max(1, Math.Min(page, totalPages));

            // Calculate skip and take values
            int skipAmount = (page - 1) * ItemsPerPage;
            var documents = query
                .Skip(skipAmount)
                .Take(ItemsPerPage)
                .ToList();

            // Create view model
            var viewModel = new PaletteViewModel
            {
                Documents = documents,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                PatientSearchQuery = patientSearchQuery,
                SortBy = sortBy,
                FilterBy = filterBy,
                ViewMode = viewMode,
                ShowOnlyMyPatients = showOnlyMyPatients,
                CurrentUserId = worker.ID
            };

            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}