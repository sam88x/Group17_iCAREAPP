using System;
using System.Linq;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web;

namespace Group17_iCAREAPP.Controllers
{
    public class iCAREBoardController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // GET: iCAREBoard
        public ActionResult Index()
        {
            try
            {
                var geoCodes = db.GeoCodes
                    .Select(g => new GeoCodeViewModel
                    {
                        ID = g.ID,
                        Description = g.description
                    })
                    .Distinct()
                    .ToList();

                if (!geoCodes.Any())
                {
                    TempData["Info"] = "No geographical units found.";
                }

                return View(geoCodes);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading geographical units.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: iCAREBoard/DisplayByGeoCode/[geoCodeId]
        [HttpGet]
        public ActionResult DisplayByGeoCode(string geoCodeId)
        {
            try
            {
                if (string.IsNullOrEmpty(geoCodeId))
                {
                    TempData["Error"] = "Geographical code is required.";
                    return RedirectToAction("Index");
                }

                var geoCode = db.GeoCodes.FirstOrDefault(g => g.ID == geoCodeId);
                if (geoCode == null)
                {
                    TempData["Error"] = "Invalid geographical code.";
                    return RedirectToAction("Index");
                }

                var patientRecords = db.PatientRecord
                    .Include("iCAREWorker")
                    .Where(pr => pr.geographicalUnit == geoCodeId)
                    .ToList();

                if (!patientRecords.Any())
                {
                    TempData["Info"] = "No patient records found for the specified geographical area.";
                }

                ViewBag.GeoCodeId = geoCodeId;
                ViewBag.GeoCodeDescription = geoCode.description;
                return View(patientRecords);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while retrieving patient records.";
                return RedirectToAction("Index");
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

    public class GeoCodeViewModel
    {
        public string ID { get; set; }
        public string Description { get; set; }
    }
}