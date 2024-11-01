using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;
using System.Diagnostics;

namespace Group17_iCAREAPP.Controllers
{
    public class GeoCodesController : Controller
    {
        private Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // GET: GeoCodes
        public ActionResult Index()
        {
            var geoCodes = db.GeoCodes.Include(g => g.PatientRecord);
            return View(geoCodes.ToList());
        }

        // GET: GeoCodes/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var patientRecords = db.PatientRecord.Where(pr => pr.geographicalUnit == id).ToList();
            var user = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);

            if (user != null)
                ViewBag.UserId = user.ID;

            ViewBag.GeoDescription = db.GeoCodes.FirstOrDefault(g => g.ID == id).description;


            return View(patientRecords);
        }



        public ActionResult AssignWholeArea(string id)
        {
            Debug.WriteLine("==========================================");
            Debug.WriteLine("Starting AssignWholeArea");
            Debug.WriteLine($"Area ID: {id}");

            if (id == null)
            {
                Debug.WriteLine("Error: ID is null");
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            try
            {
                var patientRecords = db.PatientRecord
                    .Where(pr => pr.geographicalUnit == id)
                    .ToList();

                Debug.WriteLine($"Found {patientRecords.Count} patients in area");

                var user = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);
                Debug.WriteLine($"Current user name: {User.Identity.Name}");

                if (user != null)
                {
                    ViewBag.UserId = user.ID;
                    ViewBag.WorkerId = user.ID;
                    Debug.WriteLine($"User/Worker ID set: {user.ID}");
                }
                else
                {
                    Debug.WriteLine("Warning: User not found");
                }

                var worker = db.iCAREWorker.FirstOrDefault(w => w.ID == user.ID);
                if (worker != null)
                {
                    var roleName = db.UserRole.FirstOrDefault(r => r.ID == worker.userPermission)?.roleName;
                    ViewBag.roleName = roleName;
                    Debug.WriteLine($"Role name set: {roleName}");
                }
                else
                {
                    Debug.WriteLine("Warning: Worker not found");
                }

                ViewBag.GeoDescription = db.GeoCodes.FirstOrDefault(g => g.ID == id)?.description;
                Debug.WriteLine($"Area description: {ViewBag.GeoDescription}");

                Debug.WriteLine("AssignWholeArea completed successfully");
                return View(patientRecords);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AssignWholeArea: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // GET: GeoCodes/Create
        public ActionResult Create()
        {
            ViewBag.ID = new SelectList(db.PatientRecord, "ID", "name");
            return View();
        }

        // POST: GeoCodes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,description")] GeoCodes geoCodes)
        {
            if (ModelState.IsValid)
            {
                db.GeoCodes.Add(geoCodes);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ID = new SelectList(db.PatientRecord, "ID", "name", geoCodes.ID);
            return View(geoCodes);
        }

        // GET: GeoCodes/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GeoCodes geoCodes = db.GeoCodes.Find(id);
            if (geoCodes == null)
            {
                return HttpNotFound();
            }
            ViewBag.ID = new SelectList(db.PatientRecord, "ID", "name", geoCodes.ID);
            return View(geoCodes);
        }

        // POST: GeoCodes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,description")] GeoCodes geoCodes)
        {
            if (ModelState.IsValid)
            {
                db.Entry(geoCodes).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ID = new SelectList(db.PatientRecord, "ID", "name", geoCodes.ID);
            return View(geoCodes);
        }

        // GET: GeoCodes/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GeoCodes geoCodes = db.GeoCodes.Find(id);
            if (geoCodes == null)
            {
                return HttpNotFound();
            }
            return View(geoCodes);
        }

        // POST: GeoCodes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            GeoCodes geoCodes = db.GeoCodes.Find(id);
            db.GeoCodes.Remove(geoCodes);
            db.SaveChanges();
            return RedirectToAction("Index");
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