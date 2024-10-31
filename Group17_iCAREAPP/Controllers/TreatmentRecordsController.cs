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
    public class TreatmentRecordsController : Controller
    {
        private Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // GET: TreatmentRecords
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

        //Checking Assignability
        [HttpPost]
        public JsonResult CheckAssignability(string patientId)
        {
            var user = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);
            var workerID = user.ID;
            var worker = db.iCAREWorker.FirstOrDefault(w => w.ID == workerID);
            var roleName = "";
            roleName = db.UserRole.FirstOrDefault(r => r.ID == worker.userPermission).roleName;

            if (workerID != null)
                ViewBag.workerId = workerID;
            var patientRecord = db.PatientRecord.FirstOrDefault(p => p.ID.ToString() == patientId);
            if (patientRecord == null)
            {
                return Json(new { success = false, message = "Patient not found." ,roleName = roleName});
            }
            bool isAssigned = db.TreatmentRecord.Any(t => t.workerID == worker.ID && t.patientID == patientRecord.ID);
            bool canAssign = false;

            var assignmentStatus = db.PatientAssignmentStatus
                .FirstOrDefault(p => p.PatientRecordID == patientRecord.ID);

            if (roleName == "Doctor")
            {
                if (assignmentStatus.AssignmentStatus != "Assigned" && assignmentStatus.NumOfNurses > 0)
                    canAssign = true;
            }
            else if(roleName == "Nurse")
            {
                if (assignmentStatus == null || assignmentStatus.NumOfNurses < 3) ;
                canAssign = true;
            }

            if(isAssigned)
            {
                return Json(new { success = false, message = "You already assigned.", roleName = roleName });
            }
            if(!canAssign)
            {
                return Json(new { success = false, message = "Unable to assign the patient.", roleName });
            }

            //Save Assign Status
            return Json(new { success = true, message = "You can assign.", roleName = roleName});
        }

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

        [HttpPost]
        //[ValidateAntiForgeryToken]
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

        private void assignDoctor(string patientId)
        {
            var assignmentStatus = db.PatientAssignmentStatus
                .FirstOrDefault(p => p.PatientRecordID == patientId);

            assignmentStatus.AssignmentStatus = "Assigned";
            db.SaveChanges();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AssignPatient([Bind(Include = "treatmentID,description,treatmentDate,patientID,workerID")] TreatmentRecord treatmentRecord, string roleName)
        {
            if (ModelState.IsValid)
            {
                if(roleName == "Nurse")
                {
                    assignNurse(treatmentRecord.patientID);
                }
                else if(roleName == "Doctor")
                {
                    assignDoctor(treatmentRecord.patientID);
                }
                db.TreatmentRecord.Add(treatmentRecord);
                db.SaveChanges();
                //return RedirectToAction("Index");
                return Json(new { success = true, message = "Assign succeeded" });
            }

            //return View(treatmentRecord);
            return Json(new { success = false, message = "Assign failed" });

        }

        // GET: TreatmentRecords/Edit/5
        public ActionResult Edit(string id)
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
            ViewBag.workerID = new SelectList(db.iCAREWorker, "ID", "profession", treatmentRecord.workerID);
            ViewBag.patientID = new SelectList(db.PatientRecord, "ID", "name", treatmentRecord.patientID);
            return View(treatmentRecord);
        }

        // POST: TreatmentRecords/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "treatmentID,description,treatmentDate,patientID,workerID")] TreatmentRecord treatmentRecord)
        {
            if (ModelState.IsValid)
            {
                db.Entry(treatmentRecord).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
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
