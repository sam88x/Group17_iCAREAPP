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
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        public ActionResult ManageUsers()
        {
            try
            {
                var users = db.iCAREUser
                    .Include(u => u.iCAREWorker)
                    .Include(u => u.iCAREAdmin)
                    .Include(u => u.UserPassword)
                    .Include(u => u.iCAREWorker.UserRole)
                    .Select(u => new UserManagementViewModel
                    {
                        ID = u.ID,
                        Name = u.name ?? "N/A",
                        Username = u.UserPassword != null ? u.UserPassword.userName : "N/A",
                        UserType = u.iCAREAdmin != null ? "Administrator" :
                                 u.iCAREWorker != null ? u.iCAREWorker.profession : "Unknown",
                        Role = u.iCAREWorker != null && u.iCAREWorker.UserRole != null ?
                              u.iCAREWorker.UserRole.roleName : "N/A",
                        AccountStatus = u.UserPassword != null &&
                                      u.UserPassword.userAccountExpiryDate <= DateTime.Now ?
                                      "Inactive" : "Active",
                        AccountExpiryDate = u.UserPassword != null ?
                                          u.UserPassword.userAccountExpiryDate : null
                    })
                    .ToList();

                return View(users);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ManageUsers: {ex.Message}");
                TempData["Error"] = "An error occurred while loading users.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Displays the form for creating a new user
        /// </summary>
        [HttpGet]
        public ActionResult CreateUser()
        {
            try
            {
                var model = new CreateUserViewModel();
                // Prepare dropdown for professions
                ViewBag.Professions = new SelectList(new[]
                {
            new { Value = "Doctor", Text = "Doctor" },
            new { Value = "Nurse", Text = "Nurse" }
        }, "Value", "Text");

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateUser GET: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the create user form.";
                return RedirectToAction("ManageUsers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(CreateUserViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("CreateUser POST started");

            ModelState.Remove("UserType");

            if (!ModelState.IsValid)
            {
                PrepareViewBagForCreateUser();
                return View(model);
            }

            using (var dbContextTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    // First, verify that we have a valid admin ID
                    var currentUsername = User.Identity.Name;
                    var adminUser = db.iCAREAdmin.FirstOrDefault();

                    if (adminUser == null)
                    {
                        System.Diagnostics.Debug.WriteLine("No admin found in the system");
                        ModelState.AddModelError("", "System configuration error: No admin found.");
                        PrepareViewBagForCreateUser();
                        return View(model);
                    }

                    System.Diagnostics.Debug.WriteLine($"Using admin ID: {adminUser.ID}");

                    // Check username uniqueness
                    if (db.UserPassword.Any(u => u.userName == model.Username))
                    {
                        ModelState.AddModelError("Username", "Username already exists.");
                        PrepareViewBagForCreateUser();
                        return View(model);
                    }

                    var userId = Guid.NewGuid().ToString();
                    System.Diagnostics.Debug.WriteLine($"Generated User ID: {userId}");

                    // 1. Create iCAREUser
                    var iCAREUser = new iCAREUser
                    {
                        ID = userId,
                        name = model.Name
                    };
                    db.iCAREUser.Add(iCAREUser);
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("Created iCAREUser successfully");

                    // 2. Create UserPassword
                    var userPassword = new UserPassword
                    {
                        ID = userId,
                        userName = model.Username,
                        encryptedPassword = HashPassword(model.Password),
                        passwordExpiryTime = 90,
                        userAccountExpiryDate = model.AccountExpiryDate
                    };
                    db.UserPassword.Add(userPassword);
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("Created UserPassword successfully");

                    // 3. Create iCAREWorker with valid admin reference
                    if (model.Profession == "Doctor" || model.Profession == "Nurse")
                    {
                        var roleId = model.Profession == "Doctor" ? "DR001" : "NR001";

                        var worker = new iCAREWorker
                        {
                            ID = userId,
                            profession = model.Profession,
                            creator = adminUser.ID,  // Use the verified admin ID
                            userPermission = roleId,
                            iCAREAdmin = adminUser  // Set the navigation property
                        };
                        db.iCAREWorker.Add(worker);
                        db.SaveChanges();
                        System.Diagnostics.Debug.WriteLine("Created iCAREWorker successfully");
                    }

                    dbContextTransaction.Commit();
                    System.Diagnostics.Debug.WriteLine("Transaction committed successfully");

                    TempData["Success"] = $"User {model.Name} ({model.Profession}) created successfully.";
                    return RedirectToAction("ManageUsers");
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    LogException("Error occurred", ex);
                    ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
                    PrepareViewBagForCreateUser();
                    return View(model);
                }
            }
        }

        private void LogException(string context, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{context}:");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");

            if (ex is DbUpdateException dbEx && dbEx.InnerException != null)
            {
                var innerEx = dbEx.InnerException;
                while (innerEx != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                }
            }
        }

        private void PrepareViewBagForCreateUser()
        {
            ViewBag.Professions = new SelectList(new[]
            {
        new { Value = "Doctor", Text = "Doctor" },
        new { Value = "Nurse", Text = "Nurse" }
    }, "Value", "Text");
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

    public class UserManagementViewModel
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string UserType { get; set; }
        public string Role { get; set; }
        public string AccountStatus { get; set; }
        public DateTime? AccountExpiryDate { get; set; }
    }
}

