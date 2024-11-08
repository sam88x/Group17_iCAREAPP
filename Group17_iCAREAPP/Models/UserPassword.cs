// The UserPassword class is an entity that stores the password and related information for a user account.
// Contains information such as user ID, username, encrypted password, password expiration time, account expiration date, etc.
// It references user account information through its relationship with iCAREUser.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class UserPassword
    {
        public string ID { get; set; }
        public string userName { get; set; }
        public string encryptedPassword { get; set; }
        public Nullable<int> passwordExpiryTime { get; set; }
        public Nullable<System.DateTime> userAccountExpiryDate { get; set; }
    
        public virtual iCAREUser iCAREUser { get; set; }
    }
}
