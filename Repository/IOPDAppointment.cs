using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IOPDAppointment
    {
        void CreateAppointment(OPDAppointmentModel appointment, int hospitalId, int? subHospitalId);
        int UpdateAppointment(OPDAppointmentModel appointment, int hospitalId, int? subHospitalId);
        void DeleteAppointment(int appointmentId, int hospitalId, int? subHospitalId);
        OPDAppointmentModel? GetAppointmentById(int appointmentId, int hospitalId, int? subHospitalId);
        List<OPDAppointmentModel> GetAllAppointments(int hospitalId, int? subHospitalId);
        void UpdateStatus(int appointmentId, int hospitalId, int? subHospitalId, string status);
        List<OPDMedicineVM> GetMedicinesByOPDId(int opdId);

        List<OPD> GetPatientFullHistory(int patientId);

    }
}
