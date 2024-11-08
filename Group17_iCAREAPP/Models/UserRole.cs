// The UserRole class is the entity that stores the user's role information.
// It contains a role ID and role name, and references workers who belong to the role through their relationships with multiple iCAREWorkers.
// This class manages a collection of iCAREWorker objects based on their roles.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class UserRole
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public UserRole()
        {
            this.iCAREWorker = new HashSet<iCAREWorker>();
        }
    
        public string ID { get; set; }
        public string roleName { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<iCAREWorker> iCAREWorker { get; set; }
    }
}
