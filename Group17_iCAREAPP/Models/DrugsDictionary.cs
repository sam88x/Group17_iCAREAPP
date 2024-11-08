// The DrugsDictionary class is an entity that manages information about drugs.
// It stores data about drugs, including the ID and name of the drug.

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DrugsDictionary
    {
        public string ID { get; set; }
        public string name { get; set; }
    }
}
