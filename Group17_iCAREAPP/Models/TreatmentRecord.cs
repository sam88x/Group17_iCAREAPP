// The TreatmentRecord class is an entity that stores a record of the treatment provided to a specific patient.
// Contains information such as treatment ID, description, treatment date, patient ID, worker ID, etc.
// It references treatment provider and patient record information through its relationship with iCAREWorker and PatientRecord.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class TreatmentRecord
    {
        public string treatmentID { get; set; }
        public string description { get; set; }
        public System.DateTime treatmentDate { get; set; }
        public string patientID { get; set; }
        public string workerID { get; set; }
    
        public virtual iCAREWorker iCAREWorker { get; set; }
        public virtual PatientRecord PatientRecord { get; set; }
    }
}
