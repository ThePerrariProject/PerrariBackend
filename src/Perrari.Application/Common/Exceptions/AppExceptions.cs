namespace Perrari.Application.Common.Exceptions;

public class UnauthorizedException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

public class AccountLockedException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);
