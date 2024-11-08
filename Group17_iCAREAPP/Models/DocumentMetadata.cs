// The DocumentMetadata class is the entity that manages metadata information about a document.
// It contains the document's ID, name, creation date, version, user ID, and patient ID.
// It tracks the document's modification history through its relationship with ModificationHistory, and manages the document's creator and associated patient information through its relationships with iCAREWorker and PatientRecord.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DocumentMetadata
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DocumentMetadata()
        {
            this.ModificationHistory = new HashSet<ModificationHistory>();
        }
    
        public string docID { get; set; }
        public string docName { get; set; }
        public System.DateTime dateOfCreation { get; set; }
        public Nullable<int> version { get; set; }
        public string userID { get; set; }
        public string patientID { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ModificationHistory> ModificationHistory { get; set; }
        public virtual iCAREWorker iCAREWorker { get; set; }
        public virtual PatientRecord PatientRecord { get; set; }
    }
}
