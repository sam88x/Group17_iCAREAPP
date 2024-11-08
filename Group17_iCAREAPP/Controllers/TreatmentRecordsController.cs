using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;

namespace Group17_iCAREAPP.Controllers
{
    public class TreatmentRecordsController : Controller
    {
        private Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // GET: TreatmentRecords
        //Creates list of Treatment Records
        public ActionResult Index()
        {
            var treatmentRecord = db.TreatmentRecord.Include(t => t.iCAREWorker).Include(t => t.PatientRecord);
            return View(treatmentRecord.ToList());
        }


        // GET: TreatmentRecords/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TreatmentRecord treatmentRecord = db.TreatmentRecord.Find(id);
            if (treatmentRecord == null)
            {
                return HttpNotFound();
            }
            return View(treatmentRecord);
        }

        // GET: TreatmentRecords/Create
        public ActionResult Create()
        {
            ViewBag.workerID = new SelectList(db.iCAREWorker, "ID", "profession");
            ViewBag.patientID = new SelectList(db.PatientRecord, "ID", "name");
            return View();
        }


        // POST: Check if the patient passed as argument(patientId, PK) can be assigned to the current user.
        // [Detail]
        // Takes the patient's id and traverse the TreatmentRecord Table to check if it is already assigned.
        // It it is not assigned and the PatientAssignmentStatus table doesn't contain the patient's information, creat an information with the patientId.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CheckAssignability(string patientId)
        {
            try
            {
                // Get current user
                var user = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Get current worker, using current user's ID,
                var worker = db.iCAREWorker
                    .Include(w => w.UserRole)
                    .FirstOrDefault(w => w.ID == user.ID);

                if (worker == null)
                {
                    return Json(new { success = false, message = "Worker not found." });
                }

                // Get patient
                var patient = db.PatientRecord
                    .Include(p => p.PatientAssignmentStatus)
                    .FirstOrDefault(p => p.ID == patientId);

                if (patient == null)
                {
                    return Json(new { success = false, message = "Patient not found." });
                }

                // Check if already assigned
                bool isAssigned = db.TreatmentRecord
                    .Any(t => t.workerID == worker.ID && t.patientID == patientId);

                if (isAssigned)
                {
                    return Json(new { success = false, message = "You are already assigned to this patient." });
                }

                // Get or create assignment status
                var assignmentStatus = patient.PatientAssignmentStatus;
                if (assignmentStatus == null)
                {
                    assignmentStatus = new PatientAssignmentStatus
                    {
                        PatientRecordID = patientId,
                        AssignmentStatus = "New",
                        NumOfNurses = 0
                    };
                    db.PatientAssignmentStatus.Add(assignmentStatus);
                    db.SaveChanges();
                }

                // Check to determine if the patient can be assign to the current user.
                // Check the user's role(Doctor / Nurse) and the number of Doctor and Nurses currently assigned to the patient.
                if (worker.UserRole.roleName == "Doctor")
                {
                    if (assignmentStatus.AssignmentStatus == "Assigned")
                    {
                        return Json(new { success = false, message = "Another doctor is already assigned." });
                    }
                    if (assignmentStatus.NumOfNurses == 0)
                    {
                        return Json(new { success = false, message = "No nurse is assigned yet." });
                    }
                }
                else if (worker.UserRole.roleName == "Nurse")
                {
                    if (assignmentStatus.NumOfNurses >= 3)
                    {
                        return Json(new { success = false, message = "Maximum nurses already assigned." });
                    }
                }

                return Json(new { success = true, message = "Available for assignment." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult GetPatientRecord(string patientId)
        {
            var patientRecord = db.PatientRecord.FirstOrDefault(p => p.ID == patientId);

            var patientData = new
            {
                ID = patientRecord.ID,
                name = patientRecord.name,
                treatmentArea = patientRecord.treatmentArea,
            };

            return Json(patientData, JsonRequestBehavior.AllowGet);
        }

        // GET : 
        public ActionResult AddTreatmentRecord(string patientId)
        {
            ViewBag.patientId = patientId;
            var patient = db.PatientRecord.FirstOrDefault(p => p.ID.ToString() == patientId);
            ViewBag.patientName = patient != null ? patient.name : "Unknown Patient";

            var user = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);

            if (user != null)
                ViewBag.workerId = user.ID;

            var now = DateTime.Now;
            var treatmentId = "TREAT-" + now.Ticks;
            var thedate = now.ToString("yyyy-MM-dd"); 

            

            ViewBag.treatmentId = treatmentId;
            ViewBag.thedate = thedate;

            return View();
        }



        // POST: TreatmentRecords/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "treatmentID,description,treatmentDate,patientID,workerID")] TreatmentRecord treatmentRecord)
        {
            if (ModelState.IsValid)
            {

                db.TreatmentRecord.Add(treatmentRecord);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            
            return View(treatmentRecord);      

        }

        // POST : 
        [HttpPost]
        public ActionResult AddTreatmentRecord([Bind(Include = "description,treatmentDate,patientID,workerID,treatmentID")] TreatmentRecord treatmentRecord)
        {
            if (ModelState.IsValid)
            {
                db.TreatmentRecord.Add(treatmentRecord);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Treatment record added successfully.";
               
                return RedirectToAction("Index","MyBoard");
            }


            return View(treatmentRecord);

        }

        // Assign a patient when the current user is a Nurse.
        private void assignNurse(string patientId)
        {
            var assignmentStatus = db.PatientAssignmentStatus
                .FirstOrDefault(p => p.PatientRecordID == patientId);

            if(assignmentStatus == null) // make new assignment
            {
                assignmentStatus = new PatientAssignmentStatus
                {
                    PatientRecordID = patientId,
                    AssignmentStatus = "NurseAssigned",
                    NumOfNurses = 1
                };
                db.PatientAssignmentStatus.Add(assignmentStatus);
            }
            else // modify assignment
            {
                assignmentStatus.NumOfNurses++;
            }
            db.SaveChanges();
        }

        // Assign a patient when the current worker is a Doctor.
        private void assignDoctor(string patientId)
        {
            var assignmentStatus = db.PatientAssignmentStatus
                .FirstOrDefault(p => p.PatientRecordID == patientId);

            assignmentStatus.AssignmentStatus = "Assigned";
            db.SaveChanges();
        }

        // Assign Patients using treatmentRecord depending on the worker's role
        [HttpPost]
        public JsonResult AssignPatient([Bind(Include = "treatmentID,description,treatmentDate,patientID,workerID")] TreatmentRecord treatmentRecord, string roleName)
        {
            try
            {
                Debug.WriteLine($"AssignPatient called - Role: {roleName}, PatientID: {treatmentRecord.patientID}");

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(x => x.Errors)
                        .Select(x => x.ErrorMessage));
                    return Json(new { success = false, message = "Invalid model state: " + errors });
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var assignmentStatus = db.PatientAssignmentStatus
                            .FirstOrDefault(p => p.PatientRecordID == treatmentRecord.patientID);

                        if (assignmentStatus == null)
                        {
                            assignmentStatus = new PatientAssignmentStatus
                            {
                                PatientRecordID = treatmentRecord.patientID,
                                AssignmentStatus = roleName == "Doctor" ? "Assigned" : "NurseAssigned",
                                NumOfNurses = roleName == "Nurse" ? 1 : 0
                            };
                            db.PatientAssignmentStatus.Add(assignmentStatus);
                        }
                        else
                        {
                            if (roleName == "Nurse")
                            {
                                assignmentStatus.NumOfNurses++;
                            }
                            else if (roleName == "Doctor")
                            {
                                assignmentStatus.AssignmentStatus = "Assigned";
                            }
                        }

                        // Set the treatment date if it's not set
                        if (treatmentRecord.treatmentDate == default(DateTime))
                        {
                            treatmentRecord.treatmentDate = DateTime.Now;
                        }

                        db.TreatmentRecord.Add(treatmentRecord);
                        db.SaveChanges();
                        transaction.Commit();

                        Debug.WriteLine("Assignment successful");
                        return Json(new
                        {
                            success = true,
                            message = "Assignment successful",
                            roleName = roleName
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during assignment: {ex.Message}");
                        transaction.Rollback();
                        return Json(new { success = false, message = "Assignment failed: " + ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Outer error in AssignPatient: {ex.Message}");
                return Json(new { success = false, message = "Assignment failed: " + ex.Message });
            }
        }

        // GET: TreatmentRecords/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            TreatmentRecord treatmentRecord = db.TreatmentRecord
                .Include(t => t.iCAREWorker)
                .Include(t => t.PatientRecord)
                .FirstOrDefault(t => t.treatmentID == id);

            if (treatmentRecord == null)
            {
                return HttpNotFound();
            }

            // Store the original treatment date in TempData
            TempData["OriginalTreatmentDate"] = treatmentRecord.treatmentDate;

            ViewBag.workerID = new SelectList(db.iCAREWorker, "ID", "profession", treatmentRecord.workerID);
            ViewBag.patientID = new SelectList(db.PatientRecord, "ID", "name", treatmentRecord.patientID);

            // Don't include treatment date in ViewBag as we want to preserve the original
            return View(treatmentRecord);
        }

        // POST: TreatmentRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "treatmentID,description,patientID,workerID")] TreatmentRecord treatmentRecord)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing record from the database
                    var existingRecord = db.TreatmentRecord.Find(treatmentRecord.treatmentID);
                    if (existingRecord == null)
                    {
                        return HttpNotFound();
                    }

                    // Update only the fields that should be modified
                    existingRecord.description = treatmentRecord.description;
                    existingRecord.workerID = treatmentRecord.workerID;
                    existingRecord.patientID = treatmentRecord.patientID;
                    // treatmentDate is not updated, preserving the original value

                    db.Entry(existingRecord).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["Success"] = "Treatment record updated successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the changes: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.workerID = new SelectList(db.iCAREWorker, "ID", "profession", treatmentRecord.workerID);
            ViewBag.patientID = new SelectList(db.PatientRecord, "ID", "name", treatmentRecord.patientID);

            return View(treatmentRecord);
        }
        // GET: TreatmentRecords/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TreatmentRecord treatmentRecord = db.TreatmentRecord.Find(id);
            if (treatmentRecord == null)
            {
                return HttpNotFound();
            }
            return View(treatmentRecord);
        }

        // POST: TreatmentRecords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            TreatmentRecord treatmentRecord = db.TreatmentRecord.Find(id);
            db.TreatmentRecord.Remove(treatmentRecord);
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
