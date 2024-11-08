using System;
using System.Linq;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;
using Group17_iCAREAPP.Models.ViewModels;
using System.Data.Entity;

namespace Group17_iCAREAPP.Controllers
{
    [Authorize]
    public class PaletteController : Controller
    {
        private readonly Group17_iCAREDBEntities _context;
        // Only allows 12 items per page for the card view
        private const int ItemsPerPage = 12;

        // Constructor: Initializes database context for the controller
        // Creates new instance of Group17_iCAREDBEntities for database operations
        public PaletteController()
        {
            _context = new Group17_iCAREDBEntities();
        }

        // GET: Palette/Index
        // Displays searchable, filterable grid/list view of all documents in the system
        // Includes pagination, sorting, and filtering capabilities
        // Parameters:
        //   searchQuery: Optional document name search term
        //   patientSearchQuery: Optional patient name search term
        //   sortBy: Sort order (date/name/version/patient)
        //   filterBy: Filter criteria
        //   page: Current page number (12 items per page)
        //   viewMode: Display mode (grid/list)
        //   showOnlyMyPatients: Toggle to show only current worker's patients
        // Returns: View with PaletteViewModel containing filtered document list
        // Redirects to home if worker profile not found
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
                .AsQueryable(); // All files in the system

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

            // Calculate pages to easily move through documents
            int totalItems = query.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)ItemsPerPage));
            page = Math.Max(1, Math.Min(page, totalPages));

            // Calculate how many documents to skip
            int skipAmount = (page - 1) * ItemsPerPage;
            var documents = query
                .Skip(skipAmount)
                .Take(ItemsPerPage)
                .ToList();

            // Create view model to make the view easier to manage
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


        // Implements proper disposal of database context
        // Ensures database connections are properly closed
        // Parameters:
        //   disposing: Boolean indicating if managed resources should be disposed
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