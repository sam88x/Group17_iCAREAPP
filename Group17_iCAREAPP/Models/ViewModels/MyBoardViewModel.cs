// Models/ViewModels/MyBoardViewModel.cs
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