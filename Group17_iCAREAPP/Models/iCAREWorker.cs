// The iCAREWorker class is the entity that manages the information of a worker (nurse, doctor, etc.) in the iCARE system.
// It contains the worker's ID, profession, creator, user permissions, etc. and defines relationships with various document metadata, patient records, and treatment records.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class iCAREWorker
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public iCAREWorker()
        {
            this.DocumentMetadata = new HashSet<DocumentMetadata>();
            this.PatientRecord = new HashSet<PatientRecord>();
            this.TreatmentRecord = new HashSet<TreatmentRecord>();
        }
    
        public string ID { get; set; }
        public string profession { get; set; }
        public string creator { get; set; }
        public string userPermission { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DocumentMetadata> DocumentMetadata { get; set; }
        public virtual iCAREAdmin iCAREAdmin { get; set; }
        public virtual iCAREUser iCAREUser { get; set; }
        public virtual UserRole UserRole { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PatientRecord> PatientRecord { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TreatmentRecord> TreatmentRecord { get; set; }
    }
}
