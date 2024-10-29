using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;
using Group17_iCAREAPP.Models.ViewModels;

namespace Group17_iCAREAPP.Controllers
{
    public class MyBoardController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        [Authorize]
        public ActionResult Index()
        {
            try
            {
                // Get current user from UserPassword table
                var username = User.Identity.Name;
                var userPassword = db.UserPassword
                    .Include("iCAREUser")
                    .FirstOrDefault(u => u.userName == username);

                if (userPassword?.iCAREUser == null)
                {
                    TempData["Error"] = "User profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Get worker profile
                var worker = db.iCAREWorker
                    .Include("UserRole")
                    .Include("iCAREUser")
                    .FirstOrDefault(w => w.ID == userPassword.iCAREUser.ID);

                if (worker == null || worker.UserRole == null)
                {
                    TempData["Error"] = "Access denied. Worker profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if user is Doctor or Nurse based on role ID
                if (worker.UserRole.ID != "DR001" && worker.UserRole.ID != "NR001")
                {
                    TempData["Error"] = "Access denied. Invalid role.";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new MyBoardViewModel
                {
                    Worker = worker,
                    Patients = GetActivePatients(worker.ID),
                    IsDoctor = worker.UserRole.ID == "DR001"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the board.";
                return RedirectToAction("Index", "Home");
            }
        }

        private dynamic GetActivePatients(string workerId)
        {
            try
            {
                // Get all patients this worker has treated
                var patientIds = db.TreatmentRecord
                    .Where(t => t.workerID == workerId)
                    .Select(t => t.patientID)
                    .Distinct()
                    .ToList();

                var patients = db.PatientRecord
                    .Where(p => patientIds.Contains(p.ID))
                    .Select(p => new
                    {
                        Patient = p,
                        LastTreatment = p.TreatmentRecord
                            .OrderByDescending(t => t.treatmentDate)
                            .FirstOrDefault()
                    })
                    .ToList()
                    .Select(x => new PatientBoardInfo
                    {
                        Patient = x.Patient,
                        LastTreatment = x.LastTreatment,
                        DocumentCount = x.Patient.DocumentMetadata.Count
                    })
                    .OrderByDescending(p => p.LastTreatment?.treatmentDate)
                    .ToList();

                return patients;
            }
            catch
            {
                return new List<PatientBoardInfo>();
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
