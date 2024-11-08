// File was tested, but ended up not being used

using System;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;
using System.Threading.Tasks;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;

namespace Group17_iCAREAPP.Controllers
{
    public class TreatmentRecordController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // Class that aligns patients and workers
        public class AssignPatientRequest
        {
            [Required]
            [Display(Name = "Worker ID")]
            public string WorkerId { get; set; }

            [Required]
            [Display(Name = "Patient ID")]
            public string PatientId { get; set; }

            [Required]
            [Display(Name = "Description")]
            public string Description { get; set; }
        }

        // POST: TreatmentRecord/AssignPatient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AssignPatient(AssignPatientRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var worker = await db.iCAREWorker.FindAsync(model.WorkerId);
                    if (worker == null)
                    {
                        ModelState.AddModelError("", "Worker not found.");
                        return View(model);
                    }

                    var patient = await db.PatientRecord.FindAsync(model.PatientId);
                    if (patient == null)
                    {
                        ModelState.AddModelError("", "Patient not found.");
                        return View(model);
                    }

                    var treatmentId = "TREAT-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                    var treatmentRecord = new TreatmentRecord
                    {
                        treatmentID = treatmentId,
                        description = model.Description,
                        workerID = model.WorkerId,
                        patientID = model.PatientId,
                        treatmentDate = DateTime.Now
                    };

                    db.TreatmentRecord.Add(treatmentRecord);
                    await db.SaveChangesAsync();

                    TempData["Success"] = $"Patient {model.PatientId} successfully assigned.";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while processing your request: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: TreatmentRecord/AssignPatient
        public ActionResult AssignPatient()
        {
            return View();
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