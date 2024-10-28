using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;

namespace Group17_iCAREAPP.Controllers
{
    public class DisplayMyBoardController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();
        public ActionResult MyBoard(string workerId)
        {

            var patientIds = TreatmentRecord.getPatientIDs(db, workerId);
            Debug.WriteLine("Worker ID: " + workerId);
            Debug.WriteLine("Patient IDs: " + string.Join(", ", patientIds));
            var patientRecords = PatientRecord.getMyPatients(db, patientIds);

            //var patientIds = getPatientIDs(workerId);
            //var patientRecords = getMyPatients(patientIds); //for each -> add


            return View(patientRecords);
        }


        //public List<string> getPatientIDs(string workerId)
        //{
        //    var patientIds = db.TreatmentRecord
        //        .Where(p => p.workerID == workerId)
        //        .Select(p => p.patientID)
        //        .Distinct()
        //        .ToList();

        //    return patientIds;
        //}

        //public List<PatientRecord> getMyPatients(List<string> patientIds)
        //{
        //    if (patientIds == null || !patientIds.Any())
        //    {
        //        return new List<PatientRecord>(); // Return empty list if no IDs
        //    }
        //    var patients = db.PatientRecord
        //        .Where(p => patientIds.Contains(p.ID))
        //        .ToList();

        //    return patients;
        //}


    }
}