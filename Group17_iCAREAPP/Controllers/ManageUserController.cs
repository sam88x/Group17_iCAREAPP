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
    // Must be an administrator to manage the users of the system
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // GET: Admin/ManageUsers
        // Retrieves and displays list of all system users with their roles and account status
        // Includes related data: worker info, admin status, passwords, and roles
        // Transforms data into UserManagementViewModel for display
        // Returns: View with list of users or redirects to home with error message
        public ActionResult ManageUsers()
        {
            try
            {
                // Collects all the infomation about the user and their login info
                var users = db.iCAREUser
                    .Include(u => u.iCAREWorker)
                    .Include(u => u.iCAREAdmin)
                    .Include(u => u.UserPassword)
                    .Include(u => u.iCAREWorker.UserRole)
                    .Select(u => new UserManagementViewModel
                    {
                        ID = u.ID,
                        Name = u.name ?? "N/A", // If not in database, N/A
                        Username = u.UserPassword != null ? u.UserPassword.userName : "N/A", // If not in database, N/A
                        UserType = u.iCAREAdmin != null ? "Administrator" : // User is adminstrator if in admin table
                                 u.iCAREWorker != null ? u.iCAREWorker.profession : "Unknown", // Profession is similar to role, but typed out
                        Role = u.iCAREWorker != null && u.iCAREWorker.UserRole != null ?
                              u.iCAREWorker.UserRole.roleName : "N/A",
                        AccountStatus = u.UserPassword != null &&
                                      u.UserPassword.userAccountExpiryDate <= DateTime.Now ?
                                      "Inactive" : "Active", // Keeps track of whether the time has expired on the acount
                        AccountExpiryDate = u.UserPassword != null ?
                                          u.UserPassword.userAccountExpiryDate : null
                    })
                    .ToList(); // Creates list of all users in system

                return View(users);
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error in ManageUsers: {ex.Message}");
                TempData["Error"] = "An error occurred while loading users.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Admin/CreateUser
        // Displays form for creating new system user
        // Prepares dropdown list of available professions (Doctor/Nurse)
        // Returns: Create view with empty CreateUserViewModel or redirects with error
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

        // POST: Admin/CreateUser
        // Processes creation of new system user with associated roles and permissions
        // Creates entries in multiple tables: iCAREUser, UserPassword, iCAREWorker
        // Uses transaction to ensure data consistency across tables
        // Parameters:
        //   model: CreateUserViewModel containing new user details
        // Returns: Redirects to ManageUsers on success or returns to view with errors
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(CreateUserViewModel model)
        {
            // System.Diagnostics.Debug.WriteLine("CreateUser POST started");

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
                    // Verify that we have a valid admin ID
                    var currentUsername = User.Identity.Name;
                    var adminUser = db.iCAREAdmin.FirstOrDefault();

                    if (adminUser == null)
                    {
                        // System.Diagnostics.Debug.WriteLine("No admin found in the system");
                        // Error if the admin is not found
                        ModelState.AddModelError("", "System configuration error: No admin found.");
                        PrepareViewBagForCreateUser();
                        return View(model);
                    }

                    //System.Diagnostics.Debug.WriteLine($"Using admin ID: {adminUser.ID}");

                    // Check username uniqueness
                    if (db.UserPassword.Any(u => u.userName == model.Username))
                    {
                        // ModelState.AddModelError("Username", "Username already exists.");
                        // Prompts user again
                        PrepareViewBagForCreateUser();
                        return View(model);
                    }

                    var userId = Guid.NewGuid().ToString(); // Creates unqiue identifier code
                    // System.Diagnostics.Debug.WriteLine($"Generated User ID: {userId}");

                    // Create iCAREUser
                    var iCAREUser = new iCAREUser
                    {
                        ID = userId,
                        name = model.Name
                    };
                    db.iCAREUser.Add(iCAREUser);
                    db.SaveChanges();
                    // System.Diagnostics.Debug.WriteLine("Created iCAREUser successfully");

                    // Create UserPassword
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

                    // Create iCAREWorker with valid admin reference
                    if (model.Profession == "Doctor" || model.Profession == "Nurse")
                    {
                        // Uses specific ids from the database to assign roles
                        var roleId = model.Profession == "Doctor" ? "DR001" : "NR001";

                        var worker = new iCAREWorker
                        {
                            ID = userId,
                            profession = model.Profession,
                            creator = adminUser.ID,  // Use the verified admin ID
                            userPermission = roleId,
                            iCAREAdmin = adminUser
                        };
                        db.iCAREWorker.Add(worker); // Worker added to the database
                        db.SaveChanges();
                        // System.Diagnostics.Debug.WriteLine("Created iCAREWorker successfully");
                    }

                    dbContextTransaction.Commit();
                    // System.Diagnostics.Debug.WriteLine("Transaction committed successfully");

                    // Success message and goes to list of users
                    TempData["Success"] = $"User {model.Name} ({model.Profession}) created successfully.";
                    return RedirectToAction("ManageUsers");
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    // LogException("Error occurred", ex);
                    ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
                    // If there is an error, will reprompt user for another entry into the form
                    PrepareViewBagForCreateUser();
                    return View(model);
                }
            }
        }

        // Used specifically for debugging
        //private void LogException(string context, Exception ex)
        //{
        //    System.Diagnostics.Debug.WriteLine($"{context}:");
        //    System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");

        //    if (ex is DbUpdateException dbEx && dbEx.InnerException != null)
        //    {
        //        var innerEx = dbEx.InnerException;
        //        while (innerEx != null)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"Inner Exception: {innerEx.Message}");
        //            innerEx = innerEx.InnerException;
        //        }
        //    }
        //}

        // Private utility method to prepare profession dropdown list
        // Sets up ViewBag.Professions with Doctor and Nurse options
        // Used by both Create and Edit views
        private void PrepareViewBagForCreateUser()
        {
            ViewBag.Professions = new SelectList(new[]
            {
                new { Value = "Doctor", Text = "Doctor" },
                new { Value = "Nurse", Text = "Nurse" }
            }, "Value", "Text");
        }


        // GET: Admin/EditUser
        // Loads existing user data for editing
        // Includes related worker and password information
        // Parameters:
        //   id: User ID to edit
        // Returns: Edit view with populated EditUserViewModel or error response
        [HttpGet]
        public ActionResult EditUser(string id)
        {
            // System.Diagnostics.Debug.WriteLine($"Edit User ID: {id}");

            // If the user id passed is not present, throws an exception
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            // Looks for the user of interest
            var user = db.iCAREUser
                .Include(u => u.iCAREWorker)
                .Include(u => u.UserPassword)
                .FirstOrDefault(u => u.ID == id);

            // Checks that the user is in the database
            if (user == null)
            {
                // System.Diagnostics.Debug.WriteLine("User not found");  // Debug log
                return HttpNotFound();
            }

            // Creates view model for easier management of viewing the users
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

            // System.Diagnostics.Debug.WriteLine($"Model created - Name: {model.Name}, Profession: {model.Profession}");

            return View(model);
        }

        // POST: Admin/EditUser
        // Processes updates to existing user information
        // Updates user details, profession, and optionally changes password
        // Parameters:
        //   model: EditUserViewModel containing updated user information
        // Returns: Redirects to ManageUsers on success or returns to view with errors
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Very similar to above
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
                        // Saves changes and returns to the list of workers
                        db.SaveChanges();
                        TempData["Success"] = "User updated successfully.";
                        return RedirectToAction("ManageUsers");
                    }
                    catch (Exception ex)
                    {
                        // System.Diagnostics.Debug.WriteLine($"Error updating user: {ex.Message}");
                        ModelState.AddModelError("", "An error occurred while saving changes.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "User not found.");
                }
            }

            // Something went wrong and need to redisplay form
            ViewBag.Professions = new SelectList(new[]
            {
                new { Value = "Doctor", Text = "Doctor" },
                new { Value = "Nurse", Text = "Nurse" }
            }, "Value", "Text", model.Profession);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        // POST: Admin/DeactivateUser
        // Deactivates user account by setting expiration date to current time
        // Parameters:
        //   id: ID of user to deactivate
        // Returns: Redirects to ManageUsers
        public ActionResult DeactivateUser(string id)
        {
            var userPassword = db.UserPassword.Find(id);
            if (userPassword != null)
            {
                // Simply changes the expiration date to their account to right now, so it expires
                userPassword.userAccountExpiryDate = DateTime.Now;
                db.SaveChanges();
            }
            return RedirectToAction("ManageUsers");
        }

        // Private utility method for password hashing
        // Implements SHA-256 encryption for secure password storage
        // Parameters:
        //   password: Plain text password to hash
        // Returns: Base64 encoded hash string
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
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

    // View model to help with creation
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
    
    // Edit view model to help with the editing process
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

    // View model for displaying all the workers in the final table
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

