using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplicationSampleTest2.Models
{
    public class OPD
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int AppointmentId { get; set; }
        public string BP { get; set; }
        public string Pulse { get; set; }
        public string Investigation { get; set; }
        public string ReportDetail { get; set; }
        public string ReportFilePath { get; set; }  // DB sathi path
        [NotMapped]
        public IFormFile ReportFile { get; set; }   // Upload sathi
        public DateTime? NextAppointmentDate { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public DateTime AppointmentDate { get; set; }

        public List<string> Symptom { get; set; } = new List<string>();

        public List<int> Symptoms { get; set; } = new List<int>();
        // changed to use OPDMedicine (string flags for morning/afternoon/evening)
        public List<OPDMedicine> Medicines { get; set; } = new List<OPDMedicine>();
    }
    public class OPDMedicineVM
    {
        public string MedicineName { get; set; }
        public int MedicineId { get; set; }
        public int Morning { get; set; }
        public int Afternoon { get; set; }
        public int Evening { get; set; }
        public int Days { get; set; }
    }


    public class OPDMedicine
    {
        public string MedicineName { get; set; }
        public int MedicineId { get; set; }
        public string Morning { get; set; }
        public string Afternoon { get; set; }
        public string Evening { get; set; }
        public int Days { get; set; }
    }

    public class OPDDetailVM
    {
        public OPD OPD { get; set; }
        public List<Symptom> Symptoms { get; set; } 
        public List<tablet> Medicines { get; set; } 
    }
    
}
