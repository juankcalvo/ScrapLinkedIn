﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ScrapLinkedInDall.Database.Models;

[Table("Experience")]
public partial class Experience
{
    [Key]
    public int ExperienceId { get; set; }

    [Required]
    public string Details { get; set; }

    public int UserProfileId { get; set; }

    [ForeignKey("UserProfileId")]
    [InverseProperty("Experiences")]
    public virtual UserProfile UserProfile { get; set; }
}