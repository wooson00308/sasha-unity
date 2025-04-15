using NPOI.SS.UserModel;

namespace ExcelToSO
{
    public interface IExcelRow
    {
        void FromExcelRow(IRow row);
    }
}
