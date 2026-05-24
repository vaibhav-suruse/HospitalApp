// Repository/IDischarge.cs
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IDischarge
    {
        DischargeModel GetAdmissionForDischarge(int ipdId);
        void DischargePatient(DischargeModel model, int updatedBy);
        DischargeModel GetDischargeSummary(int ipdId);

        
        List<DischargeMedicineModel> GetDischargeMedicines(int ipdId);
        void SaveDischargeMedicine(DischargeMedicineModel model, int createdBy);
        void DeleteDischargeMedicine(int id);
    }
}