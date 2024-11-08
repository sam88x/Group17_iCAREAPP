using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;

namespace Group17_iCAREAPP.Controllers
{
    [Authorize(Roles = "Doctor,Nurse")]
    public class PatientRecordController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // GET: PatientRecords
        // List of all patient records by the GeoCode of the patient
        public ActionResult Index()
        {
            var patientRecords = db.PatientRecord
                .Include(p => p.iCAREWorker)
                .Include(p => p.iCAREWorker.iCAREUser)
                .Include(p => p.GeoCodes)
                .ToList();
            return View(patientRecords);
        }

        // GET: PatientRecords/Create
        public ActionResult Create()
        {
            ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description");
            return View();
        }

        // POST: PatientRecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "name,address,dateOfBirth,height,weight,bloodGroup,bedID,treatmentArea,geographicalUnit")] PatientRecord patientRecord)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the iCAREWorker ID from the current user's username
                    var user = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);
                    if (user == null)
                    {
                        ModelState.AddModelError("", "Current user not found in the system.");
                        ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description", patientRecord.geographicalUnit);
                        return View(patientRecord);
                    }

                    patientRecord.ID = Guid.NewGuid().ToString();
                    patientRecord.modifierID = user.ID;  // Use the correct ID instead of username

                    db.PatientRecord.Add(patientRecord);
                    db.SaveChanges();

                    TempData["Success"] = "Patient record created successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating patient record: " + ex.Message);
                }
            }

            ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description", patientRecord.geographicalUnit);
            return View(patientRecord);
        }

        // GET: PatientRecords/Edit/5
        public ActionResult Edit(string id)
        {
            // Check to see if the paitent record id exists
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Checks to see if the patient record exists in the database
            var patientRecord = db.PatientRecord.Find(id);
            if (patientRecord == null)
            {
                return HttpNotFound();
            }

            // Displays names of the geographical units
            ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description", patientRecord.geographicalUnit);
            // Creates list of all possible blood groups so that can be selected
            ViewBag.BloodGroups = new SelectList(
                new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" },
                patientRecord.bloodGroup
            );
            var user = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);

            if (user != null)
                ViewBag.iCAREWorkerID = user.ID;


            return View(patientRecord);
        }

        // POST: PatientRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,name,address,dateOfBirth,height,weight,bloodGroup,bedID,treatmentArea,geographicalUnit")] PatientRecord patientRecord)
        {
            if (ModelState.IsValid)
            {
                var user = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);

                // Marks records as being modified
                patientRecord.modifierID = user.ID;
                db.Entry(patientRecord).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Patient record updated successfully.";
                // Returns to patient details
                return RedirectToAction("Details", "PatientRecord", new { id = patientRecord.ID });
            }
            // Same information as above
            ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description", patientRecord.geographicalUnit);
            ViewBag.BloodGroups = new SelectList(
                new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" },
                patientRecord.bloodGroup
            );

            return View(patientRecord);
        }
        public ActionResult Details(string id)
        {
            try
            {
                // Includes infromation about the documents and treatments for that patient
                var patient = db.PatientRecord
                    .Include("DocumentMetadata")
                    .Include("TreatmentRecord")
                    .FirstOrDefault(p => p.ID == id);

                // If the patient does not exist, redirects to the home page
                if (patient == null)
                {
                    TempData["Error"] = "Patient not found.";
                    return RedirectToAction("Index", "Home");
                }

                return View(patient);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading patient details.";
                return RedirectToAction("Index", "Home");
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