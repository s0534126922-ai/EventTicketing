using System;
using System.Collections.Generic;
using System.Text;


namespace EventTicketing.Core.Exceptions;

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
