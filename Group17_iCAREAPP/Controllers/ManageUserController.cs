using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;
using System.Security.Cryptography;
using System.Text;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Infrastructure;

namespace Group17_iCAREAPP.Controllers
{
    /// <summary>
    /// Controller for handling all administrator functionality related to user management
    /// Only administrators can access these actions
    /// </summary>
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        /// <summary>
        /// Displays a list of all users in the system
        /// </summary>
        public ActionResult ManageUsers()
        {
            // Get all users with their related information
            var users = db.iCAREUser
                .Include(u => u.iCAREWorker)
                .Include(u => u.iCAREAdmin)
                .Include(u => u.UserPassword)
                .Include(u => u.iCAREWorker.UserRole)
                .ToList();

            return View(users);
        }

        /// <summary>
        /// Displays the form for creating a new user
        /// </summary>
        [HttpGet]
        public ActionResult CreateUser()
        {
            // Prepare dropdown for user types
            ViewBag.UserTypes = new SelectList(new[]
            {
        new { Value = "Admin", Text = "Administrator" },
        new { Value = "Doctor", Text = "Doctor" },
        new { Value = "Nurse", Text = "Nurse" }
    }, "Value", "Text");

            // Prepare dropdown for worker roles (will be used for doctors and nurses)
            ViewBag.Roles = new SelectList(db.UserRole, "ID", "roleName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(FormCollection form)
        {
            string name = form["name"];
            string userName = form["userName"];
            string password = form["password"];
            string profession = form["profession"];
            string accountExpiryDate = form["accountExpiryDate"];

            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(userName) ||
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(profession))
                {
                    ModelState.AddModelError("", "All fields except expiry date are required.");
                    return View();
                }

                // Check username
                if (db.UserPassword.Any(u => u.userName == userName))
                {
                    ModelState.AddModelError("", "Username already exists.");
                    return View();
                }

                // Get role ID
                string roleId;
                if (profession == "Doctor")
                {
                    roleId = "DR001";
                }
                else if (profession == "Nurse")
                {
                    roleId = "NR001";
                }
                else
                {
                    ModelState.AddModelError("", "Invalid profession selected.");
                    return View();
                }

                var userId = Guid.NewGuid().ToString();
                System.Diagnostics.Debug.WriteLine($"Generated User ID: {userId}");

                try
                {
                    // Create base user
                    var user = new iCAREUser
                    {
                        ID = userId,
                        name = name
                    };
                    db.iCAREUser.Add(user);
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("Created iCAREUser successfully");

                    // Create password
                    var userPassword = new UserPassword
                    {
                        ID = userId,
                        userName = userName,
                        encryptedPassword = HashPassword(password),
                        passwordExpiryTime = 90
                    };

                    if (!string.IsNullOrEmpty(accountExpiryDate))
                    {
                        if (DateTime.TryParse(accountExpiryDate, out DateTime expiryDate))
                        {
                            userPassword.userAccountExpiryDate = expiryDate;
                        }
                    }
                    db.UserPassword.Add(userPassword);
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("Created UserPassword successfully");

                    // Create worker with known admin ID
                    var worker = new iCAREWorker
                    {
                        ID = userId,
                        profession = profession,
                        creator = "0001",  // Using the known admin ID
                        userPermission = roleId
                    };
                    db.iCAREWorker.Add(worker);
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("Created iCAREWorker successfully");

                    TempData["Success"] = $"User {name} ({profession}) created successfully.";
                    return RedirectToAction("ManageUsers");
                }
                catch (DbUpdateException ex)
                {
                    var innerException = ex.InnerException;
                    while (innerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner Exception: {innerException.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack Trace: {innerException.StackTrace}");
                        innerException = innerException.InnerException;
                    }

                    // Cleanup if needed
                    var createdUser = db.iCAREUser.Find(userId);
                    if (createdUser != null)
                    {
                        db.iCAREUser.Remove(createdUser);
                    }

                    var createdPassword = db.UserPassword.Find(userId);
                    if (createdPassword != null)
                    {
                        db.UserPassword.Remove(createdPassword);
                    }

                    try
                    {
                        db.SaveChanges();
                    }
                    catch { /* Ignore cleanup errors */ }

                    ModelState.AddModelError("", "Error creating user. Please try again.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Outer Exception: {ex.Message}");
                ModelState.AddModelError("", "An unexpected error occurred.");
            }

            return View();
        }


        [HttpGet]
        public ActionResult EditUser(string id)
        {
            System.Diagnostics.Debug.WriteLine($"Edit User ID: {id}");  // Debug log

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var user = db.iCAREUser
                .Include(u => u.iCAREWorker)
                .Include(u => u.UserPassword)
                .FirstOrDefault(u => u.ID == id);

            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("User not found");  // Debug log
                return HttpNotFound();
            }

            var model = new EditUserViewModel
            {
                ID = user.ID,
                Name = user.name,
                Profession = user.iCAREWorker?.profession,
                UserRole = user.iCAREWorker?.userPermission
            };

            // Set up the profession dropdown
            ViewBag.Professions = new SelectList(new[]
            {
        new { Value = "Doctor", Text = "Doctor" },
        new { Value = "Nurse", Text = "Nurse" }
    }, "Value", "Text", model.Profession);

            // Debug log
            System.Diagnostics.Debug.WriteLine($"Model created - Name: {model.Name}, Profession: {model.Profession}");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.iCAREUser
                    .Include(u => u.iCAREWorker)
                    .Include(u => u.UserPassword)
                    .FirstOrDefault(u => u.ID == model.ID);

                if (user != null)
                {
                    user.name = model.Name;

                    if (user.iCAREWorker != null)
                    {
                        user.iCAREWorker.profession = model.Profession;
                        // Only update userPermission if the profession changed
                        if (model.Profession == "Doctor")
                        {
                            user.iCAREWorker.userPermission = "DR001";
                        }
                        else if (model.Profession == "Nurse")
                        {
                            user.iCAREWorker.userPermission = "NR001";
                        }
                    }

                    // Handle password change if provided
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        var userPassword = user.UserPassword;
                        if (userPassword != null)
                        {
                            userPassword.encryptedPassword = HashPassword(model.NewPassword);
                        }
                    }

                    try
                    {
                        db.SaveChanges();
                        TempData["Success"] = "User updated successfully.";
                        return RedirectToAction("ManageUsers");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating user: {ex.Message}");
                        ModelState.AddModelError("", "An error occurred while saving changes.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "User not found.");
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Professions = new SelectList(new[]
            {
                new { Value = "Doctor", Text = "Doctor" },
                new { Value = "Nurse", Text = "Nurse" }
            }, "Value", "Text", model.Profession);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeactivateUser(string id)
        {
            var userPassword = db.UserPassword.Find(id);
            if (userPassword != null)
            {
                userPassword.userAccountExpiryDate = DateTime.Now;
                db.SaveChanges();
            }
            return RedirectToAction("ManageUsers");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
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

    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "User Type")]
        public string UserType { get; set; }

        // Worker-specific fields
        [Display(Name = "Profession")]
        public string Profession { get; set; }

        [Display(Name = "Role")]
        public string UserRole { get; set; }

        // Admin-specific fields
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Account Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? AccountExpiryDate { get; set; }
    }

    public class EditUserViewModel
    {
        public string ID { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        public bool IsAdmin { get; set; }

        // Worker-specific fields
        [Display(Name = "Profession")]
        public string Profession { get; set; }

        [Display(Name = "Role")]
        public string UserRole { get; set; }

        // Admin-specific fields
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }
    }
}