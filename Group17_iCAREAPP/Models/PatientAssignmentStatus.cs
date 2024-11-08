// The PatientAssignmentStatus class represents the assignment status information for a specific patient record.
// Includes the patient record ID, assignment status, number of nurses assigned, etc.
// It references patient record information through a virtual relationship with PatientRecord.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class PatientAssignmentStatus
    {
        public string PatientRecordID { get; set; }
        public string AssignmentStatus { get; set; }
        public int NumOfNurses { get; set; }
    
        public virtual PatientRecord PatientRecord { get; set; }
    }
}
