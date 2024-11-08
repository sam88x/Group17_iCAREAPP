// Models/ViewModels/MyBoardViewModel.cs
/* 
 * The MyBoardViewModel class is the viewmodel that manages the data required for the Myboard. 
 * This class contains the Worker, PatientRecords' list, TreatmentRecords' list. 
 */

using System;
using System.Collections.Generic;

namespace Group17_iCAREAPP.Models.ViewModels
{
    public class MyBoardViewModel
    {
        public iCAREWorker Worker { get; set; }
        public List<PatientBoardInfo> Patients { get; set; }
        public bool IsDoctor { get; set; }

        public MyBoardViewModel()
        {
            Patients = new List<PatientBoardInfo>();
        }
    }

    public class PatientBoardInfo
    {
        public PatientRecord Patient { get; set; }
        public TreatmentRecord LastTreatment { get; set; }
        public int DocumentCount { get; set; }
    }
}