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
    public class ModificationHistoriesController : Controller
    {
        private Group17_iCAREDBEntities1 db = new Group17_iCAREDBEntities1();

        // GET: ModificationHistories
        public ActionResult Index()
        {
            var modificationHistory = db.ModificationHistory.Include(m => m.DocumentMetadata).Include(m => m.iCAREUser);
            return View(modificationHistory.ToList());
        }

        // GET: ModificationHistories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModificationHistory modificationHistory = db.ModificationHistory.Find(id);
            if (modificationHistory == null)
            {
                return HttpNotFound();
            }
            return View(modificationHistory);
        }

        // GET: ModificationHistories/Create
        public ActionResult Create()
        {
            ViewBag.docID = new SelectList(db.DocumentMetadata, "docID", "docName");
            ViewBag.modifiedByUserID = new SelectList(db.iCAREUser, "ID", "name");
            return View();
        }

        // POST: ModificationHistories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,docID,datOfModification,description,modifiedByUserID")] ModificationHistory modificationHistory)
        {
            if (ModelState.IsValid)
            {
                db.ModificationHistory.Add(modificationHistory);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.docID = new SelectList(db.DocumentMetadata, "docID", "docName", modificationHistory.docID);
            ViewBag.modifiedByUserID = new SelectList(db.iCAREUser, "ID", "name", modificationHistory.modifiedByUserID);
            return View(modificationHistory);
        }

        // GET: ModificationHistories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModificationHistory modificationHistory = db.ModificationHistory.Find(id);
            if (modificationHistory == null)
            {
                return HttpNotFound();
            }
            ViewBag.docID = new SelectList(db.DocumentMetadata, "docID", "docName", modificationHistory.docID);
            ViewBag.modifiedByUserID = new SelectList(db.iCAREUser, "ID", "name", modificationHistory.modifiedByUserID);
            return View(modificationHistory);
        }

        // POST: ModificationHistories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,docID,datOfModification,description,modifiedByUserID")] ModificationHistory modificationHistory)
        {
            if (ModelState.IsValid)
            {
                db.Entry(modificationHistory).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.docID = new SelectList(db.DocumentMetadata, "docID", "docName", modificationHistory.docID);
            ViewBag.modifiedByUserID = new SelectList(db.iCAREUser, "ID", "name", modificationHistory.modifiedByUserID);
            return View(modificationHistory);
        }

        // GET: ModificationHistories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModificationHistory modificationHistory = db.ModificationHistory.Find(id);
            if (modificationHistory == null)
            {
                return HttpNotFound();
            }
            return View(modificationHistory);
        }

        // POST: ModificationHistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ModificationHistory modificationHistory = db.ModificationHistory.Find(id);
            db.ModificationHistory.Remove(modificationHistory);
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
