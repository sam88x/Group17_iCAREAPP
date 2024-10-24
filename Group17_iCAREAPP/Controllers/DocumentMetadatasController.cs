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
    public class DocumentMetadatasController : Controller
    {
        private Group17_iCAREDBEntities1 db = new Group17_iCAREDBEntities1();

        // GET: DocumentMetadatas
        public ActionResult Index()
        {
            var documentMetadata = db.DocumentMetadata.Include(d => d.iCAREWorker).Include(d => d.PatientRecord);
            return View(documentMetadata.ToList());
        }

        // GET: DocumentMetadatas/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentMetadata documentMetadata = db.DocumentMetadata.Find(id);
            if (documentMetadata == null)
            {
                return HttpNotFound();
            }
            return View(documentMetadata);
        }

        // GET: DocumentMetadatas/Create
        public ActionResult Create()
        {
            ViewBag.userID = new SelectList(db.iCAREWorker, "ID", "profession");
            ViewBag.patientID = new SelectList(db.PatientRecord, "ID", "name");
            return View();
        }

        // POST: DocumentMetadatas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "docID,docName,dateOfCreation,version,userID,patientID")] DocumentMetadata documentMetadata)
        {
            if (ModelState.IsValid)
            {
                db.DocumentMetadata.Add(documentMetadata);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.userID = new SelectList(db.iCAREWorker, "ID", "profession", documentMetadata.userID);
            ViewBag.patientID = new SelectList(db.PatientRecord, "ID", "name", documentMetadata.patientID);
            return View(documentMetadata);
        }

        // GET: DocumentMetadatas/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentMetadata documentMetadata = db.DocumentMetadata.Find(id);
            if (documentMetadata == null)
            {
                return HttpNotFound();
            }
            ViewBag.userID = new SelectList(db.iCAREWorker, "ID", "profession", documentMetadata.userID);
            ViewBag.patientID = new SelectList(db.PatientRecord, "ID", "name", documentMetadata.patientID);
            return View(documentMetadata);
        }

        // POST: DocumentMetadatas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "docID,docName,dateOfCreation,version,userID,patientID")] DocumentMetadata documentMetadata)
        {
            if (ModelState.IsValid)
            {
                db.Entry(documentMetadata).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.userID = new SelectList(db.iCAREWorker, "ID", "profession", documentMetadata.userID);
            ViewBag.patientID = new SelectList(db.PatientRecord, "ID", "name", documentMetadata.patientID);
            return View(documentMetadata);
        }

        // GET: DocumentMetadatas/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentMetadata documentMetadata = db.DocumentMetadata.Find(id);
            if (documentMetadata == null)
            {
                return HttpNotFound();
            }
            return View(documentMetadata);
        }

        // POST: DocumentMetadatas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            DocumentMetadata documentMetadata = db.DocumentMetadata.Find(id);
            db.DocumentMetadata.Remove(documentMetadata);
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
