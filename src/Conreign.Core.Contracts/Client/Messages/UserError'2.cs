﻿namespace Conreign.Core.Contracts.Client.Messages
{
    public class UserError<T, TDetails> : UserError<T> where T : struct
    {
        public UserError(string message, T type, TDetails details) : base(message, type)
        {
            Details = details;
        }

        public TDetails Details { get; }
    }
}
