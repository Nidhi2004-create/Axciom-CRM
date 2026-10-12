using System.Text;

namespace AcxiomCRM.Services
{
    public interface IExportService
    {
        byte[] GenerateCsv<T>(IEnumerable<T> data, Func<T, IEnumerable<string>> rowSelector, IEnumerable<string> headers);
    }

    public class ExportService : IExportService
    {
        public byte[] GenerateCsv<T>(IEnumerable<T> data, Func<T, IEnumerable<string>> rowSelector, IEnumerable<string> headers)
        {
            var sb = new StringBuilder();

            // Header row
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

            // Data rows
            foreach (var item in data)
            {
                var row = rowSelector(item).Select(EscapeCsvField);
                sb.AppendLine(string.Join(",", row));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        private static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            string clean = field.Replace("\"", "\"\"");
            return $"\"{clean}\"";
        }
    }
}
