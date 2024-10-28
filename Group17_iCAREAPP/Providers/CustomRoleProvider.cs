using System;
using System.Web.Security;
using System.Linq;
using Group17_iCAREAPP.Models;

namespace Group17_iCAREAPP.Providers
{
    public class CustomRoleProvider : RoleProvider
    {
        public override bool IsUserInRole(string username, string roleName)
        {
            using (var db = new Group17_iCAREDBEntities())
            {
                var userPassword = db.UserPassword
                    .Include("iCAREUser")
                    .Include("iCAREUser.iCAREAdmin")
                    .Include("iCAREUser.iCAREWorker")
                    .FirstOrDefault(u => u.userName == username);

                if (userPassword?.iCAREUser != null)
                {
                    // Check if user is admin
                    if (roleName == "Administrator")
                    {
                        return userPassword.iCAREUser.iCAREAdmin != null;
                    }

                    // Check worker roles
                    var worker = userPassword.iCAREUser.iCAREWorker;
                    if (worker?.UserRole != null)
                    {
                        return worker.UserRole.roleName == roleName;
                    }
                }
                return false;
            }
        }

        public override string[] GetRolesForUser(string username)
        {
            using (var db = new Group17_iCAREDBEntities())
            {
                var userPassword = db.UserPassword
                    .Include("iCAREUser")
                    .Include("iCAREUser.iCAREAdmin")
                    .Include("iCAREUser.iCAREWorker")
                    .FirstOrDefault(u => u.userName == username);

                if (userPassword?.iCAREUser != null)
                {
                    // If user is admin
                    if (userPassword.iCAREUser.iCAREAdmin != null)
                    {
                        return new[] { "Administrator" };
                    }

                    // If user is worker
                    var worker = userPassword.iCAREUser.iCAREWorker;
                    if (worker?.UserRole != null)
                    {
                        return new[] { worker.UserRole.roleName };
                    }
                }
                return new string[] { };
            }
        }

        #region Not Implemented Methods
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override string ApplicationName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}