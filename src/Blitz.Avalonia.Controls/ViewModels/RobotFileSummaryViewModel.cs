using System.Text;
using Blitz.Interfacing;

namespace Blitz.Avalonia.Controls.ViewModels;

public class RobotFileSummaryViewModel : ViewModelBase
{
    private RobotFileDetectionSummary _summary;
    public RobotFileSummaryViewModel(RobotFileDetectionSummary summary)
    {
        _summary = summary;
    }

    public int RobotFilesCount => _summary.RobotFileDetailsList.Count;

    private StringBuilder _reportBuilder = new StringBuilder();

    public string GetCsvReport()
    {
        if (_reportBuilder.Length > 0)
        {
            return _reportBuilder.ToString();
        }

        _reportBuilder.AppendLine("#FileName,#FileSize,#Reason");
        foreach (var fileSummary in _summary.RobotFileDetailsList)
        {
            _reportBuilder.Append(fileSummary.FileName);
            _reportBuilder.Append(',');
            _reportBuilder.Append(fileSummary.FileSize);
            _reportBuilder.Append(',');
            _reportBuilder.Append(fileSummary.RobotState.ToString());
            _reportBuilder.AppendLine();
        }

        return _reportBuilder.ToString();
    }

    public string ActionMessage => _summary.ActionMessage;
}