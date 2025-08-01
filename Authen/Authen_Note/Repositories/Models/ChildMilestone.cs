﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class ChildMilestone
{
    public Guid ChildId { get; set; }

    public int MilestoneId { get; set; }

    public DateOnly? AchievedDate { get; set; }

    public string Status { get; set; }

    public string Notes { get; set; }

    public string Guidelines { get; set; }

    public string Importance { get; set; }

    public string Category { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Child Child { get; set; }

    public virtual Milestone Milestone { get; set; }
}