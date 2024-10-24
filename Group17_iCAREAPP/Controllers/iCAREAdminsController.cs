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

        // GET: iCAREAdmins
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
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error retrieving admin details: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: iCAREAdmins/Create
        public ActionResult Create()
        {
            try
            {
                var users = db.iCAREUser.ToList();
                if (users == null || !users.Any())
                {
                    TempData["WarningMessage"] = "No users available for admin creation.";
                }
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,adminEmail,dateHired,dateFinished")] iCAREAdmin iCAREAdmin)
        {
            try
            {
                if (ModelState.IsValid)
                {
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

        // Other actions remain the same but should be updated with similar error handling

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