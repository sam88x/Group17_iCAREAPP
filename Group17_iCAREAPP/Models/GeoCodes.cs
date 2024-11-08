// The GeoCodes class is the entity that manages geographic codes and their descriptions.
// It contains an ID and description for each geocode, and is associated with a patient record through a relationship with PatientRecord.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class GeoCodes
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public GeoCodes()
        {
            this.PatientRecord1 = new HashSet<PatientRecord>();
        }
    
        public string ID { get; set; }
        public string description { get; set; }
    
        public virtual PatientRecord PatientRecord { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PatientRecord> PatientRecord1 { get; set; }
    }
}
