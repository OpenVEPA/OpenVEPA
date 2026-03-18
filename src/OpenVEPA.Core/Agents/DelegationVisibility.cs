namespace OpenVEPA.Core.Agents;

/// <summary>Controls how delegation is presented to the user.</summary>
public enum DelegationVisibility
{
    /// <summary>Delegation is hidden from the user.</summary>
    Invisible,

    /// <summary>A brief note is shown indicating delegation occurred.</summary>
    Visible,

    /// <summary>Full details of the delegation are shown to the user.</summary>
    Detailed
}
