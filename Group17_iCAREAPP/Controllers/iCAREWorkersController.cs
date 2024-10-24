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
    public class iCAREWorkersController : Controller
    {
        private Group17_iCAREDBEntities1 db = new Group17_iCAREDBEntities1();

        // GET: iCAREWorkers
        public ActionResult Index()
        {
            var iCAREWorker = db.iCAREWorker.Include(i => i.iCAREAdmin).Include(i => i.iCAREUser).Include(i => i.UserRole);
            return View(iCAREWorker.ToList());
        }

        // GET: iCAREWorkers/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            iCAREWorker iCAREWorker = db.iCAREWorker.Find(id);
            if (iCAREWorker == null)
            {
                return HttpNotFound();
            }
            return View(iCAREWorker);
        }

        // GET: iCAREWorkers/Create
        public ActionResult Create()
        {
            ViewBag.creator = new SelectList(db.iCAREAdmin, "ID", "adminEmail");
            ViewBag.ID = new SelectList(db.iCAREUser, "ID", "name");
            ViewBag.userPermission = new SelectList(db.UserRole, "ID", "roleName");
            return View();
        }

        // POST: iCAREWorkers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,profession,creator,userPermission")] iCAREWorker iCAREWorker)
        {
            if (ModelState.IsValid)
            {
                db.iCAREWorker.Add(iCAREWorker);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.creator = new SelectList(db.iCAREAdmin, "ID", "adminEmail", iCAREWorker.creator);
            ViewBag.ID = new SelectList(db.iCAREUser, "ID", "name", iCAREWorker.ID);
            ViewBag.userPermission = new SelectList(db.UserRole, "ID", "roleName", iCAREWorker.userPermission);
            return View(iCAREWorker);
        }

        // GET: iCAREWorkers/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            iCAREWorker iCAREWorker = db.iCAREWorker.Find(id);
            if (iCAREWorker == null)
            {
                return HttpNotFound();
            }
            ViewBag.creator = new SelectList(db.iCAREAdmin, "ID", "adminEmail", iCAREWorker.creator);
            ViewBag.ID = new SelectList(db.iCAREUser, "ID", "name", iCAREWorker.ID);
            ViewBag.userPermission = new SelectList(db.UserRole, "ID", "roleName", iCAREWorker.userPermission);
            return View(iCAREWorker);
        }

        // POST: iCAREWorkers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,profession,creator,userPermission")] iCAREWorker iCAREWorker)
        {
            if (ModelState.IsValid)
            {
                db.Entry(iCAREWorker).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.creator = new SelectList(db.iCAREAdmin, "ID", "adminEmail", iCAREWorker.creator);
            ViewBag.ID = new SelectList(db.iCAREUser, "ID", "name", iCAREWorker.ID);
            ViewBag.userPermission = new SelectList(db.UserRole, "ID", "roleName", iCAREWorker.userPermission);
            return View(iCAREWorker);
        }

        // GET: iCAREWorkers/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            iCAREWorker iCAREWorker = db.iCAREWorker.Find(id);
            if (iCAREWorker == null)
            {
                return HttpNotFound();
            }
            return View(iCAREWorker);
        }

        // POST: iCAREWorkers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            iCAREWorker iCAREWorker = db.iCAREWorker.Find(id);
            db.iCAREWorker.Remove(iCAREWorker);
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
