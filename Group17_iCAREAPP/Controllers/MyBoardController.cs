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

        // GET: MyBoard/Index
        // Displays personalized dashboard for authenticated medical staff (doctors/nurses)
        // Validates user's role and permissions
        // Retrieves worker profile and their assigned patients
        // Includes error handling for invalid/missing profiles
        // Returns: Dashboard view with MyBoardViewModel or redirects to home with error
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

                // Both the worker and their role must be present
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

                // Creates instance of view model to make view simple to manage
                var viewModel = new MyBoardViewModel
                {
                    Worker = worker,
                    Patients = GetActivePatients(worker.ID),
                    IsDoctor = worker.UserRole.ID == "DR001" // Checks if the id matches that of a doctor
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the board.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Private utility method to retrieve active patients for a worker
        // Gets all patients the worker has treated, ordered by most recent treatment
        // Includes treatment history and document count for each patient
        // Parameters:
        //   workerId: ID of the worker whose patients to retrieve
        // Returns: List of PatientBoardInfo objects with patient details and metrics
        // Falls back to empty list on error
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

                // Collects list of patient records from the treatment records
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
                        LastTreatment = x.LastTreatment, // Keeps track of each patient's latest treatment
                        DocumentCount = x.Patient.DocumentMetadata.Count // Keeps track of how many documents are for a patient
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

        // Implements proper disposal of database context
        // Ensures database connections are properly closed
        // Parameters:
        //   disposing: Boolean indicating if managed resources should be disposed
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
