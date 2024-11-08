//------------------------------------------------------------------------------
// The ModificationHistory class is an entity that tracks the history of document modifications.
// Each modification history contains information such as the date of the modification, a description of the modification, and the user ID that made the modification.
// It references the documents and users involved through virtual relationships with DocumentMetadata and iCAREUser.
//------------------------------------------------------------------------------

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ModificationHistory
    {
        public int ID { get; set; }
        public string docID { get; set; }
        public System.DateTime datOfModification { get; set; }
        public string description { get; set; }
        public string modifiedByUserID { get; set; }
    
        public virtual DocumentMetadata DocumentMetadata { get; set; }
        public virtual iCAREUser iCAREUser { get; set; }
    }
}
