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
    public class DrugsDictionariesController : Controller
    {
        private Group17_iCAREDBEntities1 db = new Group17_iCAREDBEntities1();

        // GET: DrugsDictionaries
        public ActionResult Index()
        {
            return View(db.DrugsDictionary.ToList());
        }

        // GET: DrugsDictionaries/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DrugsDictionary drugsDictionary = db.DrugsDictionary.Find(id);
            if (drugsDictionary == null)
            {
                return HttpNotFound();
            }
            return View(drugsDictionary);
        }

        // GET: DrugsDictionaries/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DrugsDictionaries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,name")] DrugsDictionary drugsDictionary)
        {
            if (ModelState.IsValid)
            {
                db.DrugsDictionary.Add(drugsDictionary);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(drugsDictionary);
        }

        // GET: DrugsDictionaries/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DrugsDictionary drugsDictionary = db.DrugsDictionary.Find(id);
            if (drugsDictionary == null)
            {
                return HttpNotFound();
            }
            return View(drugsDictionary);
        }

        // POST: DrugsDictionaries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,name")] DrugsDictionary drugsDictionary)
        {
            if (ModelState.IsValid)
            {
                db.Entry(drugsDictionary).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(drugsDictionary);
        }

        // GET: DrugsDictionaries/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DrugsDictionary drugsDictionary = db.DrugsDictionary.Find(id);
            if (drugsDictionary == null)
            {
                return HttpNotFound();
            }
            return View(drugsDictionary);
        }

        // POST: DrugsDictionaries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            DrugsDictionary drugsDictionary = db.DrugsDictionary.Find(id);
            db.DrugsDictionary.Remove(drugsDictionary);
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
