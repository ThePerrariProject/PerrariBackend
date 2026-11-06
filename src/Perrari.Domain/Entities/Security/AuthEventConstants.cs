namespace Perrari.Domain.Entities.Security;

public static class AuthEventTypes
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string LoginBlocked = "LOGIN_BLOCKED";
    public const string Locked = "LOCKED";
    public const string TwoFactorRequired = "TWO_FACTOR_REQUIRED";
    public const string TwoFactorFailed = "TWO_FACTOR_FAILED";
    public const string TwoFactorEnabled = "TWO_FACTOR_ENABLED";
    public const string TwoFactorDisabled = "TWO_FACTOR_DISABLED";
    public const string RecoveryCodeUsed = "RECOVERY_CODE_USED";
    public const string TokenRefreshed = "TOKEN_REFRESHED";
    public const string TokenReuseDetected = "TOKEN_REUSED_DETECTED";
    public const string Logout = "LOGOUT";
}

public static class AuthEventStatusses
{
    public const string Success = "SUCCESS";
    public const string Failure = "FAILURE";
}