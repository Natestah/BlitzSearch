using Blitz.Interfacing;

namespace Blitz.Avalonia.Controls.ViewModels;

public class MissingRequirementsViewModel(MissingRequirementResult missingRequirementResult) : ViewModelBase
{
    public MissingRequirementResult.Requirement Requirement => missingRequirementResult.MissingRequirement;

    public string RequirementMessage => missingRequirementResult.Message;
}