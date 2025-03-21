﻿using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IdentityController : ControllerBase
{
    private ITokenService _TokenService { get; }
    private IAuthenticationService _AuthenticationService { get; }
    private IAccountService _AccountService { get; }

    public IdentityController(
        ITokenService tokenService,
        IAuthenticationService authenticationService,
        IAccountService accountService)
    {
        _TokenService = tokenService;
        _AuthenticationService = authenticationService;
        _AccountService = accountService;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] string jwt)
    {
        var token = new Token(jwt);
        var isValid = await _TokenService.ValidateSecurityTokenAsync(token);
        return Ok(isValid);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SignInRequest signIn)
    {
        try
        {
            var token = await _AuthenticationService.SignInAsync(signIn);
            return Ok(token);
        }
        catch (AuthenticationException authEx) when (authEx.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(authEx.Message);
        }
        catch (AuthenticationException authEx) when (authEx.Cause == ExceptionCause.Unknown)
        {
            return Problem(statusCode: 500, detail: authEx.Message);
        }
    }

    [HttpPost("register")]
    [SwaggerHeaderParameter("API-KEY", Description = "Authorization API Key")]
    public async Task<IActionResult> Register([FromBody] SignUpRequest signUp)
    {
        try
        {
            var registrationKey = AuthorizationHelper.GetAuthorizationApiKey(Request);
            var user = await _AuthenticationService.SignUpAsync(signUp, registrationKey);
            return Ok(user);
        }
        catch (RegistrationException registerEx) when (registerEx.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(new { registerEx.Message, registerEx.Details });
        }
        catch (RegistrationException registerEx) when (registerEx.Cause == ExceptionCause.SystemConfiguration)
        {
            return Unauthorized();
        }
    }

    [HttpPost("forgotpassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var user = await _AuthenticationService.GetIdentityAsync(request.Email);
            if (user is null)
            {
                return BadRequest();
            }
            await _AccountService.RequestPasswordReset(user.Guid);
            return Ok(new { message = "Reset link sent successfully" });
        }
        catch (AccountException accountExc) when (accountExc.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(accountExc.Message);
        }
        catch (AccountException accountExc) when (accountExc.Cause == ExceptionCause.Unknown)
        {
            return Problem(statusCode: 500, detail: accountExc.Message);
        }
    }

    [HttpPost("resetpassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var user = await _AuthenticationService.GetIdentityAsync(request.Email);
            if (user is null)
            {
                return BadRequest();
            }
            await _AccountService.ResetPassword(user.Guid, new ResetToken(request.Token), request.NewPassword);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (AccountException accountExc) when (accountExc.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(accountExc.Message);
        }
        catch (AccountException accountExc) when (accountExc.Cause == ExceptionCause.Unknown)
        {
            return Problem(statusCode: 500, detail: accountExc.Message);
        }
    }
}