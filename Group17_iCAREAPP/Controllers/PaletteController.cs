using System;
using System.Linq;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;
using Group17_iCAREAPP.Models.ViewModels;

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

        public ActionResult Index(string searchQuery = "", string sortBy = "date", string filterBy = "all", int page = 1)
        {
            ViewBag.Title = "Palette";
            
            // Get base query
            var query = _context.DocumentMetadata.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(d => d.docName.Contains(searchQuery));
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
            skipAmount = Math.Max(0, Math.Min(skipAmount, totalItems));
            int takeAmount = Math.Min(ItemsPerPage, totalItems - skipAmount);

            // Get paginated results
            var documents = query
                .Skip(skipAmount)
                .Take(takeAmount)
                .ToList();

            // Create view model
            var viewModel = new PaletteViewModel
            {
                Documents = documents,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                SortBy = sortBy,
                FilterBy = filterBy
            };

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult SelectDocument(string docId)
        {
            if (string.IsNullOrEmpty(docId))
            {
                return HttpNotFound();
            }

            var document = _context.DocumentMetadata.Find(docId);
            if (document == null)
            {
                return HttpNotFound();
            }

            return RedirectToAction("View", "Document", new { id = docId });
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