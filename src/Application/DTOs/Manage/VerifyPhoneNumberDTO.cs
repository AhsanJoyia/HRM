﻿namespace Application.DTOs.Manage;

public class VerifyPhoneNumberDTO
{
    [Required]
    public string? Code { get; set; }

    [Required]
    [Phone]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}
