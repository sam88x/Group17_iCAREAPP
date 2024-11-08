using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;

namespace Group17_iCAREAPP.Controllers
{
    public class iCAREAdminsController : Controller
    {
        private Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // GET: iCAREAdmins/Index
        // Retrieves and displays a list of all admin users with their related user data
        // Handles database errors gracefully by returning empty list if needed
        // Includes error handling and logging via TempData
        // Returns: Index view with list of iCAREAdmin objects or empty list on error
        public ActionResult Index()
        {
            try
            {
                // Check if database context is available
                if (db == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Database context is not initialized");
                }

                // Explicitly load the iCAREAdmin with related iCAREUser data
                var adminList = db.iCAREAdmin
                    .Include(i => i.iCAREUser)
                    .ToList();

                // Check if the list is null or empty
                if (adminList == null)
                {
                    adminList = new List<iCAREAdmin>(); // Return empty list instead of null
                }

                return View(adminList);
            }
            catch (Exception ex)
            {
                // Log the exception details here if you have logging implemented
                // For now, we'll create a TempData message
                TempData["ErrorMessage"] = "An error occurred while retrieving admin data: " + ex.Message;
                return View(new List<iCAREAdmin>()); // Return empty list in case of error
            }
        }

        // GET: iCAREAdmins/Details/5
        // Displays detailed information for a specific admin user
        // Includes related iCAREUser data through Entity Framework Include
        // Parameters:
        //   id: Admin ID to retrieve details for
        // Returns: Details view with admin information or redirects with error
        public ActionResult Details(string id)
        {
            // Throws Bad Request error if user does not exist
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Find the admin by user id
            try
            {
                iCAREAdmin iCAREAdmin = db.iCAREAdmin
                    .Include(i => i.iCAREUser)
                    .FirstOrDefault(i => i.ID == id);

                if (iCAREAdmin == null)
                {
                    return HttpNotFound();
                }
                return View(iCAREAdmin);
            }
            // If the admin info can't be found throw an error
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error retrieving admin details: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: iCAREAdmins/Create
        // Displays form for creating a new admin user
        // Populates dropdown with available users who can be made admins
        // Handles empty user list scenario with warning message
        // Returns: Create view with user selection list or redirects with error
        public ActionResult Create()
        {
            try
            {
                // Displays the form for creating anew admin user
                var users = db.iCAREUser.ToList();
                if (users == null || !users.Any())
                {
                    TempData["WarningMessage"] = "No users available for admin creation.";
                }
                // Users can become the admin
                ViewBag.ID = new SelectList(users ?? new List<iCAREUser>(), "ID", "name");
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading create form: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: iCAREAdmins/Create
        // Processes the creation of a new admin user
        // Validates model state and saves to database
        // Parameters:
        //   iCAREAdmin: Admin object with bound properties (ID, adminEmail, dateHired, dateFinished)
        // Returns: Redirects to Index on success or returns to view with errors
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,adminEmail,dateHired,dateFinished")] iCAREAdmin iCAREAdmin)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Once created, save to database and redirect to index
                    db.iCAREAdmin.Add(iCAREAdmin);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Admin created successfully.";
                    return RedirectToAction("Index");
                }

                ViewBag.ID = new SelectList(db.iCAREUser, "ID", "name", iCAREAdmin.ID);
                return View(iCAREAdmin);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating admin: " + ex.Message;
                ViewBag.ID = new SelectList(db.iCAREUser, "ID", "name", iCAREAdmin.ID);
                return View(iCAREAdmin);
            }
        }

        // Implements proper disposal of database context
        // Ensures database connections are properly closed
        // Parameters:
        //   disposing: Boolean indicating if managed resources should be disposed
        protected override void Dispose(bool disposing)
        {
            if (disposing && db != null)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}