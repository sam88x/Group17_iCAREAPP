// The PatientRecord class is an entity that stores patient information and related data.
// It contains information such as the patient's name, address, date of birth, height, weight, blood type, bed ID, treatment area, geographic unit, and modifier ID.
// This class has collections that reference document metadata and treatment records, and also defines relationships with geocodes and assignment status.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class PatientRecord
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PatientRecord()
        {
            this.DocumentMetadata = new HashSet<DocumentMetadata>();
            this.TreatmentRecord = new HashSet<TreatmentRecord>();
        }
    
        public string ID { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public Nullable<System.DateTime> dateOfBirth { get; set; }
        public Nullable<double> height { get; set; }
        public Nullable<double> weight { get; set; }
        public string bloodGroup { get; set; }
        public string bedID { get; set; }
        public string treatmentArea { get; set; }
        public string geographicalUnit { get; set; }
        public string modifierID { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DocumentMetadata> DocumentMetadata { get; set; }
        public virtual GeoCodes GeoCodes { get; set; }
        public virtual iCAREWorker iCAREWorker { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TreatmentRecord> TreatmentRecord { get; set; }
        public virtual GeoCodes GeoCodes1 { get; set; }
        public virtual PatientAssignmentStatus PatientAssignmentStatus { get; set; }
    }
}
