﻿namespace Domain.Models;

public class ForgotPasswordRequest
{
    public string Email { get; set; }
}

public class ChangePasswordRequest
{
    public string Email { get; set; }
    public string NewPassword { get; set; }
    public string Token { get; set; }
}
