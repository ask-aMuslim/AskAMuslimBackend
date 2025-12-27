using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskAMuslimBackend.Domain.Entities;

public class StudentProfile : BaseEntity
{
    public Guid StudentId { get; set; }
    
    // Basic Profile Information
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    
    // Religious Status Information
    // IsMuslim is derived from User.ReligionStatus (BornMuslim or RevertedMuslim = true, NonMuslim = false)
    public bool IsMuslim { get; set; }
    
    // For Muslims only: Born Muslim or New Muslim (Revert)
    public bool IsNewMuslim { get; set; }
    
    // For New Muslims (Reverts) only
    public DateOnly? DateOfIslamConversion { get; set; }
    public string? OldReligion { get; set; }
    public string? ReasonForConversion { get; set; }

    // Navigation property
    public Student Student { get; set; } = null!;
}
