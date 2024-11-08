// The iCAREAdmin class is the entity that manages administrator information for the iCARE system.
// It contains the administrator's ID, email, hire date, and retirement date information.
// It also references the administrator user information through its relationship with iCAREUser, and manages the workers that the administrator is affiliated with through its relationship with iCAREWorker.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class iCAREAdmin
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public iCAREAdmin()
        {
            this.iCAREWorker = new HashSet<iCAREWorker>();
        }
    
        public string ID { get; set; }
        public string adminEmail { get; set; }
        public System.DateTime dateHired { get; set; }
        public Nullable<System.DateTime> dateFinished { get; set; }
    
        public virtual iCAREUser iCAREUser { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<iCAREWorker> iCAREWorker { get; set; }
    }
}
