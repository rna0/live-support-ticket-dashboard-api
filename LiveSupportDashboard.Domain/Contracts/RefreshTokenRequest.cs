﻿namespace LiveSupportDashboard.Domain.Contracts;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
