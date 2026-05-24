using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface Ipatient
    {
        int AddPatient(Patient model, int hospitalId, int? subHospitalId);
        List<Patient> GetAllPatients(int hospitalId, int? subHospitalId);
        Patient GetPatientById(int patientId, int hospitalId, int? subHospitalId);
        int UpdatePatient(Patient model, int hospitalId, int? subHospitalId);
        int DeletePatient(int patientId, int hospitalId, int? subHospitalId);

        // Login method
        Patient Login(string email, string password);

        bool CheckEmail(string email);
        bool UpdatePassword(string email, string newPassword);
        bool SignupPatient(Patient patient, out string message);

        List<Patient> SearchPatientByMobile( string search, int hospitalId, int? subHospitalId);



        PatientAccount LoginAccount(string email, string password);
        List<PatientProfileVM> GetProfilesByAccountId(int accountId);
        int SignupAccount(PatientAccount account, out string message);
        void GenerateOTP(string email, string purpose, string otp);
        bool VerifyOTP(string email, string otp, string purpose);
        void ResetPasswordAccount(string email, string newPassword);
        bool EmailExistsInAccount(string email);
        void AddFamilyMember(int accountId,string firstName, string lastName, string relation,
                             int age,string gender,string email);   // ← add email parameter

        bool IsSamePassword(string email, string newPassword);

    }
}
