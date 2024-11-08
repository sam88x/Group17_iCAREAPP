// The iCAREUser class is the entity that manages the user's information within the iCARE system.
// It contains the user's ID and name, and establishes relationships with iCAREAdmin, iCAREWorker, and UserPassword.
// It also tracks the user's modification history through a relationship with ModificationHistory.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class iCAREUser
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public iCAREUser()
        {
            this.ModificationHistory = new HashSet<ModificationHistory>();
        }
    
        public string ID { get; set; }
        public string name { get; set; }
    
        public virtual iCAREAdmin iCAREAdmin { get; set; }
        public virtual iCAREWorker iCAREWorker { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ModificationHistory> ModificationHistory { get; set; }
        public virtual UserPassword UserPassword { get; set; }
    }
}
